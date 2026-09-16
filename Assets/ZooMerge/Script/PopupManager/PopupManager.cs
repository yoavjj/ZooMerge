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

    private PauseRestartPopup pausePopup;
    public static event Action OnForceClosePausePopup;

    [SerializeField] private BallSpawner ballSpawner;
    [SerializeField] LevelProgressBarSlider levelProgressBarSlider;
    [SerializeField] private LevelProgressDisplay sessionProgressDisplay;

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

        // ✅ consume retry BEFORE popup reads retry count
        CloudSaveManager.AddLoss(GameOverReason.Lost);

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

        PlayUiSfx(SfxCue.ButtonClick);

        BallEventManager.RaiseSessionPaused(); // 🆕 Trigger pause animation/UI logic
    }

    public void ClearPausePopupReference()
    {
        pauseRestartPopupInstance = null;
    }

    public void ShowEndLvlPopup(GameOverReason reason)
    {
        isSessionActive = false;

        ForceClosePausePopup(); // Ensure any open pause popup is closed immediately

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

        if (gameUIPopupInstance == null)
        {
            string popupId =
                reason == GameOverReason.Won
                    ? WIN_COMPLETE_POPUP
                    : LOSE_POPUP;

            gameUIPopupInstance = SpawnEndPopup(popupId);
        }

        if (WinLosePopup.Instance == null)
        {
            Debug.LogError(
                "[PopupManager] Spawned end popup has no WinLosePopup component."
            );

            return;
        }

        WinLosePopup.Instance.SetLevelCompleteContext(
            reason == GameOverReason.Won
        );

        PopupMessageCenter.ShowEndPopupMessage(
            WinLosePopup.Instance,
            reason
        );
    }

    public void ShowEnemyDefeatedMessage()
    {
        ForceClosePausePopup();
        if (winLosePopupRoutine != null) StopCoroutine(winLosePopupRoutine);
        winLosePopupRoutine = StartCoroutine(
            ShowWinLosePopupAfterDelay(
                winLosePopupDelay,
                WIN_POPUP,
                () =>
                {
                    if (WinLosePopup.Instance != null)
                        WinLosePopup.Instance.SetLevelCompleteContext(false);

                    PopupMessageCenter.ShowEnemyDefeated(
                        WinLosePopup.Instance
                    );
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

    public void BeginSessionDeferred(bool isNewLevel, bool restartmidlevel = false, int warmupFrames = 2)
    {
        if (beginSessionRoutine != null)
            StopCoroutine(beginSessionRoutine);

        beginSessionRoutine = StartCoroutine(BeginSessionDeferredRoutine(isNewLevel, restartmidlevel, warmupFrames));
    }

    private IEnumerator BeginSessionDeferredRoutine(
        bool isNewLevel,
        bool restartmidlevel,
        int warmupFrames)
    {
        int frames =
            Mathf.Clamp(
                warmupFrames,
                1,
                5
            );

        for (int i = 0; i < frames; i++)
            yield return null;

        CircleDragInput.Instance?.DisableInput();
        AdManager.Instance?.LoadBanner();

        EnemySpawner.Instance?.ClearEnemy();

        CircleDragInput.Instance?.ClearSpawnContainer();

        // Hide any warmed-up preview while rewards play.
        ballSpawner?.SetPreviewVisible(false);

        // --------------------------------
        // COMPLETED LEVEL REWARDS
        // --------------------------------

        if (isNewLevel)
        {
            yield return StartCoroutine(
                TryGrantCompletedLevelReward()
            );
        }

        // --------------------------------
        // REWARDS FINISHED
        // --------------------------------

        ballSpawner?.SetPreviewVisible(true);

        // Only now promote/create the active ball.
        ballSpawner?.BeginSession();

        BallEventManager.RaiseSessionStarted();

        // Wait one frame before enemy Addressables.
        yield return null;

        int nextEnemyId =
            MergeLevelManager.GetCurrentEnemyId();

        EnemySpawner.Instance?.SpawnEnemy(
            nextEnemyId,
            delayEnter: true
        );

        if (!isNewLevel)
            BallEventManager.RaiseEnemyAdvanced();

        BallStateSaver.Instance.SaveState(
            BallRegistry.ActiveBalls.ToArray()
        );

        BallEventManager.ResetMidLevelLossFlag();

        StartCoroutine(
            PromoteNextFrame()
        );

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
        ballSpawner?.WarmupPreview();
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
        LevelCompletionReward reward = MergeLevelManager.PreviousCompletedLevelReward;

        bool hasReward =
            reward != null &&
            reward.rewardType != LevelRewardType.None;

        if (!hasReward)
            yield break;

        // --------------------------------
        // HEART FIRST
        // --------------------------------

        if ((reward.rewardType & LevelRewardType.Heart) != 0)
        {
            int amount = Mathf.Max(1, reward.amount);

            // Bring ONLY the bottom UI in early,
            // because the heart reward flies toward it.
            SessionManager.Instance?.PrepareBottomUIForReward();

            bool heartFinished = false;

            if (CollectibleFlyService.Instance != null && heartFlyTarget != null)
            {
                CollectibleFlyService.Instance.Fly(
                    "Heart_Session",
                    amount,
                    heartFlyTarget,
                    null,
                    () => heartFinished = true
                );

                Debug.Log(
                    $"[PopupManager] Granting completed-level reward: {amount} Heart(s)."
                );

                while (!heartFinished)
                    yield return null;
            }
        }

        // --------------------------------
        // SPACESHIP SKIN SECOND
        // --------------------------------

        if ((reward.rewardType & LevelRewardType.SpaceshipSkin) != 0)
        {
            if (string.IsNullOrWhiteSpace(reward.spaceshipSkinId))
            {
                Debug.LogWarning("[PopupManager] Spaceship skin reward has no skin ID.");
                yield break;
            }

            if (SpaceshipSkinController.Instance == null)
            {
                Debug.LogWarning("[PopupManager] SpaceshipSkinController is missing.");
                yield break;
            }

            bool skinFinished = false;

            SpaceshipSkinController.Instance.UnlockAndRevealSkin(
                reward.spaceshipSkinId,
                success =>
                {
                    skinFinished = true;

                    if (success)
                        Debug.Log($"[PopupManager] Granted spaceship skin: {reward.spaceshipSkinId}");
                }
            );

            while (!skinFinished)
                yield return null;
        }
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

