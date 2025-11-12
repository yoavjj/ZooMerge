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

        // ✅ Setup UI slider
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.value = 1f;

        healthTween = new HealthTween(this);
        UpdateHealthText();

        BallEventManager.OnEnemyHitWithScore += OnEnemyHitWithScore;
        BallEventManager.OnSessionStarted += ResetHealth;
        BallEventManager.OnEnemyAdvanced += () => StartCoroutine(DelayedResetHealth(10f)); // or however many seconds you want

        Debug.Log($"❤️ Initialized with {currentHealth} HP for Level {currentLevel.level}");
    }

    private void OnDestroy()
    {
        BallEventManager.OnEnemyHitWithScore -= OnEnemyHitWithScore;
        BallEventManager.OnSessionStarted -= ResetHealth;
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

                    if (MergeLevelManager.TryAdvanceEnemy())
                    {
                        Debug.Log("✅ Enemy defeated! Preparing next enemy...");
                        ShowEnemyTransitionMessage();
                    }
                    else
                    {
                        Debug.Log("🏁 All enemies defeated! Level complete.");
                        BallEventManager.RaiseGameOver(null, GameOverReason.Won);
                        BallEventManager.RaiseSessionWonAnimation();
                        BallEventManager.RaiseResetCounters();
                        MergeAttemptTracker.ClearAll();
                        BallRegistry.Clear();
                    }
                }
            });
    }

    private void ShowEnemyTransitionMessage()
    {
        // 1. ensure old enemy destroyed
        BallEventManager.RaiseEnemySessionEnded();

        // 2. show popup
        PopupManager.Instance?.ShowEnemyDefeatedMessage();

        // 3. delay then spawn next
        Invoke(nameof(StartNextEnemy), 1.5f);
    }

    private void StartNextEnemy()
    {
        // Off‐subscribe or guard to prevent multiple calls
        CancelInvoke(nameof(StartNextEnemy));  // just in case

        int nextEnemyId = MergeLevelManager.GetCurrentEnemyId();
        EnemySpawner.Instance?.ClearEnemy();
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

        // ✅ Hardcode the slider to always animate from 0.2 to 1
        float fromSlider = 0.2f;
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
            1.5f // 👈 slower refill animation
        );

        Debug.Log($"🔄 Health reset for Level {currentLevel.level} ({currentHealth} HP)");
    }

    private float NormalizeHealthToSlider(int health)
    {
        float minVisual = 0.2f;
        float normalized = Mathf.Clamp01((float)health / MergeLevelManager.GetCurrentEnemyHealth());
        return Mathf.Lerp(minVisual, 1f, normalized);
    }
}
