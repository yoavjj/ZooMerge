using System.Collections;
using UnityEngine;
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

    [Header("seesion start Reveal")]
    [SerializeField, Min(0f)] private float playPressedDelay = 0.25f;
    private Coroutine playPressedRoutine;

    [SerializeField] private LevelArtController levelArtController;

    [Header("Out Animation Warmup")]
    [SerializeField, Range(1, 100)] private int outWarmupFrames = 2;
    private bool playLocked = false;

    private int cachedLevelNumber;
    private bool cachedIsNewLevel;
    private bool cacheReady = false;

    [Header("Out Of Tries Popup (Main Menu)")]
    [SerializeField] private PrefabLibrary prefabLibrary;
    [SerializeField] private Transform outOfTriesContainer;
    private GameObject outOfTriesInstance;
    private const string OUT_OF_TRIES_POPUP = "OutOfTriesPopup";

    [Header("Galaxy Roadmap Popup")]
    [SerializeField] private Transform roadmapContainer;

    private Popup_GalaxyRoadmap roadmapInstance;
    private bool roadmapOpenOrSpawning = false;

    private const string GALAXY_ROADMAP = "GalaxyRoadmapPopup";


    private bool IsOutOfTriesPopupOpen => outOfTriesInstance != null;

    [SerializeField] private CollectibleFlyTarget heartFlyTarget; // target on your UI (tries/heart icon)
    [SerializeField] private string heartMenuEntryId = "Heart_menu"; // collectible fly entry for retry reward (optional)
    [SerializeField] private int heartMenuAmount = 1;

    private void Awake()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayPressed);
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

        if (roadmapInstance != null)
        {
            roadmapInstance.OnClosedRoadmap -= HandleRoadmapClosed;
            Destroy(roadmapInstance.gameObject);
        }
    }

    private void HandleRetriesPurchasedFromPopup()
    {
        // The popup is closing, so allow another popup later if needed
        outOfTriesInstance = null;

        // Start the heart reward fly.
        // The retry will be added when the heart reaches the target
        // and your existing animation event calls AE_AddArriveAmountToText().
        FlyHeartMenu();

        // Allow pressing Play again
        playLocked = false;

        if (playButton != null)
            playButton.interactable = true;
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

        yield return new WaitForSeconds(1f);

        PopupManager.Instance?.WarmupSession();
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

        if (!IsOutOfTriesPopupOpen &&
            PlayerProgress.HasRetryLimitForCurrentLevel() &&
            PlayerProgress.CurrentLevelRetriesRemaining() <= 0)
        {
            PlayUiSfx(SfxCue.ButtonClickNegative);
            ShowOutOfTriesPopupFromMainMenu();
            return;
        }

        PlayUiSfx(SfxCue.ButtonClick);

        playLocked = true;

        if (playButton != null)
            playButton.interactable = false;

        CloudSaveManager.StartPlayTimer();

        BallEventManager.RaiseMainMenuPopupClosed();
        PopupNavigationSlider.Instance?.DestroyOtherTabPopups();

        mainMenuAnimator.SetTrigger("Out");

        if (playPressedRoutine != null)
            StopCoroutine(playPressedRoutine);

        AnalyticsEvents.MainMenuExit("play_pressed");

        playPressedRoutine = StartCoroutine(
            PlayPressedRoutine()
        );
    }

    private void ShowOutOfTriesPopupFromMainMenu()
    {
        if (IsOutOfTriesPopupOpen) return;

        PlayUiSfx(SfxCue.ButtonClick);

        if (prefabLibrary == null || outOfTriesContainer == null)
        {
            Debug.LogWarning("[MainMenuUI] Missing prefabLibrary or outOfTriesContainer.");
            return;
        }

        var prefab = prefabLibrary.GetRaw(OUT_OF_TRIES_POPUP);
        if (prefab == null) return;

        outOfTriesInstance = Instantiate(prefab, outOfTriesContainer);

        // ✅ Main-menu context => HIDE quit button to prevent crash flow
        OutOfTriesPopup.LastSpawned?.SetQuitButtonVisible(false);
    }

    private IEnumerator PlayPressedRoutine()
    {
        // ✅ Let the Out animation actually start rendering before heavy work
        int frames = Mathf.Clamp(outWarmupFrames, 1, 5);
        for (int i = 0; i < frames; i++)
            yield return null;

        // (Optional) tiny real-time slice helps on some devices
        // yield return new WaitForSecondsRealtime(0.02f);

        // ✅ Ensure cache is ready (cheap)
        if (!cacheReady)
            CacheSessionStartData();

        // ✅ Do the heavy stuff AFTER animation has started
        MergeLevelManager.SetLevel(cachedLevelNumber);

        // ✅ checkpoint/retry state for this level start
        PlayerProgress.OnLevelStarted(MergeLevelManager.CurrentGalaxyId, MergeLevelManager.CurrentLevelInGalaxy);

        PopupManager.Instance?.BeginSession(cachedIsNewLevel);
        PopupManager.Instance?.InitializeProgressBarNow();

        Destroy(gameObject, 0.65f);
    }

    public void ForceRefreshProgressUIAndCache()
    {
        // Refresh any level art / display
        levelArtController?.Refresh();

        // Re-cache which level should start when Play is pressed
        CacheSessionStartData();
    }

    public void FlyHeartMenu()
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

        // Use default spawn container (pass null)
        CollectibleFlyService.Instance.Fly(heartMenuEntryId, heartMenuAmount, heartFlyTarget, null);
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
}
