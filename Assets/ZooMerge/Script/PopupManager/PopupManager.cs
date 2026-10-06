using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using static BallEventManager;
using System.Collections.Generic;
//using UnityEngine.iOS;

public class PopupManager : SfxBehaviourTirgger
{
    public static PopupManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private PrefabLibrary prefabLibrary;
    [SerializeField] private CollectibleFlyTarget heartFlyTarget;

    private const string MAIN_MENU = "MainMenuPopup";
    private const string WIN_POPUP = "WinPopup";
    private const string WIN_COMPLETE_POPUP = "WinCompletePopup";
    private const string LOSE_POPUP = "LosePopup";
    private const string PAUSE = "PauseRestartPopup";

    public static event Action OnForceClosePausePopup;

    [SerializeField] private BallSpawner ballSpawner;
    [SerializeField] LevelProgressBarSlider levelProgressBarSlider;
    [SerializeField] private LevelProgressDisplay sessionProgressDisplay;

    [SerializeField] private GameObject ensureActivePanelOnStart;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float winLosePopupDelay = 0.5f;
    [SerializeField, Min(0f)] private float losePopupDelay = 0.65f;

    private Coroutine winLosePopupRoutine;
    private Coroutine losePopupRoutine;

    private Coroutine beginSessionRoutine;

    private GameObject pauseRestartPopupInstance;
    private GameObject mainMenuPopupInstance;
    private GameObject gameUIPopupInstance;

    private bool isSessionActive;

    private bool endPopupLocked = false;
    private GameOverReason? lockedEndReason = null;

    private readonly Dictionary<string, RectTransform> navigationPopups = new();

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
    }

    private void Update()
    {
        // Editor-only hotkey to open pause popup
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            // Only during active gameplay + not game over
            if (!isSessionActive) return;
            if (BallEventManager.IsGameOver) return;

            ShowPauseRestartPopup();
        }
#endif
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

        if (losePopupRoutine != null)
        {
            StopCoroutine(losePopupRoutine);
            losePopupRoutine = null;
        }
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

        // Apply the loss immediately.
        // The popup will read the already-updated retry count.
        CloudSaveManager.AddLoss(GameOverReason.Lost);

        AnalyticsEvents.LevelEnd("lost");

        // Only delay the visual popup.
        if (losePopupRoutine != null)
            StopCoroutine(losePopupRoutine);

        losePopupRoutine =
            StartCoroutine(ShowLosePopupAfterDelay());
    }

    private IEnumerator ShowLosePopupAfterDelay()
    {
        if (losePopupDelay > 0f)
            yield return new WaitForSecondsRealtime(losePopupDelay);

        losePopupRoutine = null;

        ShowEndLvlPopup(GameOverReason.Lost);
    }

    public void ShowPauseRestartPopup()
    {
        // Already open -> same pause button acts like Resume.
        if (pauseRestartPopupInstance != null)
        {
            BallEventManager.RaiseSessionResumed();

            SessionManager.Instance?.HidePauseButtonArt();

            PlayUiSfx(SfxCue.ButtonClick);

            OnForceClosePausePopup?.Invoke();
            ClearPausePopupReference();

            return;
        }

        // From here down, we're trying to OPEN the pause popup.
        if (BallEventManager.PauseBlocked) return;

        if (BallEventManager.IsGameOverCountdownActive) return;

        if (MergeScoreDisplayController.Instance != null &&
            MergeScoreDisplayController.Instance.HasActiveScorePopups)
            return;

        GameObject prefab = prefabLibrary.GetRaw(PAUSE);

        if (prefab == null)
            return;

        pauseRestartPopupInstance = Instantiate(prefab, transform);
        pauseRestartPopupInstance.SetActive(true);

        SessionManager.Instance?.ShowPauseButtonArt();

        PlayUiSfx(SfxCue.ButtonClick);

        BallEventManager.RaiseSessionPaused();
    }

    public void ClearPausePopupReference()
    {
        pauseRestartPopupInstance = null;
    }

    public void ShowEndLvlPopup(GameOverReason reason)
    {
        isSessionActive = false;

        ForceClosePausePopup();

        if (endPopupLocked)
        {
            if (lockedEndReason.HasValue && lockedEndReason.Value != reason)
                return;

            return;
        }

        endPopupLocked = true;
        lockedEndReason = reason;

        if (winLosePopupRoutine != null)
        {
            StopCoroutine(winLosePopupRoutine);
            winLosePopupRoutine = null;
        }

        if (gameUIPopupInstance == null)
        {
            string popupId =
                reason == GameOverReason.Won
                    ? WIN_COMPLETE_POPUP
                    : LOSE_POPUP;

            gameUIPopupInstance = SpawnEndPopup(popupId);
        }

        if (gameUIPopupInstance == null)
            return;

        WinLosePopup popup = gameUIPopupInstance.GetComponent<WinLosePopup>();

        if (popup == null)
        {
            Debug.LogError("[PopupManager] Spawned end popup has no WinLosePopup component.");
            return;
        }

        popup.PrepareBeforeShow(
            reason,
            reason == GameOverReason.Won
        );

        gameUIPopupInstance.SetActive(true);

        PopupMessageCenter.ShowEndPopupMessage(
            popup,
            reason
        );

        popup.PlayPopupIn();
    }

    public void ShowEnemyDefeatedMessage()
    {
        ForceClosePausePopup();

        if (winLosePopupRoutine != null)
            StopCoroutine(winLosePopupRoutine);

        winLosePopupRoutine = StartCoroutine(
            ShowWinLosePopupAfterDelay(
                winLosePopupDelay,
                WIN_POPUP,
                () =>
                {
                    if (WinLosePopup.Instance == null)
                        return;

                    WinLosePopup.Instance.SetLevelCompleteContext(false);

                    PopupMessageCenter.ShowEnemyDefeated(
                        WinLosePopup.Instance
                    );

                    WinLosePopup.Instance.PlayPopupIn();
                }
            )
        );
    }

    private GameObject SpawnEndPopup(string prefabId)
    {
        if (prefabLibrary == null)
        {
            Debug.LogError("[PopupManager] PrefabLibrary is missing.");
            return null;
        }

        GameObject prefab = prefabLibrary.GetRaw(prefabId);

        if (prefab == null)
        {
            Debug.LogError($"[PopupManager] Popup prefab not found: {prefabId}");
            return null;
        }

        GameObject instance = Instantiate(prefab, transform);
        instance.SetActive(false);

        if (instance.transform is RectTransform rect)
            StretchToParent(rect);

        return instance;
    }

    private IEnumerator ShowWinLosePopupAfterDelay(
        float delay,
        string prefabId,
        Action showBody)
    {
        isSessionActive = false;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (gameUIPopupInstance == null)
            gameUIPopupInstance = SpawnEndPopup(prefabId);

        if (gameUIPopupInstance == null)
        {
            winLosePopupRoutine = null;
            yield break;
        }

        gameUIPopupInstance.SetActive(true);

        showBody?.Invoke();

        winLosePopupRoutine = null;
    }

    public void ShowMainMenu()
    {
        RectTransform popup =
            GetOrCreateNavigationPopup(MAIN_MENU);

        if (popup == null)
            return;

        popup.gameObject.SetActive(true);
        popup.anchoredPosition = Vector2.zero;

        mainMenuPopupInstance = popup.gameObject;

        BallEventManager.RaiseMainMenuPopupOpened();
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        rect.anchoredPosition = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    public void BeginSessionDeferred(
        bool isNewLevel,
        bool restartmidlevel = false,
        int warmupFrames = 2,
        bool pendingRewardsOnly = false)
    {
        if (beginSessionRoutine != null)
            StopCoroutine(beginSessionRoutine);

        beginSessionRoutine = StartCoroutine(
            BeginSessionDeferredRoutine(
                isNewLevel,
                restartmidlevel,
                warmupFrames,
                pendingRewardsOnly
            )
        );
    }

    private IEnumerator BeginSessionDeferredRoutine(
        bool isNewLevel,
        bool restartmidlevel,
        int warmupFrames,
        bool pendingRewardsOnly)
    {
        int frames = Mathf.Clamp(
            warmupFrames,
            1,
            5
        );

        for (int i = 0; i < frames; i++)
            yield return null;

        // FRAME A
        CircleDragInput.Instance?.DisableInput();

        ballSpawner?.SetPreviewVisible(false);

        yield return null;

        // FRAME B
        EnemySpawner.Instance?.ClearEnemy();

        if (!restartmidlevel)
            CircleDragInput.Instance?.ClearSpawnContainer();

        yield return null;

        // FRAME C+
        // Reward animations happen only after setup work is out of the way.
        if (pendingRewardsOnly)
        {
            yield return StartCoroutine(
                TryGrantPendingHeartReward()
            );

            yield return StartCoroutine(
                TryGrantPendingSpaceshipSkinReward()
            );
        }
        else if (isNewLevel)
        {
            yield return StartCoroutine(
                TryGrantCompletedLevelReward()
            );
        }

        // Give reward completion one clean rendered frame.
        yield return null;

        ballSpawner?.SetPreviewVisible(true);

        // Start ball.
        ballSpawner?.BeginSession();

        yield return null;

        // Fire session listeners separately from ball creation.
        BallEventManager.RaiseSessionStarted();

        yield return null;

        // Enemy work gets its own frame.
        int nextEnemyId =
            MergeLevelManager.GetCurrentEnemyId();

        EnemySpawner.Instance?.SpawnEnemy(
            nextEnemyId,
            delayEnter: true
        );

        yield return null;

        if (!isNewLevel)
            BallEventManager.RaiseEnemyAdvanced();

        BallStateSaver.Instance.SaveState(
            BallRegistry.ActiveBalls.ToArray()
        );

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

        // Clean up any hanging preview or active ball.
        // ✅ But when restarting a saved mid-level, do NOT clear the restored cage balls.
        if (!restartmidlevel)
        {
            CircleDragInput.Instance?.ClearSpawnContainer();
        }

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
        // if (levelProgressBarSlider == null)
        // {
        //     Debug.LogError("⚠️ PopupManager: LevelProgressBarSlider reference is missing.");
        //     return;
        // }

        // levelProgressBarSlider.InitializeCurrentLevel();
        // // Grey-out all enemies already defeated (up to CurrentEnemyIndex - 1)
        // levelProgressBarSlider.SyncIconsToCurrentProgress(includeCurrent: false);


        if (sessionProgressDisplay != null)
        {
            sessionProgressDisplay.InitializeCurrentLevel();
        }
    }

    private void HandleSessionStarted()
    {
        isSessionActive = true;
    }

    private void HandleSessionOver(BallInfo info, GameOverReason reason)
    {
        // ✅ Session is officially over
        isSessionActive = false;

        // ✅ If pause is open for any reason, kill it immediately
        ForceClosePausePopup();

        // ✅ Optional: extra hard block for system pause calls
        lockedEndReason = reason;  // keeps consistency with your end popup lock
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
        if (pauseRestartPopupInstance != null) return;

        // Optional: Skip if main menu is active
        if (mainMenuPopupInstance != null && mainMenuPopupInstance.activeInHierarchy)
            return;

        if (endPopupLocked) return;

        ShowPauseRestartPopup();
    }

    public void WarmupSession()
    {
        // Pre-create first ball.
        ballSpawner?.WarmupPreview();

        // Pre-create first enemy.
        if (EnemySpawner.Instance != null)
        {
            int enemyId =
                MergeLevelManager.GetCurrentEnemyId();

            EnemySpawner.Instance.WarmupEnemy(
                enemyId
            );
        }
    }

    private void AnalyticsEvents_OnSessionStarted()
    {
        AnalyticsEvents.LevelStart("popup_manager_begin_session");
    }

    public void ForceClosePausePopup()
    {
        // Tell the pause popup (if it exists) to close itself properly
        OnForceClosePausePopup?.Invoke();

        // Extra safety: clear reference even if popup was already destroyed
        ClearPausePopupReference();
    }

    private IEnumerator TryGrantCompletedLevelReward()
    {
        // HEART FIRST
        yield return StartCoroutine(
            TryGrantPendingHeartReward()
        );

        // SPACESHIP SKIN SECOND
        yield return StartCoroutine(
            TryGrantPendingSpaceshipSkinReward()
        );
    }

    private IEnumerator TryGrantPendingSpaceshipSkinReward()
    {
        string pendingSkinId =
            SpaceshipSkinProgress.PendingSkinRewardId;

        if (string.IsNullOrWhiteSpace(pendingSkinId))
            yield break;

        if (SpaceshipSkinController.Instance == null)
        {
            Debug.LogWarning(
                "[PopupManager] SpaceshipSkinController is missing."
            );

            yield break;
        }

        bool skinFinished = false;
        bool skinSuccess = false;

        SpaceshipSkinController.Instance.UnlockAndRevealSkin(
            pendingSkinId,
            success =>
            {
                skinSuccess = success;
                skinFinished = true;
            }
        );

        while (!skinFinished)
            yield return null;

        if (!skinSuccess)
            yield break;

        SpaceshipSkinProgress.ClearPendingSkinReward();

        Debug.Log(
            $"[PopupManager] Granted pending spaceship skin: {pendingSkinId}"
        );
    }

    private IEnumerator TryGrantPendingHeartReward()
    {
        int amount =
            PlayerProgress.PendingHeartRewardAmount;

        if (amount <= 0)
            yield break;

        if (CollectibleFlyService.Instance == null)
        {
            Debug.LogWarning(
                "[PopupManager] CollectibleFlyService is missing."
            );

            yield break;
        }

        if (heartFlyTarget == null)
        {
            Debug.LogWarning(
                "[PopupManager] Heart fly target is missing."
            );

            yield break;
        }

        SessionManager.Instance?.PrepareBottomUIForReward();

        bool heartFinished = false;

        CollectibleFlyService.Instance.Fly(
            "Heart_Session",
            amount,
            heartFlyTarget,
            null,
            () => heartFinished = true
        );

        Debug.Log(
            $"[PopupManager] Granting pending heart reward: {amount}"
        );

        while (!heartFinished)
            yield return null;

        // The collectible completed successfully.
        PlayerProgress.ClearPendingHeartReward();

        Debug.Log(
            $"[PopupManager] Pending heart reward granted and cleared: {amount}"
        );
    }

    public RectTransform GetOrCreateNavigationPopup(string prefabId)
    {
        if (string.IsNullOrEmpty(prefabId))
            return null;

        if (navigationPopups.TryGetValue(
                prefabId,
                out RectTransform existing) &&
            existing != null)
        {
            return existing;
        }

        if (prefabLibrary == null)
            return null;

        GameObject prefab = prefabLibrary.GetRaw(prefabId);

        if (prefab == null)
        {
            Debug.LogWarning(
                $"[PopupManager] Navigation popup not found: {prefabId}"
            );

            return null;
        }

        GameObject instance = Instantiate(prefab, transform);

        RectTransform rect =
            instance.transform as RectTransform;

        if (rect == null)
        {
            Debug.LogWarning(
                $"[PopupManager] Navigation popup needs a RectTransform: {prefabId}"
            );

            Destroy(instance);
            return null;
        }

        StretchToParent(rect);

        navigationPopups[prefabId] = rect;

        if (prefabId == MAIN_MENU)
            mainMenuPopupInstance = instance;

        return rect;
    }

    public void DestroyNavigationPopupsExcept(string prefabIdToKeep)
    {
        List<string> idsToRemove = new();

        foreach (var pair in navigationPopups)
        {
            string prefabId = pair.Key;
            RectTransform popup = pair.Value;

            if (prefabId == prefabIdToKeep)
                continue;

            if (popup != null)
                Destroy(popup.gameObject);

            idsToRemove.Add(prefabId);
        }

        foreach (string id in idsToRemove)
        {
            navigationPopups.Remove(id);
        }
    }
}

