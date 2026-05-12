using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameOverAlertAnimatorBridge : MonoBehaviour
{
    [Header("Main Alert Animator")]
    [SerializeField] private Animator alertAnimator;

    [Header("Timer Animator + Text")]
    [SerializeField] private Animator timerAnimator;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Trigger Names")]
    [SerializeField] private string alertTrigger = "Alert";
    [SerializeField] private string savedTrigger = "Saved";

    private readonly HashSet<BallInfo> touchingBalls = new HashSet<BallInfo>();

    private Coroutine countdownRoutine;
    private float countdownTotal = 0f;

    private void Awake()
    {
        if (alertAnimator == null)
            alertAnimator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        BallEventManager.OnBallGameOverAlertStarted += HandleAlertStarted;
        BallEventManager.OnBallGameOverSaved += HandleSaved;

        BallEventManager.OnBallTouchedGameOverLine += HandleLost;

        BallEventManager.OnSessionStarted += ResetState;
        BallEventManager.OnReturnToMainMenu += ResetState;
        BallEventManager.OnGameOverAnimation += ResetState;
    }

    private void OnDisable()
    {
        BallEventManager.OnBallGameOverAlertStarted -= HandleAlertStarted;
        BallEventManager.OnBallGameOverSaved -= HandleSaved;

        BallEventManager.OnBallTouchedGameOverLine -= HandleLost;

        BallEventManager.OnSessionStarted -= ResetState;
        BallEventManager.OnReturnToMainMenu -= ResetState;
        BallEventManager.OnGameOverAnimation -= ResetState;
    }

    private void HandleAlertStarted(BallInfo ball, float countdownSeconds)
    {
        if (ball == null) return;

        // Add ball to active set. If it was already there, ignore.
        bool wasAdded = touchingBalls.Add(ball);
        if (!wasAdded) return;

        // ✅ Only fire Alert when this is the FIRST ball entering
        if (touchingBalls.Count == 1)
        {
            countdownTotal = Mathf.Max(0f, countdownSeconds);

            TriggerAlert(alertAnimator);
            TriggerAlert(timerAnimator);

            StartCountdown(countdownTotal);
        }
    }

    private void HandleSaved(BallInfo ball)
    {
        if (ball == null) return;

        touchingBalls.Remove(ball);

        // ✅ Only fire Saved when the LAST ball leaves
        if (touchingBalls.Count == 0)
        {
            StopCountdown();

            TriggerSaved(alertAnimator);
            TriggerSaved(timerAnimator);

            // IMPORTANT: if you allow multiple balls, don't set IsGameOverCountdownActive=false
            // in BallEventManager.RaiseBallGameOverSaved. Do it here when Count==0 instead.
            // BallEventManager.SetGameOverCountdownActive(false); // if you added a setter.
        }
    }

    private void HandleLost(BallInfo _)
    {
        // Session is lost → just close the UI using "Saved" trigger (acts like "Out")
        touchingBalls.Clear();
        StopCountdown();

        TriggerSaved(alertAnimator);
        TriggerSaved(timerAnimator);

        if (timerText != null)
            timerText.text = "00:00";
    }

    private void TriggerAlert(Animator a)
    {
        if (a == null) return;
        a.ResetTrigger(savedTrigger);
        a.SetTrigger(alertTrigger);
    }

    private void TriggerSaved(Animator a)
    {
        if (a == null) return;
        a.ResetTrigger(alertTrigger);
        a.SetTrigger(savedTrigger);
    }

    private void StartCountdown(float seconds)
    {
        StopCountdown();

        if (timerText != null)
            timerText.text = FormatSeconds(seconds);

        if (seconds > 0f)
            countdownRoutine = StartCoroutine(CountdownRoutine(seconds));
    }

    private void StopCountdown()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }
    }

    private IEnumerator CountdownRoutine(float total)
    {
        float t = total;

        while (t > 0f)
        {
            // If somehow no balls remain, stop updating
            if (touchingBalls.Count == 0) yield break;

            if (timerText != null)
                timerText.text = FormatSeconds(t);

            t -= Time.deltaTime;
            yield return null;
        }

        if (timerText != null)
            timerText.text = FormatSeconds(0f);

        countdownRoutine = null;
    }

    // "SS:CC" (seconds : centiseconds) -> 05:22
    private static string FormatSeconds(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int whole = Mathf.FloorToInt(seconds);
        int centi = Mathf.FloorToInt((seconds - whole) * 100f);

        // handle rounding edge cases
        if (centi >= 100) { centi = 0; whole += 1; }

        return $"{whole:00}:{centi:00}";
    }

    private void ResetState()
    {
        touchingBalls.Clear();
        StopCountdown();

        // ✅ Force both animators back to "Saved" state so Alert can't stay stuck
        TriggerSaved(alertAnimator);
        TriggerSaved(timerAnimator);

        if (timerText != null)
            timerText.text = "00:00";
    }
}