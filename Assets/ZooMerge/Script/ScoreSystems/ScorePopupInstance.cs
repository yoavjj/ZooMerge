using System.Collections;
using TMPro;
using UnityEngine;

public class ScorePopupInstance : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Animator animator;

    public TextMeshProUGUI Text => scoreText;
    public Animator Animator => animator;
    public Transform Transform => transform;

    private System.Action<ScorePopupInstance> onComplete;

    private Coroutine flyRoutine;
    private bool hasExited;
    private Camera cam;
    private Transform target;

    [SerializeField] private float minZRotation = -15f;
    [SerializeField] private float maxZRotation = 15f;

    private float targetZRotation;

    // 🔹 Cache the score value when the popup is initialized
    private int popupScore;

    public void Init(
        Vector3 screenStart,
        Camera cam,
        Transform target,
        float duration,
        float holdTime,
        AnimationCurve curve,
        System.Action<ScorePopupInstance> onComplete,
        float xRange,
        float yMin,
        float yMax,
        int score  // 👈 pass in the score value
    )
    {
        this.onComplete = onComplete;
        this.cam = cam;
        this.target = target;
        this.popupScore = score; // ✅ store score for collision event
        hasExited = false;

        // ✅ Reset transform rotation
        transform.rotation = Quaternion.identity;

        // ✅ Reset position
        transform.position = screenStart;

        // Start the "In" animation
        animator.SetTrigger("In");

        // Pick new random tilt for this flight
        targetZRotation = Random.Range(minZRotation, maxZRotation);

        // Restart coroutine safely
        if (flyRoutine != null)
            StopCoroutine(flyRoutine);

        flyRoutine = StartCoroutine(Fly(duration, holdTime, curve, xRange, yMin, yMax));
    }

    private IEnumerator Fly(
        float duration, float holdTime, AnimationCurve curve,
        float xRange, float yMin, float yMax)
    {
        yield return new WaitForSeconds(holdTime);

        Vector3 start = transform.position;
        Vector3 end = cam.WorldToScreenPoint(target.position);
        Vector3 control = start + new Vector3(Random.Range(-xRange, xRange), Random.Range(yMin, yMax), 0f);

        float t = 0f;
        float exitSpeedMultiplier = 1f;

        while (t < 1f)
        {
            t += Time.deltaTime / (duration * exitSpeedMultiplier);
            float k = curve.Evaluate(t);
            Vector3 pos = Bezier(start, control, end, k);
            transform.position = pos;

            float currentZ = Mathf.Lerp(0f, targetZRotation, t); // ✅ Smooth tilt
            transform.rotation = Quaternion.Euler(0f, 0f, currentZ);

            if (hasExited && exitSpeedMultiplier < 2f)
            {
                exitSpeedMultiplier += Time.deltaTime * 1.5f;
            }

            yield return null;
        }

        flyRoutine = null;
    }

    private Vector3 Bezier(Vector3 a, Vector3 c, Vector3 b, float t)
    {
        return Vector3.Lerp(Vector3.Lerp(a, c, t), Vector3.Lerp(c, b, t), t);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExited) return;

        if (other.CompareTag("Enemy"))
        {
            hasExited = true;
            BallEventManager.RaiseEnemyHitWithDamage(other.gameObject, popupScore);
            animator.SetTrigger("Out");
            StartCoroutine(ReturnAfterDelay(1f));
        }
        else if (other.CompareTag("MissZone"))
        {
            Debug.Log("🟡 Score popup missed — no enemy hit. Returning to pool.");
            hasExited = true;
            animator.SetTrigger("Out");
            StartCoroutine(ReturnAfterDelay(1f));
        }
    }

    private IEnumerator ReturnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        onComplete?.Invoke(this); // ✅ Return to pool only here
    }
}
