using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] private TopBarMenu topBarMenu;

    [Header("UI Buttons")]
    [SerializeField] private Button playButton;

    [SerializeField] Animator mainMenuAnimator;

    [Header("seesion start Reveal")]
    [SerializeField, Min(0f)] private float playPressedDelay = 0.25f;
    private Coroutine playPressedRoutine;

    private int cachedLevelNumber;
    private bool cachedIsNewLevel;
    private bool cacheReady = false;

    private void Awake()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayPressed);
    }

    private void Start()
    {
        StartCoroutine(BuildTopBarWhenReady());
    }

    private void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayPressed);
    }

    private IEnumerator BuildTopBarWhenReady()
    {
        yield return new WaitUntil(() => GameInventory.Instance != null && MergeSessionTracker.Instance != null);

        topBarMenu.BuildCoinUI();
        topBarMenu.BuildAllBallTypesUI();

        // ✅ Cache session data ahead of time (avoids jank on click)
        CacheSessionStartData();
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
        if (playPressedRoutine != null) return; // prevent double taps

        mainMenuAnimator.SetTrigger("Out");

        playPressedRoutine = StartCoroutine(PlayPressedRoutine());
    }

    private IEnumerator PlayPressedRoutine()
    {
        if (playPressedDelay > 0f)
            yield return new WaitForSeconds(playPressedDelay);

        // ✅ If for some reason cache wasn't ready, compute now as fallback
        if (!cacheReady)
            CacheSessionStartData();

        // Inform progression system (now cheap)
        MergeLevelManager.SetLevel(cachedLevelNumber);

        // Centralized session begin
        PopupManager.Instance?.BeginSession(cachedIsNewLevel);

        // Initialize the progress bar
        PopupManager.Instance?.InitializeProgressBarNow();

        Destroy(gameObject, 2.5f);
    }
}
