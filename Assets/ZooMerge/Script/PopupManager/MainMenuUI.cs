using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MainMenuUI : SfxBehaviourTirgger
{
    [Header("Top Bar")]
    [SerializeField] private TopBarMenu topBarMenu;

    [Header("Ball Choice")]
    [SerializeField] private BallChoiceMenu ballChoiceMenu;
    private BallSelectionManager BallSelection =>
    BallSelectionManager.Instance;

    [Header("UI Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private CardSelectionVisualController playButtonVisual;

    [SerializeField] Animator mainMenuAnimator;


    [SerializeField] private LevelArtController levelArtController;

    private bool playLocked = false;

    private int cachedLevelNumber;
    private bool cachedIsNewLevel;
    private bool cacheReady = false;

    [Header("Main Menu Overlay")]
    [SerializeField]
    private PrefabLibrary prefabLibrary;

    [FormerlySerializedAs("outOfTriesContainer")]
    [SerializeField]
    private Transform mainMenuOverlayContainer;
    private GameObject outOfTriesInstance;
    private const string OUT_OF_TRIES_POPUP = "OutOfTriesPopup";

    [Header("Coin Cooldown Feature")]
    [SerializeField] private CoinCooldownWidget coinCooldownWidget;
    [SerializeField, Min(0f)] private float collectFtueDelay = 1.5f;

    private Coroutine collectFtueDelayRoutine;
    private GameObject collectFtuePopupInstance;

    private const string COLLECT_FTUE_POPUP = "CollectFTUEPopup";

    [Header("Settings Popup")]
    private GameObject settingsPopupInstance;
    private const string SETTINGS_POPUP = "SettingsPopup";

    [Header("Galaxy Roadmap Popup")]
    [SerializeField] private Transform roadmapContainer;

    private Popup_GalaxyRoadmap roadmapInstance;
    private bool roadmapOpenOrSpawning = false;

    private const string GALAXY_ROADMAP = "GalaxyRoadmapPopup";

    private BallUnlockPopup ballUnlockPopupInstance;
    private const string BALL_UNLOCK_POPUP =
    "BallUnlockPopup";

    private const string CONNECTION_LOST_POPUP =
    "ConnectionLostPopup";

    private GameObject connectionLostInstance;
    private Coroutine internetCheckRoutine;


    private bool IsOutOfTriesPopupOpen => outOfTriesInstance != null;

    private Coroutine resumeAfterRetriesRoutine; 

    private Coroutine waitForWarmupRoutine;

    private Coroutine waitForBallWarmupRoutine;

    [SerializeField] private CollectibleFlyTarget heartFlyTarget; // target on your UI (tries/heart icon)
    [SerializeField] private string heartMenuEntryId = "Heart_menu"; // collectible fly entry for retry reward (optional)

    private void Awake()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayPressed);

        RefreshCoinCooldownFeatureVisibility();
    }

    private void OnEnable()
    {
        OutOfTriesPopup.RetriesPurchased +=
            HandleRetriesPurchasedFromPopup;

        if (BallSelection != null)
        {
            BallSelection.OnSelectionChanged +=
                HandleBallSelectionChanged;
        }

        if (ballChoiceMenu != null)
        {
            ballChoiceMenu.UnlockPopupRequested +=
                HandleUnlockPopupRequested;
        }
    }

    private void OnDisable()
    {
        OutOfTriesPopup.RetriesPurchased -=
            HandleRetriesPurchasedFromPopup;

        if (BallSelection != null)
        {
            BallSelection.OnSelectionChanged -=
                HandleBallSelectionChanged;
        }

        if (ballChoiceMenu != null)
        {
            ballChoiceMenu.UnlockPopupRequested -=
                HandleUnlockPopupRequested;
        }
    }

    private void Start()
    {
        AnalyticsEvents.MainMenuEnter("MainMenuUI.Start");
        StartCoroutine(BuildTopBarWhenReady());
    }

    private void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayPressed);

        if (outOfTriesInstance != null)
            Destroy(outOfTriesInstance);

        if (ballUnlockPopupInstance != null)
        {
            ballUnlockPopupInstance.Closed -=
                HandleBallUnlockPopupClosed;

            ballUnlockPopupInstance.AnimalUnlocked -=
                HandleAnimalUnlocked;

            Destroy(ballUnlockPopupInstance.gameObject);
            ballUnlockPopupInstance = null;
        }

        if (roadmapInstance != null)
        {
            roadmapInstance.OnClosedRoadmap -=
                HandleRoadmapClosed;

            Destroy(roadmapInstance.gameObject);
        }

        if (settingsPopupInstance != null)
            Destroy(settingsPopupInstance);

        if (internetCheckRoutine != null)
        {
            StopCoroutine(internetCheckRoutine);
            internetCheckRoutine = null;
        }

        if (connectionLostInstance != null)
        {
            Destroy(connectionLostInstance);
            connectionLostInstance = null;
        }

        if (collectFtuePopupInstance != null)
        {
            Destroy(
                collectFtuePopupInstance
            );

            collectFtuePopupInstance = null;
        }

        if (collectFtueDelayRoutine != null)
        {
            StopCoroutine(
                collectFtueDelayRoutine
            );

            collectFtueDelayRoutine = null;
        }
    }

    public void RefreshCoinCooldownFeatureVisibility()
    {
        if (coinCooldownWidget == null)
        {
            Debug.LogWarning("[MainMenuUI] CoinCooldownWidget reference is missing.");
            return;
        }

        coinCooldownWidget.gameObject.SetActive(CooldownRewardProgress.IsFeatureUnlocked);
    }

    private void HandleRetriesPurchasedFromPopup(int amount)
    {
        FlyHeartMenu(amount);

        // Keep Play locked until:
        // 1. the heart actually credits the retry
        // 2. the Out Of Tries popup has finished closing
        playLocked = true;

        if (playButton != null)
            playButton.interactable = false;

        if (resumeAfterRetriesRoutine != null)
            StopCoroutine(resumeAfterRetriesRoutine);

        resumeAfterRetriesRoutine =
            StartCoroutine(UnlockPlayAfterRetries());
    }

    private IEnumerator UnlockPlayAfterRetries()
    {
        // Wait until the flying heart actually adds the retry.
        yield return new WaitUntil(() =>
            PlayerProgress.CurrentLevelRetriesRemaining() > 0
        );

        // Wait until OutOfTriesPopup has finished its closing animation
        // and Unity has destroyed it.
        yield return new WaitUntil(() =>
            outOfTriesInstance == null
        );

        Debug.Log(
            "[MainMenuUI] Retry received. Player can press Play again."
        );

        outOfTriesInstance = null;
        resumeAfterRetriesRoutine = null;

        playLocked = false;

        if (playButton != null)
            playButton.interactable = true;

        RefreshPlayButtonState();
    }

    private void HandleBallSelectionChanged()
    {
        RefreshPlayButtonState();
    }

    private void RefreshPlayButtonState()
    {
        if (playButton == null)
            return;

        BallSelectionManager manager = BallSelection;

        bool canPlay =
            !playLocked &&
            manager != null &&
            manager.HasRequiredSelection;

        // Keep clickable so an invalid press can show the message.
        playButton.interactable = !playLocked;

        if (playButtonVisual != null)
            playButtonVisual.SetSelected(canPlay);
    }

    private IEnumerator BuildTopBarWhenReady()
    {
        yield return new WaitUntil(() =>
            GameInventory.Instance != null &&
            MergeSessionTracker.Instance != null
        );

        yield return new WaitUntil(() => FirebaseInitializer.IsReady);

        topBarMenu?.BuildCoinUI();
        topBarMenu?.BuildAllBallTypesUI();

        ballChoiceMenu?.Build();
        RefreshPlayButtonState();

        CacheSessionStartData();

        levelArtController?.Refresh();

        // Only create this feature after the player
        // has earned it.
        InitializeCoinCooldownFeature();

        // Do the first expensive ball creation while the player
        // is still looking at the Main Menu.
        PopupManager.Instance?.WarmupSession();
    }

    private void InitializeCoinCooldownFeature()
    {
        RefreshCoinCooldownFeatureVisibility();

        if (!CooldownRewardProgress.IsFeatureUnlocked)
            return;

        if (CooldownRewardProgress.ShouldShowFtue && collectFtueDelayRoutine == null)
            collectFtueDelayRoutine = StartCoroutine(ShowCoinCooldownFtueAfterDelay());
    }

    private IEnumerator ShowCoinCooldownFtueAfterDelay()
    {
        if (collectFtueDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                collectFtueDelay
            );
        }

        collectFtueDelayRoutine = null;

        // Check again in case something changed
        // while we were waiting.
        if (!CooldownRewardProgress.ShouldShowFtue)
            yield break;

        ShowCoinCooldownFtue();
    }

    private void ShowCoinCooldownFtue()
    {
        if (collectFtuePopupInstance != null)
            return;

        if (prefabLibrary == null ||
            mainMenuOverlayContainer == null)
        {
            Debug.LogWarning(
                "[MainMenuUI] Missing PrefabLibrary " +
                "or Main Menu Overlay Container."
            );

            return;
        }

        GameObject prefab =
            prefabLibrary.GetRaw(
                COLLECT_FTUE_POPUP
            );

        if (prefab == null)
        {
            Debug.LogWarning(
                "[MainMenuUI] CollectFTUEPopup " +
                "was not found in PrefabLibrary."
            );

            return;
        }

        collectFtuePopupInstance =
            Instantiate(
                prefab,
                mainMenuOverlayContainer
            );

        // The popup should only appear once.
        CooldownRewardProgress.MarkFtueShown();

        Debug.Log(
            "[MainMenuUI] " +
            "Coin Cooldown FTUE shown."
        );
    }

    private void CacheSessionStartData()
    {
        // If your level can be read without Firebase, this is instant.
        // If it depends on Firebase level load, make sure this runs after it's ready.
        cachedLevelNumber = MergeLevelManager.CurrentLevelNumber;
        cachedIsNewLevel = MergeLevelManager.CurrentEnemyIndex == 0;

        cacheReady = true;
    }

    private void OnPlayPressed()
    {
        if (playLocked)
            return;

        if (internetCheckRoutine != null)
            return;

        internetCheckRoutine =
            StartCoroutine(
                CheckInternetThenContinuePlay()
            );
    }

    private IEnumerator CheckInternetThenContinuePlay()
    {
        bool connected = false;

        yield return InternetConnectionChecker.Check(
            result => connected = result
        );

        // Internet is available.
        if (connected)
        {
            internetCheckRoutine = null;

            ContinuePlayPressed();
            yield break;
        }

        Debug.LogWarning(
            "[MainMenuUI] Internet unavailable. Showing connection popup."
        );

        bool restored = false;

        bool popupOpened =
            ShowConnectionLostPopup(
                () => restored = true
            );

        if (!popupOpened)
        {
            internetCheckRoutine = null;
            yield break;
        }

        // Wait until ConnectionLostPopup detects internet,
        // plays Connected, and its animation event completes.
        while (!restored)
            yield return null;

        connectionLostInstance = null;
        internetCheckRoutine = null;

        Debug.Log(
            "[MainMenuUI] Internet restored. Continuing Play."
        );

        // Continue the SAME Play press automatically.
        ContinuePlayPressed();
    }

    private bool ShowConnectionLostPopup(Action onConnectionRestored)
    {
        if (prefabLibrary == null ||
            mainMenuOverlayContainer == null)
        {
            Debug.LogWarning(
                "[MainMenuUI] Missing PrefabLibrary or popup container."
            );

            return false;
        }

        GameObject prefab =
            prefabLibrary.GetRaw(
                CONNECTION_LOST_POPUP
            );

        if (prefab == null)
        {
            Debug.LogWarning(
                "[MainMenuUI] ConnectionLostPopup prefab not found."
            );

            return false;
        }

        connectionLostInstance =
            Instantiate(
                prefab,
                mainMenuOverlayContainer
            );

        ConnectionLostPopup popup =
            connectionLostInstance.GetComponent<
                ConnectionLostPopup
            >();

        if (popup == null)
        {
            Debug.LogError(
                "[MainMenuUI] ConnectionLostPopup component is missing."
            );

            Destroy(connectionLostInstance);
            connectionLostInstance = null;

            return false;
        }

        popup.Initialize(
            onConnectionRestored
        );

        return true;
    }

    private void ContinuePlayPressed()
    {
        BallSelectionManager manager = BallSelection;

        if (manager == null || !manager.HasRequiredSelection)
        {
            PlayUiSfx(SfxCue.ButtonClickNegative);
            ballChoiceMenu?.ShowIncompleteSelectionMessage();

            Debug.LogWarning(
                "[MainMenuUI] Select exactly three animals before playing."
            );

            return;
        }

        // --------------------------------
        // CHECK RETRIES FIRST
        // --------------------------------

        if (!IsOutOfTriesPopupOpen &&
            !PlayerProgress.HasPendingHeartReward &&
            PlayerProgress.HasRetryLimitForCurrentLevel() &&
            PlayerProgress.CurrentLevelRetriesRemaining() <= 0)
        {
            PlayUiSfx(SfxCue.ButtonClickNegative);

            ShowOutOfTriesPopupFromMainMenu();
            return;
        }

        // From this point onward the Play request is accepted.
        // Keep the button locked while any required warmup finishes.
        playLocked = true;

        if (playButton != null)
            playButton.interactable = false;

        // --------------------------------
        // MAKE SURE BALLS ARE WARM
        // --------------------------------

        BallSpawner ballSpawner =
            CircleDragInput.Instance?.spawner;

        if (ballSpawner != null &&
            !ballSpawner.IsSessionWarm)
        {
            Debug.Log(
                "[MainMenuUI] Warming ball session before Play."
            );

            ballSpawner.EnsureSessionWarm();

            // Safety check.
            if (!ballSpawner.IsSessionWarm)
            {
                Debug.LogWarning(
                    "[MainMenuUI] Ball session warmup failed."
                );

                playLocked = false;

                if (playButton != null)
                    playButton.interactable = true;

                RefreshPlayButtonState();
                return;
            }

            Debug.Log(
                "[MainMenuUI] Ball session warmup complete."
            );
        }

        // --------------------------------
        // MAKE SURE ENEMY IS WARM
        // --------------------------------

        int enemyId =
            MergeLevelManager.GetCurrentEnemyId();

        if (EnemySpawner.Instance != null &&
            !EnemySpawner.Instance.IsEnemyWarm(enemyId))
        {
            Debug.Log(
                $"[MainMenuUI] Warming enemy {enemyId} before Play."
            );

            EnemySpawner.Instance.WarmupEnemy(enemyId);

            if (waitForWarmupRoutine != null)
                StopCoroutine(waitForWarmupRoutine);

            waitForWarmupRoutine =
                StartCoroutine(
                    WaitForEnemyWarmupThenPlay(
                        enemyId
                    )
                );

            return;
        }

        // --------------------------------
        // EVERYTHING IS READY
        // --------------------------------

        StartMainMenuOut();
    }

    private IEnumerator WaitForEnemyWarmupThenPlay(int enemyId)
    {
        while (
            EnemySpawner.Instance != null &&
            EnemySpawner.Instance.IsWarmingEnemy(enemyId)
        )
        {
            yield return null;
        }

        waitForWarmupRoutine = null;

        if (EnemySpawner.Instance != null &&
            !EnemySpawner.Instance.IsEnemyWarm(enemyId))
        {
            Debug.LogWarning(
                $"[MainMenuUI] Enemy {enemyId} warmup failed."
            );

            playLocked = false;

            if (playButton != null)
                playButton.interactable = true;

            RefreshPlayButtonState();
            yield break;
        }

        StartMainMenuOut();
    }

    private void StartMainMenuOut()
    {
        PlayUiSfx(SfxCue.ButtonClick);

        playLocked = true;

        if (playButton != null)
            playButton.interactable = false;

        CloudSaveManager.StartPlayTimer();

        AnalyticsEvents.MainMenuExit(
            "play_pressed"
        );

        mainMenuAnimator.SetTrigger("Out");
    }

    private void ShowOutOfTriesPopupFromMainMenu()
    {
        if (IsOutOfTriesPopupOpen) return;

        PlayUiSfx(SfxCue.ButtonClick);

        if (prefabLibrary == null || mainMenuOverlayContainer == null)
        {
            Debug.LogWarning("[MainMenuUI] Missing prefabLibrary or mainMenuOverlayContainer.");
            return;
        }

        var prefab = prefabLibrary.GetRaw(OUT_OF_TRIES_POPUP);
        if (prefab == null) return;

        outOfTriesInstance = Instantiate(prefab, mainMenuOverlayContainer);

        // ✅ Main-menu context => HIDE quit button to prevent crash flow
        OutOfTriesPopup.LastSpawned?.SetQuitButtonVisible(false);
    }

    public void ShowSettingsPopup()
    {
        if (settingsPopupInstance != null)
            return;

        PlayUiSfx(SfxCue.ButtonClick);

        if (prefabLibrary == null || mainMenuOverlayContainer == null)
        {
            Debug.LogWarning("[MainMenuUI] Missing PrefabLibrary or popup container.");
            return;
        }

        GameObject prefab = prefabLibrary.GetRaw(SETTINGS_POPUP);

        if (prefab == null)
        {
            Debug.LogWarning("[MainMenuUI] SettingsPopup prefab not found.");
            return;
        }

        settingsPopupInstance = Instantiate(prefab, mainMenuOverlayContainer);
    }

    public void ForceRefreshProgressUIAndCache()
    {
        // Refresh any level art / display
        levelArtController?.Refresh();

        // Re-cache which level should start when Play is pressed
        CacheSessionStartData();
    }

    public void FlyHeartMenu(int amount)
    {
        if (CollectibleFlyService.Instance == null)
        {
            Debug.LogWarning("[CollectibleFlyController] CollectibleFlyService.Instance is null.");
            return;
        }

        if (heartFlyTarget == null)
        {
            Debug.LogWarning("[CollectibleFlyController] heartFlyTarget not assigned.");
            return;
        }

        CollectibleFlyService.Instance.Fly(heartMenuEntryId, amount, heartFlyTarget, null);
    }

    public void ShowGalaxyRoadmap()
    {
        if (roadmapOpenOrSpawning)
            return;

        PlayUiSfx(SfxCue.ButtonClick);

        AnalyticsEvents.LogRoadmapView(
            true,
            MergeLevelManager.CurrentGalaxyId.ToString(),
            MergeLevelManager.CurrentLevelNumber
        );

        if (roadmapInstance != null)
        {
            roadmapOpenOrSpawning = true;

            roadmapInstance.gameObject.SetActive(true);
            roadmapInstance.Initialize();
            roadmapInstance.PlayIntro(false);

            ResetRoadmapRectTransform(roadmapInstance.transform);
            return;
        }

        roadmapOpenOrSpawning = true;

        roadmapInstance = SpawnGalaxyRoadmap();

        if (roadmapInstance == null)
        {
            roadmapOpenOrSpawning = false;
            return;
        }

        roadmapInstance.OnClosedRoadmap += HandleRoadmapClosed;

        roadmapInstance.PrepareProgressBeforeReveal();
        roadmapInstance.Initialize();
        roadmapInstance.PlayIntro(false);

        ResetRoadmapRectTransform(roadmapInstance.transform);
    }

    private Popup_GalaxyRoadmap SpawnGalaxyRoadmap()
    {
        if (prefabLibrary == null || roadmapContainer == null)
        {
            Debug.LogWarning("[MainMenuUI] Missing prefabLibrary or roadmapContainer.");
            return null;
        }

        var prefab = prefabLibrary.GetGalaxyRoadmap(GALAXY_ROADMAP);

        if (prefab == null)
        {
            Debug.LogWarning("[MainMenuUI] GalaxyRoadmapPopup prefab not found.");
            return null;
        }

        return Instantiate(prefab, roadmapContainer);
    }

    private void HandleRoadmapClosed()
    {
        roadmapOpenOrSpawning = false;

        if (roadmapInstance != null)
            roadmapInstance.OnClosedRoadmap -= HandleRoadmapClosed;

        roadmapInstance = null;
    }

    private void ResetRoadmapRectTransform(Transform t)
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

    private void HandleUnlockPopupRequested(BallType type)
    {
        ShowBallUnlockPopup(type);
    }

    private void ShowBallUnlockPopup(BallType type)
    {
        if (prefabLibrary == null)
        {
            Debug.LogWarning(
                "[MainMenuUI] PrefabLibrary is not assigned."
            );

            return;
        }

        if (mainMenuOverlayContainer == null)
        {
            Debug.LogWarning(
                "[MainMenuUI] Popup container is not assigned."
            );

            return;
        }

        if (ballUnlockPopupInstance != null)
        {
            ballUnlockPopupInstance.Open(type);
            return;
        }

        BallUnlockPopup popupPrefab =
            prefabLibrary.GetBallUnlockPopup(
                BALL_UNLOCK_POPUP
            );

        if (popupPrefab == null)
            return;

        ballUnlockPopupInstance = Instantiate(
            popupPrefab,
            mainMenuOverlayContainer
        );

        ballUnlockPopupInstance.Closed +=
            HandleBallUnlockPopupClosed;

        ballUnlockPopupInstance.AnimalUnlocked +=
            HandleAnimalUnlocked;

        ballUnlockPopupInstance.Open(type);
    }

    private void HandleAnimalUnlocked(BallType type)
    {
        ballChoiceMenu?.RefreshAll();
    }

    private void HandleBallUnlockPopupClosed()
    {
        if (ballUnlockPopupInstance != null)
        {
            ballUnlockPopupInstance.Closed -=
                HandleBallUnlockPopupClosed;

            ballUnlockPopupInstance.AnimalUnlocked -=
                HandleAnimalUnlocked;
        }

        ballUnlockPopupInstance = null;
    }

    public void AE_MainMenuOutFinished()
    {
        if (!cacheReady)
            CacheSessionStartData();

        MergeLevelManager.SetLevel(cachedLevelNumber);

        PlayerProgress.OnLevelStarted(
            MergeLevelManager.CurrentGalaxyId,
            MergeLevelManager.CurrentLevelInGalaxy
        );

        BallEventManager.RaiseMainMenuPopupClosed();

        // Safe now: the visible Main Menu Out animation is finished.
        PopupNavigationSlider.Instance?.DestroyOtherTabPopups();

        PopupManager.Instance?.BeginSessionDeferred(
            cachedIsNewLevel,
            warmupFrames: 2,
            pendingRewardsOnly: true
        );

        Destroy(gameObject, 0.5f);
    }
}
