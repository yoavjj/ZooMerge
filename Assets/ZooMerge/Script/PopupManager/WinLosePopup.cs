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
    [SerializeField] private GameObject winContentPrefab;
    [SerializeField] private GameObject loseContentPrefab;
    [SerializeField] private GameObject levelCompleteContentPrefab;

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

    [Header("Level Art Reveal")]
    [SerializeField] private LevelArtRevealController levelArtRevealController;
    [SerializeField, Min(0f)] private float levelRevealDuration = 4.5f;      // ✅ how long reveal stays on screen
    [SerializeField, Min(0f)] private float revealOutDuration = 0.6f;        // ✅ how long reveal "Out" takes
    private Coroutine playPressedRoutine;

    private bool isContinue = false;
    private bool levelCompleteContext = false;
    private GameOverReason currentReason;
    private Coroutine applyRoutine;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDisable()
    {
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

        // 🆕 Decide which prefab to spawn based on the reason and level context
        GameObject prefabToSpawn;
        if (reason == GameOverReason.Lost)
        {
            prefabToSpawn = loseContentPrefab;
        }
        else
        {
            // If it's a win, check if it's the end of the entire level
            prefabToSpawn = levelCompleteContext ? levelCompleteContentPrefab : winContentPrefab;
        }

        var instance = Instantiate(prefabToSpawn, contentRoot);

        // IMPORTANT: no GetComponent
        activeContent = instance.GetComponent<IWinLoseContent>();

        activeContent?.OnShown();
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

    public void OnPlayPressed()
    {
        if (mergeSummaryPanel != null && mergeSummaryPanel.IsBusy)
        {
            Debug.Log("⏳ Wait! Merge summary still running.");
            return;
        }

        bool isNewLevel = levelCompleteContext; // true only when popup showed a real level win

        if (isNewLevel && levelArtRevealController != null)
        {
            int curLevel = MergeLevelManager.CurrentLevelNumber; // still the “completed” level at this moment
            levelArtRevealController.Prepare(curLevel);
            levelArtRevealController.PlayRevealAndSwap();
        }


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
        // We are still on the completed level at this moment
        if (levelArtRevealController != null)
        {
            int curLevel = MergeLevelManager.CurrentLevelNumber;
            levelArtRevealController.Prepare(curLevel);
            levelArtRevealController.PlayRevealAndSwap();
        }

        // Keep popup alive during reveal (no animator Out here)
        yield return new WaitForSeconds(levelRevealDuration);

        // Trigger reveal OUT (mask exit)
        if (levelArtRevealController != null)
            levelArtRevealController.PlayRevealOut();

        // Wait for reveal-out animation to finish
        if (revealOutDuration > 0f)
            yield return new WaitForSeconds(revealOutDuration);

        // ✅ Only now advance level + reset counters
        MergeLevelManager.AdvanceLevel();
        BallEventManager.RaiseResetCounters(keepUI: false);

        // Start next session after level advance
        PopupManager.Instance?.BeginSession(isNewLevel: true);
        PopupManager.Instance?.InitializeProgressBarNow();

        // Now close popup
        PlayContentOut();
        animator.SetTrigger("Out");
        Destroy(gameObject, 1.5f);

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
        levelCompleteContext = toLevelEnd;
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
}
