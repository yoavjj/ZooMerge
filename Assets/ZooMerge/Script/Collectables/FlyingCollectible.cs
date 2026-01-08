using System.Collections;
using UnityEngine;

public class FlyingCollectible : MonoBehaviour
{
    private RectTransform rect;
    private Coroutine flyRoutine;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public RectTransform Rect => rect;

    public void LaunchToLocalPoint(
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

            // Ease-in at beginning, ease-out at end
            float easeInValue = easeIn?.Evaluate(progress) ?? progress;
            float easeOutValue = easeOut?.Evaluate(progress) ?? progress;

            // Blend both curves (ease in, ease out) – stronger start & smooth end
            float easeValue = Mathf.Lerp(easeInValue, easeOutValue, progress);

            // Bezier curve
            Vector2 a = Vector2.Lerp(start, mid, easeValue);
            Vector2 b = Vector2.Lerp(mid, targetLocal, easeValue);
            Vector2 pos = Vector2.Lerp(a, b, easeValue);

            rect.anchoredPosition = pos;
            yield return null;
        }

        rect.anchoredPosition = targetLocal;
        onArrive?.Invoke();
        flyRoutine = null;
        Destroy(gameObject);
    }
}
