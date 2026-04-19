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

    [SerializeField] private LevelArtController levelArtController;

    [Header("Out Animation Warmup")]
    [SerializeField, Range(1, 100)] private int outWarmupFrames = 2;
    private bool playLocked = false;

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
        yield return new WaitUntil(() => FirebaseInitializer.IsReady);

        topBarMenu.BuildCoinUI();
        topBarMenu.BuildAllBallTypesUI();

        // ✅ Cache session data ahead of time (avoids jank on click)
        CacheSessionStartData();

        levelArtController?.Refresh(); 

        yield return new WaitForSeconds(1f); // just to let the main menu settle visually before we do more work

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
        if (playLocked) return;
        playLocked = true;

        if (playButton != null)
            playButton.interactable = false; // prevents spam + extra work

        mainMenuAnimator.SetTrigger("Out");

        if (playPressedRoutine != null)
            StopCoroutine(playPressedRoutine);

        playPressedRoutine = StartCoroutine(PlayPressedRoutine());
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

        PopupManager.Instance?.BeginSession(cachedIsNewLevel);
        PopupManager.Instance?.InitializeProgressBarNow();

        Destroy(gameObject, 2.5f);
    }
}
