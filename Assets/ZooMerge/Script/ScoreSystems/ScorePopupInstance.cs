using System.Collections;
using TMPro;
using UnityEngine;

public class ScorePopupInstance : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Animator animator;

    [Header("Timing Sync")]
    [SerializeField, Range(0.5f, 1f)] private float outTriggerAtT = 0.9f;
    [SerializeField] private float hitAtT = 1.0f;

    [Header("Center Float")]
    [SerializeField] private float centerFloatAmplitudePx = 10f;
    [SerializeField] private float centerFloatFrequency = 2.2f;
    [SerializeField, Min(0f)] private float centerFloatRampIn = 0.08f;

    public TextMeshProUGUI Text => scoreText;
    public Animator Animator => animator;
    public Transform Transform => transform;

    public bool InUse { get; private set; }

    private System.Action<ScorePopupInstance> onComplete;

    public int PoolIndex { get; private set; }
    public void SetPoolIndex(int idx) => PoolIndex = idx;

    private Coroutine flyRoutine;
    private bool hasExited;
    private Camera cam;
    private Transform target;
    private GameObject enemyGO;

    [SerializeField] private float minZRotation = -15f;
    [SerializeField] private float maxZRotation = 15f;

    private float targetZRotation;
    private int popupScore;

    private bool outTriggered = false;

    private void OnDisable()
    {
        if (flyRoutine != null) StopCoroutine(flyRoutine);
        flyRoutine = null;
        InUse = false;
        hasExited = false;
        outTriggered = false;
    }

    public void Init(
        Vector3 screenStart,
        Vector3 centerScreen,
        Camera cam,
        Transform target,
        float toCenterDuration,
        AnimationCurve toCenterCurve,
        float centerHoldTime,
        float toTargetDuration,
        AnimationCurve toTargetCurve,
        System.Action<ScorePopupInstance> onComplete,
        float xRange,
        float yMin,
        float yMax,
        int score,
        GameObject enemy
    )
    {
        InUse = true;
        this.onComplete = onComplete;
        this.cam = cam;
        this.target = target;
        this.popupScore = score;
        this.enemyGO = enemy;

        hasExited = false;
        outTriggered = false;

        transform.rotation = Quaternion.identity;
        transform.position = screenStart;

        animator.SetTrigger("In");
        targetZRotation = Random.Range(minZRotation, maxZRotation);

        if (flyRoutine != null) StopCoroutine(flyRoutine);

        flyRoutine = StartCoroutine(Fly2Phase(
            screenStart,
            centerScreen,
            toCenterDuration,
            toCenterCurve,
            centerHoldTime,
            toTargetDuration,
            toTargetCurve,
            xRange,
            yMin,
            yMax
        ));
    }

    private IEnumerator Fly2Phase(
        Vector3 startScreen,
        Vector3 centerScreen,
        float toCenterDuration,
        AnimationCurve toCenterCurve,
        float centerHoldTime,
        float toTargetDuration,
        AnimationCurve toTargetCurve,
        float xRange,
        float yMin,
        float yMax
    )
    {
        // --------------------
        // Phase 1: fly to center
        // --------------------
        {
            Vector3 a = startScreen;
            Vector3 b = centerScreen;

            Vector3 c = (a + b) * 0.5f + new Vector3(
                Random.Range(-xRange * 0.25f, xRange * 0.25f),
                Random.Range(yMin * 0.25f, yMax * 0.25f),
                0f
            );

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.0001f, toCenterDuration);
                float k = toCenterCurve != null ? toCenterCurve.Evaluate(Mathf.Clamp01(t)) : Mathf.Clamp01(t);

                transform.position = Bezier(a, c, b, k);

                float currentZ = Mathf.Lerp(0f, targetZRotation * 0.35f, Mathf.Clamp01(t));
                transform.rotation = Quaternion.Euler(0f, 0f, currentZ);

                yield return null;
            }
        }

        // --------------------
        // Center hold: float only (smooth ramp in)
        // --------------------
        if (centerHoldTime > 0f)
        {
            Vector3 baseCenterPos = transform.position;

            float phase = Random.Range(0f, 100f);
            float elapsed = 0f;

            while (elapsed < centerHoldTime)
            {
                elapsed += Time.deltaTime;

                // ✅ ramp starts at 0 so no 1-frame snap
                float ramp = (centerFloatRampIn <= 0f)
                    ? 1f
                    : Mathf.Clamp01(elapsed / centerFloatRampIn);

                float wobbleY = Mathf.Sin((Time.time + phase) * centerFloatFrequency) * centerFloatAmplitudePx;
                float wobbleX = Mathf.Cos((Time.time * 1.35f + phase) * centerFloatFrequency) * (centerFloatAmplitudePx * 0.6f);

                transform.position = baseCenterPos + new Vector3(wobbleX, wobbleY, 0f) * ramp;

                yield return null;
            }

            // optional: snap back to base before phase 2 control point calc (keeps path clean)
            transform.position = baseCenterPos;
        }

        // --------------------
        // Phase 2: fast fly to target
        // --------------------
        Vector3 start = transform.position;
        Vector3 end = cam.WorldToScreenPoint(target.position);
        Vector3 control = start + new Vector3(
            Random.Range(-xRange, xRange),
            Random.Range(yMin, yMax),
            0f
        );

        float tt = 0f;
        while (tt < 1f)
        {
            tt += Time.deltaTime / Mathf.Max(0.0001f, toTargetDuration);
            float k = toTargetCurve != null ? toTargetCurve.Evaluate(Mathf.Clamp01(tt)) : Mathf.Clamp01(tt);

            transform.position = Bezier(start, control, end, k);

            float currentZ = Mathf.Lerp(targetZRotation * 0.35f, targetZRotation, Mathf.Clamp01(tt));
            transform.rotation = Quaternion.Euler(0f, 0f, currentZ);

            if (!outTriggered && tt >= outTriggerAtT)
            {
                outTriggered = true;
                animator.SetTrigger("Out");
            }

            if (!hasExited && tt >= hitAtT)
            {
                hasExited = true;

                if (enemyGO != null)
                    BallEventManager.RaiseEnemyHitWithDamage(enemyGO, popupScore);
                else
                    Debug.LogWarning("[ScorePopupInstance] enemyGO is null, cannot apply damage.");

                StartCoroutine(ReturnAfterDelay(1f));
                break;
            }

            yield return null;
        }

        flyRoutine = null;
    }

    private Vector3 Bezier(Vector3 a, Vector3 c, Vector3 b, float t)
    {
        return Vector3.Lerp(Vector3.Lerp(a, c, t), Vector3.Lerp(c, b, t), t);
    }

    private IEnumerator ReturnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        InUse = false;
        onComplete?.Invoke(this);
    }

}
