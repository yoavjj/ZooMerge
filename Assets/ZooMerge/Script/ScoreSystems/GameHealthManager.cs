using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static BallEventManager;

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
        currentHealth = currentLevel.enemy_health;

        // ✅ Setup UI slider
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.value = 1f;

        healthTween = new HealthTween(this);
        UpdateHealthText();

        BallEventManager.OnEnemyHitWithScore += HandleEnemyHit;
        BallEventManager.OnSessionStarted += ResetHealth;

        Debug.Log($"❤️ Initialized with {currentHealth} HP for Level {currentLevel.level}");
    }

    private void OnDestroy()
    {
        BallEventManager.OnEnemyHitWithScore -= HandleEnemyHit;
        BallEventManager.OnSessionStarted -= ResetHealth;
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
            onComplete: () =>
            {
                if (currentHealth <= 0 && !sessionEnded)
                {
                    sessionEnded = true;
                    isAnimatingToZero = false;

                    BallEventManager.RaiseGameOver(null, GameOverReason.Won);
                    BallEventManager.RaiseSessionWonAnimation();

                    // ✅ Advance to the next level when session is won
                    MergeLevelManager.AdvanceLevel();
                    Debug.Log($"🏆 Level completed! Moving to Level {MergeLevelManager.CurrentLevelNumber}");
                }
            });
    }

    private void UpdateHealthText()
    {
        if (healthText != null)
            healthText.text = currentHealth.ToString();
    }

    private void ResetHealth()
    {
        sessionEnded = false;
        isAnimatingToZero = false;

        int fromHealth = currentHealth;
        float fromSlider = healthSlider.value;

        // ✅ Get health from current level (not hardcoded)
        var currentLevel = MergeLevelManager.GetCurrentLevel();
        currentHealth = currentLevel.enemy_health;

        float toSlider = NormalizeHealthToSlider(currentHealth);

        healthTween.AnimateSliderAndText(
            healthSlider,
            healthText,
            fromSlider,
            toSlider,
            fromHealth,
            currentHealth,
            sliderEase
        );

        Debug.Log($"🔄 Health reset for Level {currentLevel.level} ({currentHealth} HP)");
    }

    private float NormalizeHealthToSlider(int health)
    {
        float minVisual = 0.2f;
        var currentLevel = MergeLevelManager.GetCurrentLevel();
        float normalized = Mathf.Clamp01((float)health / currentLevel.enemy_health);
        return Mathf.Lerp(minVisual, 1f, normalized);
    }
}
