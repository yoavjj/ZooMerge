using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FlyingCollectible : BaseFlyingCollectible
{
    [SerializeField] private Image iconImage;
    bool triggeredArrival = false;
    [SerializeField] private float earlyArrivalDistance = 20f;

    [Header("Momentum Anticipation")]
    [SerializeField, Tooltip("How far it dips down before launch (in Y)")]
    private float anticipationOffsetY = -20f;

    [SerializeField, Tooltip("How much it rotates in Z during anticipation")]
    private float anticipationZRotation = 15f;

    [SerializeField, Tooltip("How long the anticipation animation takes (seconds)")]
    private float anticipationDuration = 0.12f;

    [SerializeField, Tooltip("How long the stretch animation takes (seconds)")]
    private float stretchDuration = 0.12f;

    [SerializeField, Tooltip("Should rotation be randomized (positive/negative)?")]
    private bool randomizeRotationDirection = true;

    [SerializeField, Tooltip("Easing curve for anticipation effect")]
    private AnimationCurve anticipationCurve = null;

    private RectTransform rect;
    private Coroutine flyRoutine;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public override RectTransform Rect => rect;

    /// <summary>
    /// Assign the icon sprite (call this right after Instantiate)
    /// </summary>
    public override void SetIcon(Sprite sprite)
    {
        if (iconImage != null)
            iconImage.sprite = sprite;
    }

    public Sprite GetIcon()
    {
        return iconImage != null ? iconImage.sprite : null;
    }

    public override void LaunchToLocalPoint(
        Vector2 targetLocalPosition,
        float totalDuration,
        System.Action onArrive,
        float delay = 0f,
        float arcHeight = 100f,
        float holdDuration = 0.2f,
        AnimationCurve easeInCurve = null,
        AnimationCurve easeOutCurve = null)
    {
        if (flyRoutine != null)
            StopCoroutine(flyRoutine);

        flyRoutine = StartCoroutine(
            FlyRoutine(targetLocalPosition, totalDuration, onArrive, delay, arcHeight, holdDuration, easeInCurve, easeOutCurve)
        );
    }

    private IEnumerator FlyRoutine(
        Vector2 targetLocal,
        float totalDuration,
        System.Action onArrive,
        float delay,
        float arcHeight,
        float holdDuration,
        AnimationCurve easeIn,
        AnimationCurve easeOut)
    {
        Vector2 start = rect.anchoredPosition;

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        // ⏸️ Step 1: Hold in place before moving
        yield return new WaitForSecondsRealtime(holdDuration);

        // 🎯 Step 1.1: Momentum anticipation (sling-like effect)

        // 🎯 Step 1.1: Momentum anticipation (slingshot-like easing IN)

        float anticipationTime = anticipationDuration;
        float anticipationZ = anticipationZRotation;
        if (randomizeRotationDirection)
            anticipationZ *= Random.value < 0.5f ? -1f : 1f;

        Vector2 originalPos = rect.anchoredPosition;
        Quaternion originalRot = rect.localRotation;

        Vector2 targetAnticipationPos = originalPos + Vector2.up * anticipationOffsetY;
        Quaternion targetAnticipationRot = Quaternion.Euler(0f, 0f, anticipationZ);

        float tA = 0f;
        while (tA < anticipationTime)
        {
            tA += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(tA / anticipationTime);

            float eased = anticipationCurve != null
                ? anticipationCurve.Evaluate(p)
                : Mathf.Sin(p * Mathf.PI * 0.5f); // default: sine-out

            rect.anchoredPosition = Vector2.Lerp(originalPos, targetAnticipationPos, eased);
            rect.localRotation = Quaternion.Lerp(originalRot, targetAnticipationRot, eased);

            yield return null;
        }

        // ⏸ Hold in anticipation pose until flight starts
        yield return new WaitForSecondsRealtime(stretchDuration);

        // 🔁 Snap back before flight
        rect.anchoredPosition = originalPos;
        rect.localRotation = originalRot;

        // 🔊 Play one random woosh exactly when flight begins
        PlayRandomWooshSfx();

        // ⏱️ Step 2: Calculate available time for the flight phase
        float flightDuration = totalDuration - holdDuration;
        if (flightDuration <= 0f)
        {
            rect.anchoredPosition = targetLocal;
            onArrive?.Invoke();
            Destroy(gameObject);
            yield break;
        }

        // ✨ Bezier control point
        Vector2 mid = (start + targetLocal) * 0.5f;
        mid.y += arcHeight;

        float t = 0f;
        while (t < flightDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / flightDuration);

            float easeInValue = easeIn?.Evaluate(progress) ?? progress;
            float easeOutValue = easeOut?.Evaluate(progress) ?? progress;
            float easeValue = Mathf.Lerp(easeInValue, easeOutValue, progress);

            // Bezier curve
            Vector2 a = Vector2.Lerp(start, mid, easeValue);
            Vector2 b = Vector2.Lerp(mid, targetLocal, easeValue);
            Vector2 pos = Vector2.Lerp(a, b, easeValue);

            rect.anchoredPosition = pos;

            // ✅ Early arrival trigger
            float distanceToTarget = Vector2.Distance(pos, targetLocal);
            if (!triggeredArrival && distanceToTarget <= earlyArrivalDistance)
            {
                triggeredArrival = true;

                // Immediately notify that it "arrived"
                onArrive?.Invoke();

                // Destroy early to avoid that awkward pause
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }

        rect.anchoredPosition = targetLocal;
        onArrive?.Invoke();
        flyRoutine = null;
        Destroy(gameObject);
    }
}
