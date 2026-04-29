using System.Collections;
using System.Linq;
using UnityEngine;
using static BallEventManager;
//using UnityEngine.iOS;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private PrefabLibrary prefabLibrary;

    private const string MAIN_MENU = "MainMenuPopup";
    private const string WIN_LOSE = "WinLosePopup";
    private const string PAUSE = "PauseRestartPopup";
    
    [SerializeField] private BallSpawner ballSpawner;
    [SerializeField] LevelProgressBarSlider levelProgressBarSlider;

    [SerializeField] private GameObject ensureActivePanelOnStart;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float winLosePopupDelay = 0.5f;

    private Coroutine winLosePopupRoutine;

    private Coroutine beginSessionRoutine;

    private GameObject pauseRestartPopupInstance;
    private GameObject mainMenuPopupInstance;
    private GameObject gameUIPopupInstance;

    private bool isSessionActive;

    private bool endPopupLocked = false;
    private GameOverReason? lockedEndReason = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (ensureActivePanelOnStart != null && !ensureActivePanelOnStart.activeSelf)
            ensureActivePanelOnStart.SetActive(true);

        if (prefabLibrary != null)
        {
            var prefab = prefabLibrary.GetRaw(MAIN_MENU);
            if (prefab != null)
            {
                mainMenuPopupInstance = Instantiate(prefab, transform);
                mainMenuPopupInstance.SetActive(true);
            }
        }
    }

    private void OnEnable()
    {
        BallEventManager.OnBallTouchedGameOverLine += HandleBallTouchedGameOverLine;
        WinLosePopup.OnWinLoseClosed += UnlockEndPopup;

        BallEventManager.OnSessionStarted += HandleSessionStarted;
        BallEventManager.OnReturnToMainMenu += HandleReturnToMainMenu;
        BallEventManager.OnGameOver += HandleSessionOver;
    }

    private void OnDisable()
    {
        BallEventManager.OnBallTouchedGameOverLine -= HandleBallTouchedGameOverLine;
        WinLosePopup.OnWinLoseClosed -= UnlockEndPopup;

        BallEventManager.OnSessionStarted -= HandleSessionStarted;
        BallEventManager.OnReturnToMainMenu -= HandleReturnToMainMenu;
        BallEventManager.OnGameOver -= HandleSessionOver;

        if (winLosePopupRoutine != null) { StopCoroutine(winLosePopupRoutine); winLosePopupRoutine = null; }
    }

    private void UnlockEndPopup()
    {
        endPopupLocked = false;
        lockedEndReason = null;

        // optional: if you want the next end popup to re-instantiate cleanly
        gameUIPopupInstance = null;
    }

    private void HandleBallTouchedGameOverLine(BallInfo info)
    {
        isSessionActive = false;

        // ✅ analytics: level ended by losing
        AnalyticsEvents.LevelEnd("lost");
        
        ShowEndLvlPopup(GameOverReason.Lost); // or whatever reason you want for this unique case
    }

    public void ShowPauseRestartPopup()
    {
        if (BallEventManager.PauseBlocked) return;

        if (BallEventManager.IsGameOverCountdownActive) return;
        
        if (MergeScoreDisplayController.Instance != null &&
            MergeScoreDisplayController.Instance.HasActiveScorePopups)
            return;

        if (pauseRestartPopupInstance == null)
        {
            var prefab = prefabLibrary.GetRaw(PAUSE);
            if (prefab != null)
                pauseRestartPopupInstance = Instantiate(prefab, transform);
        }

        pauseRestartPopupInstance.SetActive(true);

        BallEventManager.RaiseSessionPaused(); // 🆕 Trigger pause animation/UI logic
    }

    public void ClearPausePopupReference()
    {
        pauseRestartPopupInstance = null;
    }

    public void ShowEndLvlPopup(GameOverReason reason)
    {
        // ✅ If an end popup is already showing/locked, ignore any other attempt.
        if (endPopupLocked)
        {
            // Optional: allow same-reason refresh, but block different reason.
            if (lockedEndReason.HasValue && lockedEndReason.Value != reason)
                return;

            // If same reason, you can either return or let it refresh the text.
            return;
        }

        endPopupLocked = true;
        lockedEndReason = reason;

        if (winLosePopupRoutine != null)
        {
            StopCoroutine(winLosePopupRoutine);
            winLosePopupRoutine = null;
        }

        // Ensure popup exists
        if (gameUIPopupInstance == null)
        {
            var prefab = prefabLibrary.GetRaw(WIN_LOSE);
            if (prefab != null)
                gameUIPopupInstance = Instantiate(prefab, transform);
        }

        // Tell popup context
        if (WinLosePopup.Instance != null)
            WinLosePopup.Instance.SetLevelCompleteContext(reason == GameOverReason.Won);

        PopupMessageCenter.ShowEndPopupMessage(WinLosePopup.Instance, reason);
    }

    public void ShowEnemyDefeatedMessage()
    {
        if (winLosePopupRoutine != null) StopCoroutine(winLosePopupRoutine);
        winLosePopupRoutine = StartCoroutine(ShowWinLosePopupAfterDelay(winLosePopupDelay, () =>
        {
            // 🆕 Ensure it knows this is NOT the end of the level
            if (WinLosePopup.Instance != null)
            {
                WinLosePopup.Instance.SetLevelCompleteContext(false);
            }
            PopupMessageCenter.ShowEnemyDefeated(WinLosePopup.Instance);
        }));
    }

    private IEnumerator ShowWinLosePopupAfterDelay(float delay, System.Action showBody)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (gameUIPopupInstance == null)
        {
            var prefab = prefabLibrary.GetRaw(WIN_LOSE);
            if (prefab != null)
                gameUIPopupInstance = Instantiate(prefab, transform);
        }

        showBody?.Invoke();
        winLosePopupRoutine = null;
    }

    public void ShowMainMenu()
    {
        if (prefabLibrary != null)
        {
            var prefab = prefabLibrary.GetRaw(MAIN_MENU);
            if (prefab != null)
            {
                mainMenuPopupInstance = Instantiate(prefab, transform);
                mainMenuPopupInstance.SetActive(true);
            }
        }
    }

    public void BeginSessionDeferred(bool isNewLevel, bool restartmidlevel = false, int warmupFrames = 2)
    {
        if (beginSessionRoutine != null)
            StopCoroutine(beginSessionRoutine);

        beginSessionRoutine = StartCoroutine(BeginSessionDeferredRoutine(isNewLevel, restartmidlevel, warmupFrames));
    }

    private IEnumerator BeginSessionDeferredRoutine(bool isNewLevel, bool restartmidlevel, int warmupFrames)
    {
        int frames = Mathf.Clamp(warmupFrames, 1, 5);
        for (int i = 0; i < frames; i++)
            yield return null;

        // Light setup first (cheap)
        CircleDragInput.Instance?.DisableInput();
        AdManager.Instance?.LoadBanner();
        EnemySpawner.Instance?.ClearEnemy();
        CircleDragInput.Instance?.ClearSpawnContainer();
        ballSpawner?.BeginSession();
        BallEventManager.RaiseSessionStarted();

        // ✅ wait 1 more frame before the expensive Addressables spawn
        yield return null;

        int nextEnemyId = MergeLevelManager.GetCurrentEnemyId();
        EnemySpawner.Instance?.SpawnEnemy(nextEnemyId, delayEnter: true);

        if (!isNewLevel)
            BallEventManager.RaiseEnemyAdvanced();

        BallStateSaver.Instance.SaveState(BallRegistry.ActiveBalls.ToArray());
        BallEventManager.ResetMidLevelLossFlag();

        StartCoroutine(PromoteNextFrame());
        InitializeProgressBarNow();

        beginSessionRoutine = null;

        AnalyticsEvents_OnSessionStarted();
    }

    public void BeginSession(bool isNewLevel, bool restartmidlevel = false)
    {
        // 🔒 LOCK INPUT during session setup
        CircleDragInput.Instance?.DisableInput();

        AdManager.Instance?.LoadBanner();

        // ✅ Ensure no duplicate enemy exists
        EnemySpawner.Instance?.ClearEnemy(delay: 0.2f);

        // Clean up any hanging preview or active ball
        CircleDragInput.Instance?.ClearSpawnContainer();

        ballSpawner?.BeginSession();
        BallEventManager.RaiseSessionStarted();

        int nextEnemyId = MergeLevelManager.GetCurrentEnemyId();
        EnemySpawner.Instance?.SpawnEnemy(nextEnemyId, delayEnter: true);

        if (!isNewLevel)
        {
            BallEventManager.RaiseEnemyAdvanced();
        }

        // ✅ Save state immediately after new session starts
        BallStateSaver.Instance.SaveState(BallRegistry.ActiveBalls.ToArray());
        BallEventManager.ResetMidLevelLossFlag();
        StartCoroutine(PromoteNextFrame());

        AnalyticsEvents_OnSessionStarted();
    }

    private IEnumerator PromoteNextFrame()
    {
        yield return null; // wait one frame (UI, layout, etc.)
        CircleDragInput.Instance?.spawner?.PromoteFromPreview();

        // 🔓 SAFE TO ENABLE INPUT NOW
        CircleDragInput.Instance?.EnableInput();
    }

    public void ConfirmReturnToMainMenu()
    {
        BallStateSaver.Instance.Clear();

        CircleDragInput.Instance?.ClearSpawnContainer(); // Clear active ball
        BallEventManager.RaiseReturnToMainMenu();        // Destroys all balls
        BallEventManager.RaiseResetCounters(false);           // Resets UI counters
        EnemySpawner.Instance?.ClearEnemy();             // Clears current enemy
        AdManager.Instance?.HideBanner();                // Hide ads

        // ✅ Reset level progress (clears grey icons & layout)
        if (levelProgressBarSlider != null)
        {
            levelProgressBarSlider.RestartVisuals();             // Reset icon animation triggers
            MergeLevelManager.SetLevel(MergeLevelManager.CurrentLevelNumber); // Reset enemy index to 0
        }

        ShowMainMenu(); // Then show main menu
    }

    public void InitializeProgressBarNow()
    {
        if (levelProgressBarSlider == null)
        {
            Debug.LogError("⚠️ PopupManager: LevelProgressBarSlider reference is missing.");
            return;
        }

        levelProgressBarSlider.InitializeCurrentLevel();
        // Grey-out all enemies already defeated (up to CurrentEnemyIndex - 1)
        levelProgressBarSlider.SyncIconsToCurrentProgress(includeCurrent: false);
    }

    private void HandleSessionStarted()
    {
        isSessionActive = true;
    }

    private void HandleSessionOver(BallInfo info, GameOverReason reason)
    {
        isSessionActive = false;
    }

    private void HandleReturnToMainMenu()
    {
        isSessionActive = false;
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause) return;

        // only during active gameplay session + not game over
        if (!isSessionActive) return;
        if (BallEventManager.IsGameOver) return;

        TryShowPausePopupFromSystem();
    }

    // ✅ CHANGED
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) return;

        // only during active gameplay session + not game over
        if (!isSessionActive) return;
        if (BallEventManager.IsGameOver) return;

        TryShowPausePopupFromSystem();
    }

    private void TryShowPausePopupFromSystem()
    {
#if UNITY_EDITOR
        return; // 👈 Skip showing pause popup in the Unity Editor
#else
        if (pauseRestartPopupInstance != null) return;

        // Optional: Skip if main menu is active
        if (mainMenuPopupInstance != null && mainMenuPopupInstance.activeInHierarchy)
            return;

        ShowPauseRestartPopup();
#endif
    }

    public void WarmupSession()
    {
        ballSpawner?.WarmupPreview();
    }

    private void AnalyticsEvents_OnSessionStarted()
    {
        AnalyticsEvents.LevelStart("popup_manager_begin_session");
    }
}

