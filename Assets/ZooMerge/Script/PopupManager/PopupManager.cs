using System.Collections;
using System.Linq;
using UnityEngine;
using static BallEventManager;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject mainMenuPopupPrefab;
    [SerializeField] private GameObject winLosePopupPrefab;
    [SerializeField] private GameObject pauseRestartPopupPrefab;
    [SerializeField] private BallSpawner ballSpawner;
    [SerializeField] LevelProgressBarSlider levelProgressBarSlider;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float winLosePopupDelay = 0.5f;

    private Coroutine winLosePopupRoutine;

    private GameObject pauseRestartPopupInstance;
    private GameObject mainMenuPopupInstance;
    private GameObject gameUIPopupInstance;

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
        if (mainMenuPopupPrefab != null)
        {
            mainMenuPopupInstance = Instantiate(mainMenuPopupPrefab, transform);
            mainMenuPopupInstance.SetActive(true);
        }
    }

    private void OnEnable()
    {
        BallEventManager.OnGameOver += ShowEndPopup;
    }

    private void OnDisable()
    {
        BallEventManager.OnGameOver -= ShowEndPopup;
        if (winLosePopupRoutine != null) { StopCoroutine(winLosePopupRoutine); winLosePopupRoutine = null; }
    }

    public void ShowPauseRestartPopup()
    {
        if (pauseRestartPopupInstance == null)
        {
            pauseRestartPopupInstance = Instantiate(pauseRestartPopupPrefab, transform);
        }

        pauseRestartPopupInstance.SetActive(true);
    }

    public void ClearPausePopupReference()
    {
        pauseRestartPopupInstance = null;
    }

    private void ShowEndPopup(BallInfo info, GameOverReason reason)
    {
        if (winLosePopupRoutine != null) StopCoroutine(winLosePopupRoutine);
        winLosePopupRoutine = StartCoroutine(ShowWinLosePopupAfterDelay(winLosePopupDelay, () =>
        {
            PopupMessageCenter.ShowEndPopupMessage(WinLosePopup.Instance, reason);
        }));
    }

    public void ShowEnemyDefeatedMessage()
    {
        if (winLosePopupRoutine != null) StopCoroutine(winLosePopupRoutine);
        winLosePopupRoutine = StartCoroutine(ShowWinLosePopupAfterDelay(winLosePopupDelay, () =>
        {
            PopupMessageCenter.ShowEnemyDefeated(WinLosePopup.Instance);
        }));
    }

    private IEnumerator ShowWinLosePopupAfterDelay(float delay, System.Action showBody)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (gameUIPopupInstance == null)
            gameUIPopupInstance = Instantiate(winLosePopupPrefab, transform);

        showBody?.Invoke();
        winLosePopupRoutine = null;
    }

    public void ShowMainMenu()
    {
        if (mainMenuPopupPrefab != null)
        {
            mainMenuPopupInstance = Instantiate(mainMenuPopupPrefab, transform);
            mainMenuPopupInstance.SetActive(true);
        }
    }

    public void BeginSession(bool isNewLevel, bool restartmidlevel = false)
    {
        AdManager.Instance?.LoadBanner();

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

        if (restartmidlevel)
        {
            EnemySpawner.Instance?.ClearEnemy();
        }

        // ✅ Save state immediately after new session starts
        BallStateSaver.Instance.SaveState(BallRegistry.ActiveBalls.ToArray());
        BallEventManager.ResetMidLevelLossFlag();
        StartCoroutine(PromoteNextFrame());
    }

    private IEnumerator PromoteNextFrame()
    {
        yield return null; // wait one frame (UI, layout, etc.)
        CircleDragInput.Instance?.spawner?.PromoteFromPreview();
    }

    public void ConfirmReturnToMainMenu()
    {
        BallStateSaver.Instance.Clear();

        CircleDragInput.Instance?.ClearSpawnContainer(); // Clear active ball
        BallEventManager.RaiseReturnToMainMenu(); // Destroys all balls
        BallEventManager.RaiseResetCounters();    // Resets UI counters
        EnemySpawner.Instance?.ClearEnemy();      // Clears current enemy
        AdManager.Instance?.HideBanner();         // Hide ads
        levelProgressBarSlider?.RestartVisuals(); // Reset progress bar visuals

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

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            TryShowPausePopupFromSystem();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            TryShowPausePopupFromSystem();
        }
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
}

