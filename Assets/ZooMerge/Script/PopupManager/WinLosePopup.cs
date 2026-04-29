using System;
using System.Collections;
using TMPro;
using UnityEngine;
using static BallEventManager;

public interface IWinLoseContent
{
    Animator Animator { get; }
    void OnShown();
}

public class WinLosePopup : MonoBehaviour
{
    public static WinLosePopup Instance { get; private set; }

    [Header("Content Variants")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private PrefabLibrary prefabLibrary;

    private const string WIN = "WinContent";
    private const string LOSE = "LoseContent";
    private const string LEVEL_COMPLETE = "LevelCompleteContent";
    private const string LEVEL_REVEAL = "LevelReveal";
    private const string GALAXY_ROADMAP = "GalaxyRoadmapPopup";

    [Header("Merge Summary")]
    [SerializeField] private MergeSummaryPanel mergeSummaryPanel;

    private IWinLoseContent activeContent;

    [Header("UI Refs")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI levelMessageText;
    [SerializeField] private TextMeshProUGUI playButtonText;
    [SerializeField] private Animator animator;

    [Header("Progress FX")]
    [SerializeField, Min(0f)] private float enemyDoneDelay = 0.25f;         // delay before triggering "Done"
    [SerializeField, Min(0f)] private float sliderAdvanceDuration = 0.35f;  // how long the slider anim takes
    [SerializeField] private AnimationCurve sliderAdvanceCurve;
    [SerializeField] LevelProgressBarSlider levelProgressBarSlider;
    [SerializeField] private CollectibleFlyController collectibleFlyController;

    [Header("Level Art Reveal (Spawned Popup)")]
    [SerializeField] private Transform levelArtRevealContainer;             // ✅ scene container (PopupsRoot)
    private LevelArtRevealController levelArtRevealInstance;
    [SerializeField, Min(0f)] private float popupOutDelay = 0.35f; // delay before closing WinLosePopup

    [Header("Play Button Ready FX")]
    [SerializeField] private Animator playButtonAnimator; // assign the Play button animator
    [SerializeField] private string readyTrigger = "Ready"; // trigger name in that animator

    [SerializeField, Min(0f)] private float levelRevealDuration = 4.5f;      // ✅ how long reveal stays on screen
    [SerializeField, Min(0f)] private float revealOutDuration = 0.6f;        // ✅ how long reveal "Out" takes
    
    private Coroutine playPressedRoutine;

    private bool isContinue = false;
    private bool levelCompleteContext = false;
    private GameOverReason currentReason;
    private Coroutine applyRoutine;

    private Popup_GalaxyRoadmap roadmapInstance;
    private bool roadmapOpenOrSpawning = false;

    public static event Action OnWinLoseClosed;

    private bool playPressedLocked = false;

    [SerializeField] private bool preloadLevelRevealOnStart = true;
    [SerializeField] private bool keepRevealInactiveWhenIdle = true;

    private enum DeferredAction
    {
        None,
        PlayPressed,
        ShowGalaxyRoadmap
    }

    private DeferredAction deferredAction = DeferredAction.None;
    private bool deferredRoadmapFromLevelFlow = false;

    private void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        if (mergeSummaryPanel != null)
            mergeSummaryPanel.onAllCollectiblesFinished += HandleSummaryReady;
    }

    private void OnDisable()
    {
        if (mergeSummaryPanel != null)
            mergeSummaryPanel.onAllCollectiblesFinished -= HandleSummaryReady;

        if (applyRoutine != null)
        {
            StopCoroutine(applyRoutine);
            applyRoutine = null;
        }

        if (playPressedRoutine != null)
        {
            StopCoroutine(playPressedRoutine);
            playPressedRoutine = null;
        }

        if (roadmapInstance != null)
            roadmapInstance.OnClosedRoadmap -= HandleRoadmapClosed;
    }

    public void SetMessage(string msg)
    {
        if (messageText != null)
            messageText.text = msg;
    }

    public void SetLevelMessage(int currentLevel, GameOverReason reason)
    {
        if (levelMessageText == null || playButtonText == null) return;

        // Build merge summary
        if (mergeSummaryPanel != null && MergeSessionTracker.Instance != null)
        {
            var snapshot = MergeSessionTracker.Instance.GetCurrentSnapshot();
            mergeSummaryPanel.Build(snapshot);
        }

        currentReason = reason;

        BuildContent(reason);

        if (animator != null)
        {
            animator.SetTrigger(reason == GameOverReason.Won ? "Win" : "Lose");
        }

        if (levelProgressBarSlider != null)
        {
            var fly = collectibleFlyController;
            if (levelProgressBarSlider != null && collectibleFlyController != null)
            {
                levelProgressBarSlider.OnEnemyMarkedDone += HandleEnemyDone;
            }
        }

        switch (reason)
        {
            case GameOverReason.Won:
                {
                    // Current state (before advancing)
                    int levelInGalaxy = MergeLevelManager.CurrentLevelInGalaxy;         // 1..N
                    int levelsInGalaxy = Mathf.Max(1, MergeLevelManager.LevelsInCurrentGalaxy);
                    string galaxyName = MergeLevelManager.CurrentGalaxyName;

                    bool isGalaxyEnd = MergeLevelManager.IsLastLevelInCurrentGalaxy;

                    //levelMessageText.text = $"Level {currentLevel} Complete!";

                    if (!isGalaxyEnd)
                    {
                        int nextLevelInGalaxy = Mathf.Clamp(levelInGalaxy + 1, 1, levelsInGalaxy);

                        // Example short “smart” label:
                        // "Milky Way 2/7" (you can tweak format)
                        playButtonText.text = $"Next Level: {nextLevelInGalaxy}";

                        // If you still want a second line:
                        //levelMessageText.text += $"\nNext: {galaxyName} {nextLevelInGalaxy}/{levelsInGalaxy}";
                    }
                    else
                    {
                        // End of galaxy -> short “new galaxy” text
                        playButtonText.text = "Next Galaxy";
                        //levelMessageText.text += "\nNext: New Galaxy";
                    }

                    break;
                }

            case GameOverReason.Lost:
                levelMessageText.text = $"Try Again: Level {currentLevel}";
                playButtonText.text = "Retry";
                break;

            default:
                levelMessageText.text = $"Level {currentLevel}";
                playButtonText.text = $"Level {currentLevel}";
                break;
        }
    }

    public void SpawnWinCoins()
    {
        if (collectibleFlyController == null)
        {
            Debug.LogWarning("⚠️ WinLosePopup: CollectibleFlyController missing.");
            return;
        }

        collectibleFlyController.SpawnCoinsToTopBar();
    }
    private void HandleEnemyDone(int index)
    {
        collectibleFlyController.PositionCoinContainerToIndex(index);
    }

    private void BuildContent(GameOverReason reason)
    {
        ClearContent();

        var prefab = GetContentPrefab(reason);
        if (prefab == null) return;

        activeContent = Instantiate(prefab, contentRoot);
        activeContent.OnShown();
    }

    public void ShowContinueOption()
    {
        SetMessage("Try Again?");
        SetContinueMessageAfterFailure();

        isContinue = true;
    }

    private void ClearContent()
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);
    }

    private WinLoseContentBase GetContentPrefab(GameOverReason reason)
    {
        if (prefabLibrary == null)
        {
            Debug.LogError("[WinLosePopup] PrefabLibrary is not assigned.");
            return null;
        }

        string id = (reason == GameOverReason.Lost)
            ? LOSE
            : (levelCompleteContext ? LEVEL_COMPLETE : WIN);

        return prefabLibrary.GetWinLose(id);
    }

    public void OnMainMenuButtonPressed()
    {
        AnalyticsEvents.MainMenuEnter("from_game");
        
        PopupManager.Instance?.ConfirmReturnToMainMenu();
        animator.SetTrigger("Out");
        PlayContentOut();
        Destroy(gameObject, 1f);
    }

    public void ShowGalaxyRoadmap(bool fromLevelFlow = false)
    {
        // ✅ If already open/spawning, ignore repeated clicks
        if (roadmapOpenOrSpawning)
            return;

        // ✅ block while collectibles/summary is still running
        if (IsSummaryBusy())
        {
            deferredAction = DeferredAction.ShowGalaxyRoadmap; // last wins
            deferredRoadmapFromLevelFlow = fromLevelFlow;
            return;
        }

        deferredAction = DeferredAction.None;

        if (!fromLevelFlow)
        {
            AnalyticsEvents.LogRoadmapView(
            !fromLevelFlow,
            MergeLevelManager.CurrentGalaxyId.ToString(), // Convert to string if your Log method expects string
            MergeLevelManager.CurrentLevelNumber
        );
        }

        // ✅ If we cached an instance but it got destroyed, clear it
        if (roadmapInstance == null)
        {
            roadmapInstance = null; // (Unity destroyed object becomes "fake null")
        }
        else
        {
            // ✅ Reuse existing roadmap
            roadmapOpenOrSpawning = true;
            roadmapInstance.gameObject.SetActive(true);
            roadmapInstance.Initialize();
            roadmapInstance.PlayIntro(fromLevelFlow);
            ResetRectTransform(roadmapInstance.transform);
            return;
        }

        // ✅ Create new roadmap
        roadmapOpenOrSpawning = true;

        roadmapInstance = SpawnGalaxyRoadmap();
        if (roadmapInstance == null)
        {
            roadmapOpenOrSpawning = false; // allow retry if something went wrong
            return;
        }

        // ✅ listen for close so we can allow opening again
        roadmapInstance.OnClosedRoadmap += HandleRoadmapClosed;

        // ✅ PREPARE BEFORE advancing level affects data
        roadmapInstance.PrepareProgressBeforeReveal();

        roadmapInstance.Initialize();
        roadmapInstance.PlayIntro(fromLevelFlow);
        ResetRectTransform(roadmapInstance.transform);
    }

    private void ResetRectTransform(Transform t)
    {
        var rt = t as RectTransform;
        if (rt != null)
        {
            rt.anchoredPosition3D = Vector3.zero;
        }
        else
        {
            t.localPosition = Vector3.zero;
        }

        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    private void HandleRoadmapClosed()
    {
        roadmapOpenOrSpawning = false;

        if (roadmapInstance != null)
            roadmapInstance.OnClosedRoadmap -= HandleRoadmapClosed;

        roadmapInstance = null;
    }

    private Popup_GalaxyRoadmap SpawnGalaxyRoadmap()
    {
        if (prefabLibrary == null || levelArtRevealContainer == null)
            return null;

        var prefab = prefabLibrary.GetGalaxyRoadmap(GALAXY_ROADMAP);
        if (prefab == null)
            return null;

        return Instantiate(prefab, levelArtRevealContainer);
    }

    public void OnPlayPressed()
    {
        // 1. Check if the button has already been pressed successfully
        if (playPressedLocked)
            return;

        if (IsSummaryBusy())
        {
            deferredAction = DeferredAction.PlayPressed; // last wins
            return;
        }

        // 2. Lock the button so it cannot be pressed again
        playPressedLocked = true;

        deferredAction = DeferredAction.None; // clear if you want
        HandleContinue();

        if (!IsNewLevel())
        {
            HandleNormalRestart();
            return;
        }

        StartNextLevelFlow();
        OnWinLoseClosed?.Invoke();
    }

    private bool IsSummaryBusy()
    {
        if (mergeSummaryPanel != null && mergeSummaryPanel.IsBusy)
        {
            Debug.Log("⏳ Wait! Merge summary still running.");
            return true;
        }
        return false;
    }

    private bool IsNewLevel()
    {
        return levelCompleteContext;
    }

    private void HandleContinue()
    {
        if (!isContinue) return;

        var dropped = CircleDragInput.Instance?.droppedContainer;
        if (dropped != null)
            BallStateSaver.Instance.RestoreState(dropped);

        isContinue = false;
    }

    private void HandleNormalRestart()
    {
        BallEventManager.RaiseResetCounters(keepUI: true);

        PopupManager.Instance?.BeginSession(isNewLevel: false);
        PopupManager.Instance?.InitializeProgressBarNow();

        ClosePopup();
    }

    private void ClosePopup()
    {
        OnWinLoseClosed?.Invoke();

        roadmapOpenOrSpawning = false;
        roadmapInstance = null;

        PlayContentOut();
        animator?.SetTrigger("Out");
        Destroy(gameObject, 2.5f);
    }

    private void StartNextLevelFlow()
    {
        if (playPressedRoutine != null)
            StopCoroutine(playPressedRoutine);

        playPressedRoutine = StartCoroutine(PlayNextLevelRevealThenAdvance());
    }

    private IEnumerator PlayNextLevelRevealThenAdvance()
    {
        bool isGalaxyEnd = MergeLevelManager.IsLastLevelInCurrentGalaxy;

        if (!isGalaxyEnd)
        {
            // Spawn and play reveal (if not already spawned)
            if (levelArtRevealInstance == null)
                levelArtRevealInstance = SpawnLevelRevealPopup();

            if (levelArtRevealInstance != null)
            {
                if (keepRevealInactiveWhenIdle)
                    levelArtRevealInstance.gameObject.SetActive(true);

                int curLevel = MergeLevelManager.CurrentLevelNumber;
                levelArtRevealInstance.Prepare(curLevel, afterCompletion: true);
                levelArtRevealInstance.PlayRevealAndSwap();

                // Advance level only after reveal is done showing
                MergeLevelManager.AdvanceLevel();
                BallEventManager.RaiseResetCounters(keepUI: false);
                levelArtRevealInstance.updateProgressBarSlider();
            }
        }
        else
        {
            // ✅ Galaxy-end: advance into the next galaxy, then show roadmap popup
            // ✅ 1. Spawn popup FIRST
            var roadmap = SpawnGalaxyRoadmap();
            if (roadmap != null)
            {
                roadmap.PrepareProgressBeforeReveal(); // ✅ cache OLD state
                roadmap.PlayIntro(true);
                roadmap.Initialize();
                ResetRectTransform(roadmap.transform);
            }

            // ✅ 2. THEN advance level
            MergeLevelManager.AdvanceLevel();
            BallEventManager.RaiseResetCounters(keepUI: false);
        }

        // ✅ Delay before closing Win/Lose popup (does NOT extend total reveal time)
        float delay = Mathf.Clamp(popupOutDelay, 0f, levelRevealDuration);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        animator.SetTrigger("Out");
        PlayContentOut();

        // ✅ Keep reveal visible for the remaining time
        float remaining = Mathf.Max(0f, levelRevealDuration - delay);
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        // Reveal OUT (only for the level reveal popup)
        if (!isGalaxyEnd && levelArtRevealInstance != null)
            levelArtRevealInstance.PlayRevealOut();

        // Let reveal-out finish (only relevant for level reveal popup)
        if (!isGalaxyEnd && revealOutDuration > 0f)
            yield return new WaitForSeconds(revealOutDuration);

        // Cleanup reveal popup
        if (!isGalaxyEnd && keepRevealInactiveWhenIdle && levelArtRevealInstance != null)
            levelArtRevealInstance.gameObject.SetActive(false);

        Destroy(gameObject, 6f);
        playPressedRoutine = null;
    }

    public void PlayContentOut()
    {
        if (activeContent == null)
            return;

        var anim = activeContent.Animator;
        if (anim != null)
        {
            anim.SetTrigger("Out");
        }
    }

    public void SetTemporaryMessage()
    {
        if (playButtonText != null)
            playButtonText.text = "Continue";

        if (levelMessageText != null)
        {
            int currentLevel = MergeLevelManager.CurrentLevelNumber;
            int currentEnemy = MergeLevelManager.CurrentEnemyIndex + 1; // next enemy index
            int totalEnemies = MergeLevelManager.TotalEnemiesInLevel;

            levelMessageText.text = $"Next: Enemy {currentEnemy}/{totalEnemies}\nLevel {currentLevel}";
        }
    }

    private void SetContinueMessageAfterFailure()
    {
        if (playButtonText != null)
            playButtonText.text = "Retry";

        if (levelMessageText != null)
        {
            int currentLevel = MergeLevelManager.CurrentLevelNumber;
            int currentEnemy = MergeLevelManager.CurrentEnemyIndex + 1; // display-friendly (1-based)
            int totalEnemies = MergeLevelManager.TotalEnemiesInLevel;

            if (MergeLevelManager.CurrentEnemyIndex == 0)
            {
                // First enemy → show simpler message
                levelMessageText.text = $"You can retry from here\nLevel {currentLevel}";
            }
            else
            {
                // Mid-level retry → show more context
                levelMessageText.text = $"You can retry from here\nEnemy {currentEnemy}/{totalEnemies} · Level {currentLevel}";
            }
        }
    }

    public void ApplyProgressAdvance(bool toLevelEnd)
    {
        if (levelProgressBarSlider == null)
        {
            Debug.LogWarning("⚠️ WinLosePopup: LevelProgressBarSlider reference is missing.");
            return;
        }

        levelCompleteContext = toLevelEnd;  // already in your code

        if (toLevelEnd && preloadLevelRevealOnStart && !MergeLevelManager.IsLastLevelInCurrentGalaxy)
        {
            if (levelArtRevealInstance == null)
                levelArtRevealInstance = SpawnLevelRevealPopup();

            if (keepRevealInactiveWhenIdle && levelArtRevealInstance != null)
                levelArtRevealInstance.gameObject.SetActive(false);
        }

        levelProgressBarSlider?.PlayAdvanceAnimationFromPopup(
            toLevelEnd,
            enemyDoneDelay,
            sliderAdvanceDuration,
            sliderAdvanceCurve
        );
    }

    private LevelArtRevealController SpawnLevelRevealPopup()
    {
        if (prefabLibrary == null || levelArtRevealContainer == null)
            return null;

        var prefab = prefabLibrary.GetLevelReveal(LEVEL_REVEAL);
        if (prefab == null)
            return null;

        var inst = Instantiate(prefab, levelArtRevealContainer);

        var rt = inst.transform as RectTransform;
        if (rt != null)
        {
            rt.anchoredPosition3D = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }
        else
        {
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
        }

        return inst;
    }

    public void ResetProgressBarVisuals()
    {
        if (levelProgressBarSlider == null) return;

        levelProgressBarSlider.InitializeCurrentLevel(); // rebuild visuals
        levelProgressBarSlider.SyncIconsToCurrentProgress(includeCurrent: false); // keep grey state as-is
        //collectibleFlyController.PositionCoinContainerToCurrentIcon(); // reposition coin container
    }

    public void SetLevelCompleteContext(bool isComplete)
    {
        levelCompleteContext = isComplete;
    }

    private void HandleSummaryReady()
    {
        if (playButtonAnimator == null || string.IsNullOrEmpty(readyTrigger))
            return;

        playButtonAnimator.ResetTrigger(readyTrigger);
        playButtonAnimator.SetTrigger(readyTrigger);

        // ✅ run last cached action (only one)
        var action = deferredAction;
        var roadmapFlow = deferredRoadmapFromLevelFlow;

        deferredAction = DeferredAction.None; // clear first to avoid loops
        deferredRoadmapFromLevelFlow = false;

        if (action == DeferredAction.PlayPressed)
            OnPlayPressed();
        else if (action == DeferredAction.ShowGalaxyRoadmap)
            ShowGalaxyRoadmap(roadmapFlow);
    }
}
