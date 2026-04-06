using System.Collections;
using UnityEngine;

/// <summary>
/// Spins a target around Z. Starts on session start, and stops smoothly on session end/game over.
/// </summary>
public class VortexSpinController : MonoBehaviour
{
    [Header("Target (optional)")]
    [Tooltip("If null, spins this transform.")]
    [SerializeField] private Transform target;

    [Header("Spin Settings")]
    [Min(0.01f)]
    [SerializeField] private float secondsPerRevolution = 90f;

    [SerializeField] private bool clockwise = false;

    [Tooltip("If true, uses unscaled time (keeps spinning even when Time.timeScale = 0).")]
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Stop Behavior")]
    [Tooltip("Seconds to ease to a full stop when StopSpin is called.")]
    [Min(0f)]
    [SerializeField] private float stopEaseDuration = 0.35f;

    [Tooltip("Easing curve for stop. X=time(0-1), Y=speed multiplier(1->0).")]
    [SerializeField]
    private AnimationCurve stopEaseCurve =
        new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));

    private Coroutine spinRoutine;
    private float currentAngle;

    // Current spin state
    private float currentDegPerSec;

    // Stop state
    private bool stopping;
    private float stopStartDegPerSec;
    private float stopT;

    private void Awake()
    {
        if (target == null) target = transform;
        currentAngle = target.localEulerAngles.z;
    }

    private void OnEnable()
    {
        BallEventManager.OnSessionStarted += HandleSessionStarted;
        BallEventManager.OnEnemySessionEnded += StopSpin;
        BallEventManager.OnGameOverAnimation += StopSpin;
        BallEventManager.OnReturnToMainMenu += StopSpin;
    }

    private void OnDisable()
    {
        BallEventManager.OnSessionStarted -= HandleSessionStarted;
        BallEventManager.OnEnemySessionEnded -= StopSpin;
        BallEventManager.OnGameOverAnimation -= StopSpin;
        BallEventManager.OnReturnToMainMenu -= StopSpin;

        // Hard stop on disable
        if (spinRoutine != null) StopCoroutine(spinRoutine);
        spinRoutine = null;
        stopping = false;
    }

    private void HandleSessionStarted()
    {
        StartSpin();
    }

    public void StartSpin()
    {
        float dir = clockwise ? -1f : 1f;
        float baseDegPerSec = 360f / Mathf.Max(0.01f, secondsPerRevolution);
        currentDegPerSec = dir * baseDegPerSec;

        // cancel any stop easing and ensure coroutine is running
        stopping = false;

        if (spinRoutine == null)
            spinRoutine = StartCoroutine(SpinLoop());
    }

    public void StopSpin()
    {
        // If not spinning, nothing to do
        if (spinRoutine == null) return;

        // Instant stop if duration is 0
        if (stopEaseDuration <= 0f)
        {
            StopCoroutine(spinRoutine);
            spinRoutine = null;
            stopping = false;
            return;
        }

        // Begin easing down to zero
        stopping = true;
        stopStartDegPerSec = currentDegPerSec;
        stopT = 0f;
    }

    private IEnumerator SpinLoop()
    {
        while (true)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            // If we are easing out, reduce speed smoothly to 0
            if (stopping)
            {
                stopT += dt / Mathf.Max(0.0001f, stopEaseDuration);
                float k = Mathf.Clamp01(stopT);

                // Curve returns 1 -> 0
                float m = stopEaseCurve != null ? stopEaseCurve.Evaluate(k) : (1f - k);
                currentDegPerSec = stopStartDegPerSec * m;

                // When done, stop fully
                if (k >= 1f)
                {
                    currentDegPerSec = 0f;
                    StopCoroutine(spinRoutine);
                    spinRoutine = null;
                    stopping = false;
                    yield break;
                }
            }

            currentAngle = Mathf.Repeat(currentAngle + currentDegPerSec * dt, 360f);

            var e = target.localEulerAngles;
            e.z = currentAngle;
            target.localEulerAngles = e;

            yield return null;
        }
    }
}