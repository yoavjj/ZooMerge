using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHealthManager : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private AnimationCurve sliderEase = AnimationCurve.Linear(0, 0, 1, 1);


    private HealthTween healthTween;

    private int currentHealth;
    private bool sessionEnded = false;
    private bool isAnimatingToZero = false; // 🔸 New guard flag

    private void Start()
    {
        FirebaseInitializer.WaitForFirebase(
            onReady: InitializeHealth,
            onError: err => Debug.LogError($"Firebase error in GameHealthManager: {err}")
        );
    }

    private void InitializeHealth()
    {
        currentHealth = FirebaseInitializer.MergeScoreData.enemy_health;

        // ✅ Keep slider normalized between 0–1
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.value = 1f; // start full

        healthTween = new HealthTween(this);

        UpdateHealthText();

        BallEventManager.OnEnemyHitWithScore += HandleEnemyHit;
        BallEventManager.OnSessionStarted += ResetHealth;

        Debug.Log($"❤️ GameHealthManager initialized with {currentHealth} HP");
    }

    private void OnDestroy()
    {
        BallEventManager.OnEnemyHitWithScore -= HandleEnemyHit;
        BallEventManager.OnSessionStarted -= ResetHealth;
    }

    private void HandleEnemyHit(int damage)
    {
        // 🧠 Early-out if session is already ending or ended
        if (sessionEnded || isAnimatingToZero) return;

        int previousHealth = currentHealth;
        float previousSlider = healthSlider.value;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        float toSlider = NormalizeHealthToSlider(currentHealth);

        // 🛑 If this hit will drop health to 0 — lock future hits immediately
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
                    BallEventManager.RaiseGameOver(null);
                    BallEventManager.RaiseSessionWonAnimation();
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

        currentHealth = FirebaseInitializer.MergeScoreData.enemy_health;
        float toSlider = NormalizeHealthToSlider(currentHealth);

        healthTween.AnimateSliderAndText(
            healthSlider,
            healthText,
            fromSlider,
            toSlider,
            fromHealth,
            currentHealth,
            sliderEase // ✅ Apply easing curve
        );
    }

    private float NormalizeHealthToSlider(int health)
    {
        float minVisual = 0.2f; // 20% visible minimum
        float normalized = Mathf.Clamp01((float)health / FirebaseInitializer.MergeScoreData.enemy_health);
        return Mathf.Lerp(minVisual, 1f, normalized); // remap from [0–1] → [0.2–1]
    }
}
