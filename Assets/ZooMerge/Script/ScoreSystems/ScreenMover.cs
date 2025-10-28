using System.Collections;
using UnityEngine;

public class ScreenMover
{
    private readonly Transform targetTransform;
    private readonly Camera camera;
    private readonly float duration;
    private readonly AnimationCurve curve;
    private readonly MonoBehaviour coroutineHost;

    private float curveStrength;
    private float verticalBias;
    private bool randomizeDirection;

    private Coroutine currentRoutine;

    public ScreenMover(
        Transform targetTransform,
        Camera camera,
        float duration,
        AnimationCurve curve,
        MonoBehaviour coroutineHost)
    {
        this.targetTransform = targetTransform;
        this.camera = camera;
        this.duration = Mathf.Max(0.01f, duration);
        this.curve = curve != null ? curve : AnimationCurve.Linear(0, 0, 1, 1);
        this.coroutineHost = coroutineHost;
    }

    public void Configure(float curveStrength, float verticalBias, bool randomizeDirection)
    {
        this.curveStrength = curveStrength;
        this.verticalBias = verticalBias;
        this.randomizeDirection = randomizeDirection;
    }

    public void Start(Vector3 worldStart, Vector3 worldEnd, float holdDuration = 0f)
    {
        Stop();

        Vector3 screenStart = camera.WorldToScreenPoint(worldStart);
        Vector3 screenEnd = camera.WorldToScreenPoint(worldEnd);
        Vector3 controlPoint = GetControlPoint(screenStart, screenEnd);

        currentRoutine = coroutineHost.StartCoroutine(Sequence(screenStart, controlPoint, screenEnd, holdDuration));
    }

    private IEnumerator Sequence(Vector3 start, Vector3 control, Vector3 end, float holdDuration)
    {
        targetTransform.position = start;

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        yield return MoveAlongCurve(start, control, end);
    }

    public void Stop()
    {
        if (currentRoutine != null)
        {
            coroutineHost.StopCoroutine(currentRoutine);
            currentRoutine = null;
        }
    }

    private Vector3 GetControlPoint(Vector3 start, Vector3 end)
    {
        Vector3 mid = (start + end) * 0.5f;

        Vector2 dir = (end - start).normalized;
        Vector2 perpendicular = new Vector2(-dir.y, dir.x);

        // Direction flip
        float dirSign = randomizeDirection && Random.value > 0.5f ? -1f : 1f;

        // Scale offset based on screen size and strength
        float maxOffset = Mathf.Min(Screen.width, Screen.height) * curveStrength;
        Vector3 offset = (Vector3)(perpendicular * maxOffset * dirSign);

        // Add optional vertical bias
        offset.y += Screen.height * verticalBias;

        Vector3 point = mid + offset;

        // Clamp within screen bounds
        point.x = Mathf.Clamp(point.x, 0f, Screen.width);
        point.y = Mathf.Clamp(point.y, 0f, Screen.height);

        return point;
    }

    private IEnumerator MoveAlongCurve(Vector3 start, Vector3 control, Vector3 end)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(elapsed / duration);

            // Quadratic Bézier
            Vector3 pos =
                Mathf.Pow(1 - t, 2) * start +
                2 * (1 - t) * t * control +
                Mathf.Pow(t, 2) * end;

            targetTransform.position = pos;
            yield return null;
        }

        targetTransform.position = end;
        currentRoutine = null;
    }
}