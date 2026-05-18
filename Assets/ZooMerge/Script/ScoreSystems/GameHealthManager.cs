using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static BallEventManager;
using System.Collections;

public class GameHealthManager : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private AnimationCurve sliderEase = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("DEBUG (Editor Only)")]
    [SerializeField] private bool debugOverrideHealth = false;
    [SerializeField] private int debugHealthValue = 1;

    private GameObject activeMissZoneInstance;

    private HealthTween healthTween;
    private int currentHealth;
    private bool sessionEnded = false;
    private bool isAnimatingToZero = false; // guard flag

    private void Start()
    {
        FirebaseInitializer.WaitForFirebase(
            onReady: InitializeHealth,
            onError: err => Debug.LogError($"Firebase error in GameHealthManager: {err}")
        );
    }

    private void InitializeHealth()
    {
        // ✅ Use current level data instead of global enemy_health
        var currentLevel = MergeLevelManager.GetCurrentLevel();
        currentHealth = MergeLevelManager.GetCurrentEnemyHealth();

#if UNITY_EDITOR
        if (debugOverrideHealth)
            currentHealth = Mathf.Max(0, debugHealthValue);
#endif

        // ✅ Setup UI slider
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.value = 1f;

        healthTween = new HealthTween(this);
        UpdateHealthText();

        BallEventManager.OnEnemyHitWithScore += OnEnemyHitWithScore;
        OnSessionStarted += ResetHealth;
        OnEnemyAdvanced += ResetHealth;

        Debug.Log($"❤️ Initialized with {currentHealth} HP for Galaxy {MergeLevelManager.CurrentGalaxyId} Level {MergeLevelManager.CurrentLevelInGalaxy}");
    }

    private void OnDestroy()
    {
        BallEventManager.OnEnemyHitWithScore -= OnEnemyHitWithScore;
        OnSessionStarted -= ResetHealth;
        OnEnemyAdvanced -= ResetHealth;
        //BallEventManager.OnEnemyAdvanced -= () => StartCoroutine(DelayedResetHealth(10f)); // or however many seconds you want

    }

    private void OnEnemyHitWithScore(GameObject enemy, int damage)
    {
        HandleEnemyHit(damage);
    }

    private void HandleEnemyHit(int damage)
    {
        // 🧠 Early exit if session ended or animating out
        if (sessionEnded || isAnimatingToZero) return;

        int previousHealth = currentHealth;
        float previousSlider = healthSlider.value;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        float toSlider = NormalizeHealthToSlider(currentHealth);

        // 🛑 Prevent further updates if about to reach zero
        if (currentHealth <= 0)
            isAnimatingToZero = true;

        healthTween.AnimateSliderAndText(
            healthSlider,
            healthText,
            previousSlider,
            toSlider,
            previousHealth,
            currentHealth,
            sliderEase,
            0.35f,
            onComplete: () =>
            {
                if (currentHealth <= 0 && !sessionEnded)
                {
                    sessionEnded = true;
                    isAnimatingToZero = false;

                    StartCoroutine(StopAndDestroyGameOverBallsAfterWin(3f));

                    if (MergeLevelManager.TryAdvanceEnemy())
                    {
                        // ✅ Save resume point immediately (same galaxy/level, next enemy)
                        PlayerProgress.CaptureFromManagers();

                        // ✅ analytics: mid-level completion (enemy segment finished)
                        AnalyticsEvents.MidLevelComplete(MergeLevelManager.CurrentEnemyIndex - 1);

                        Debug.Log("✅ Enemy defeated! Preparing next enemy...");
                        ShowEnemyTransitionMessage();
                    }
                    else
                    {
                        Debug.Log("🏁 All enemies defeated! Level complete.");

                        PlayerProgress.CaptureFromManagers();

                        // ✅ If a loss already happened, don't continue the win flow
                        if (BallEventManager.IsGameOver) return;

                        AnalyticsEvents.GalaxyLevelComplete();
                        CloudSaveManager.AddGalaxyLevelComplete();

                        MergeLevelManager.MarkLevelCompletePending();
                        BallEventManager.RaiseEnemySessionEnded();

                        BallEventManager.RaiseGameOver(null, GameOverReason.Won);
                        BallEventManager.RaiseSessionWonAnimation();
                    }
                }
            });
    }

    private IEnumerator StopAndDestroyGameOverBallsAfterWin(float delay)
    {
        // 1) stop countdown immediately
        var toDestroy = new System.Collections.Generic.List<BallInfo>();

        foreach (var ball in BallRegistry.ActiveBalls)
        {
            if (ball == null) continue;

            var dc = ball.DropController;
            if (dc != null && dc.IsTouchingGameOver)
            {
                dc.CancelGameOverCountdown();
                toDestroy.Add(ball);
            }
        }

        // 2) wait (so win popup/anim can play)
        yield return new WaitForSeconds(delay);

        // 3) destroy them
        foreach (var ball in toDestroy)
        {
            if (ball == null) continue;
            BallRegistry.Unregister(ball);
            Destroy(ball.gameObject);
        }
    }

    private void ShowEnemyTransitionMessage()
    {
        // 1. ONLY tracked enemy destroys
        BallEventManager.RaiseEnemySessionEnded();

        // 2. show popup
        //PopupManager.Instance?.ShowEnemyDefeatedMessage();

        // 3. delay then spawn next
        Invoke(nameof(StartNextEnemy), 1.5f);
    }

    private void StartNextEnemy()
    {
        // Off‐subscribe or guard to prevent multiple calls
        CancelInvoke(nameof(StartNextEnemy));  // just in case

        int nextEnemyId = MergeLevelManager.GetCurrentEnemyId();
    }

    private void UpdateHealthText()
    {
        if (healthText != null)
            healthText.text = currentHealth.ToString();
    }


    private IEnumerator DelayedResetHealth(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetHealth();
    }

    private void ResetHealth()
    {
        sessionEnded = false;
        isAnimatingToZero = false;

        // ✅ Get current enemy health
        var currentLevel = MergeLevelManager.GetCurrentLevel();
        currentHealth = MergeLevelManager.GetCurrentEnemyHealth();

#if UNITY_EDITOR
        if (debugOverrideHealth)
            currentHealth = Mathf.Max(0, debugHealthValue);
#endif

        // ✅ Hardcode the slider to always animate from 0.2 to 1
        float fromSlider = 0f;
        int fromHealth = Mathf.RoundToInt(currentHealth * fromSlider); // optional, could be 0

        float toSlider = 1f; // always fill to full

        healthTween.AnimateSliderAndText(
            healthSlider,
            healthText,
            fromSlider,
            toSlider,
            fromHealth,
            currentHealth,
            sliderEase,
            1.2f, // 👈 slower refill animation
                onComplete: () =>
    {
        // ✅ Destroy miss zone once slider finishes animating back to full
        if (activeMissZoneInstance != null)
        {
            Destroy(activeMissZoneInstance);
            activeMissZoneInstance = null;
            //Debug.Log("[GameHealthManager] MissZone prefab destroyed after health reset.");
        }
    }
        );

        Debug.Log($"🔄 Health reset for Galaxy {MergeLevelManager.CurrentGalaxyId} Level {MergeLevelManager.CurrentLevelInGalaxy} ({currentHealth} HP)");
    }

    private float NormalizeHealthToSlider(int health)
    {
        float minVisual = 0.2f;
        float normalized = Mathf.Clamp01((float)health / MergeLevelManager.GetCurrentEnemyHealth());
        return Mathf.Lerp(minVisual, 1f, normalized);
    }

    public void Debug_SetHpNow()
    {
        currentHealth = Mathf.Max(0, debugHealthValue);
        isAnimatingToZero = false;
        sessionEnded = false;

        UpdateHealthText();
        if (healthSlider != null)
            healthSlider.value = NormalizeHealthToSlider(currentHealth);
    }
}
