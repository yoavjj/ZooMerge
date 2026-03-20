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

    [SerializeField] private bool preloadLevelRevealOnStart = true;
    [SerializeField] private bool keepRevealInactiveWhenIdle = true;

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
                levelMessageText.text = $"Level {currentLevel} Complete!\nNext: Level {currentLevel + 1}";
                playButtonText.text = $"Level {currentLevel + 1}";
                break;

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
        // Clear previous content
        if (contentRoot.childCount > 0)
        {
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);
        }

        if (prefabLibrary == null)
        {
            Debug.LogError("[WinLosePopup] PrefabLibrary is not assigned.");
            return;
        }

        string prefabId = (reason == GameOverReason.Lost)
            ? LOSE
            : (levelCompleteContext ? LEVEL_COMPLETE : WIN);

        var prefab = prefabLibrary.GetWinLose(prefabId);
        if (prefab == null)
            return;

        var instance = Instantiate(prefab, contentRoot);

        activeContent = instance;

        activeContent.OnShown();
    }

    public void ShowContinueOption()
    {
        SetMessage("Try Again?");
        SetContinueMessageAfterFailure();

        isContinue = true;
    }

    public void OnMainMenuButtonPressed()
    {
        PopupManager.Instance?.ConfirmReturnToMainMenu();
        animator.SetTrigger("Out");
        Destroy(gameObject, 1f);
    }

    public void ShowGalaxyRoadmap()
    {
        if (prefabLibrary == null || levelArtRevealContainer == null)
            return;

        var prefab = prefabLibrary.GetRaw(GALAXY_ROADMAP);
        if (prefab == null)
            return;

        var instGO = Instantiate(prefab, levelArtRevealContainer);

        var roadmap = instGO.GetComponent<Popup_GalaxyRoadmap>();
        if (roadmap == null)
        {
            Debug.LogError("[WinLosePopup] GalaxyRoadmap prefab missing script.");
            return;
        }

        roadmap.Initialize();

        // optional: reset transform
        var rt = instGO.transform as RectTransform;
        if (rt != null)
        {
            rt.anchoredPosition3D = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }
    }

    public void OnPlayPressed()
    {
        if (mergeSummaryPanel != null && mergeSummaryPanel.IsBusy)
        {
            Debug.Log("⏳ Wait! Merge summary still running.");
            return;
        }

        bool isNewLevel = levelCompleteContext; // true only when popup showed a real level win

        if (isContinue)
        {
            var dropped = CircleDragInput.Instance?.droppedContainer;
            if (dropped != null)
                BallStateSaver.Instance.RestoreState(dropped);
            isContinue = false;
        }

        if (!isNewLevel)
        {
            // ✅ normal case (mid-level / retry / etc.)
            BallEventManager.RaiseResetCounters(keepUI: true);

            PopupManager.Instance?.BeginSession(isNewLevel: false);
            PopupManager.Instance?.InitializeProgressBarNow();

            PlayContentOut();
            animator.SetTrigger("Out");
            Destroy(gameObject, 1.5f);
            return;
        }

        // ✅ end-of-level flow (level reveal)
        if (playPressedRoutine != null) StopCoroutine(playPressedRoutine);
        playPressedRoutine = StartCoroutine(PlayNextLevelRevealThenAdvance());

    }

    private IEnumerator PlayNextLevelRevealThenAdvance()
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

        // Reveal OUT
        if (levelArtRevealInstance != null)
            levelArtRevealInstance.PlayRevealOut();

        PopupManager.Instance?.BeginSession(isNewLevel: true);
        PopupManager.Instance?.InitializeProgressBarNow();

        // Let reveal-out finish
        if (revealOutDuration > 0f)
            yield return new WaitForSeconds(revealOutDuration);

        // Cleanup reveal popup
        if (keepRevealInactiveWhenIdle && levelArtRevealInstance != null)
            levelArtRevealInstance.gameObject.SetActive(false);

        Destroy(gameObject, 4.8f);
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

        if (toLevelEnd && preloadLevelRevealOnStart)
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

    private void HandleSummaryReady()
    {
        if (playButtonAnimator == null || string.IsNullOrEmpty(readyTrigger))
            return;

        playButtonAnimator.ResetTrigger(readyTrigger);
        playButtonAnimator.SetTrigger(readyTrigger);
    }
}
