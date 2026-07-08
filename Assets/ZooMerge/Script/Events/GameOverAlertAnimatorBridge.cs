using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameOverAlertAnimatorBridge : SfxBehaviourTirgger
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

        CleanupDestroyedBalls();

        // If the old countdown ball was destroyed/merged,
        // the set may now be empty while the old countdown is still running.
        if (touchingBalls.Count == 0 && countdownRoutine != null)
        {
            StopCountdown();
        }

        bool wasAdded = touchingBalls.Add(ball);
        if (!wasAdded) return;

        // ✅ This ball is now the active danger ball.
        // Restart the timer from this ball's countdownSeconds.
        if (touchingBalls.Count == 1)
        {
            countdownTotal = Mathf.Max(0f, countdownSeconds);

            RestartAlertAnimators();

            PlayUiSfx(SfxCue.GameOver_Signal);

            StartCountdown(countdownTotal);
        }
    }

    private void HandleSaved(BallInfo ball)
    {
        if (ball == null)
        {
            CleanupDestroyedBalls();
        }
        else
        {
            touchingBalls.Remove(ball);
            CleanupDestroyedBalls();
        }

        if (touchingBalls.Count == 0)
        {
            StopCountdown();

            TriggerSaved(alertAnimator);
            TriggerSaved(timerAnimator);

            if (timerText != null)
                timerText.text = "00:00";
        }
    }

    private void CleanupDestroyedBalls()
    {
        if (touchingBalls.Count == 0)
            return;

        List<BallInfo> toRemove = null;

        foreach (var ball in touchingBalls)
        {
            if (ball == null)
            {
                toRemove ??= new List<BallInfo>();
                toRemove.Add(ball);
            }
        }

        if (toRemove == null)
            return;

        foreach (var ball in toRemove)
            touchingBalls.Remove(ball);
    }

    private void RestartAlertAnimators()
    {
        RestartAnimatorAlert(alertAnimator);
        RestartAnimatorAlert(timerAnimator);
    }

    private void RestartAnimatorAlert(Animator animator)
    {
        if (animator == null) return;

        animator.ResetTrigger(savedTrigger);
        animator.ResetTrigger(alertTrigger);

        // Force animator to evaluate reset before Alert trigger.
        animator.Update(0f);

        animator.SetTrigger(alertTrigger);
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

        int lastPlayedSecond = -1;

        while (t > 0f)
        {
            CleanupDestroyedBalls();

            if (touchingBalls.Count == 0)
            {
                countdownRoutine = null;
                yield break;
            }

            if (timerText != null)
                timerText.text = FormatSeconds(t);

            int wholeSecond = Mathf.CeilToInt(t);

            if (wholeSecond <= 3 &&
                wholeSecond >= 1 &&
                wholeSecond != lastPlayedSecond)
            {
                lastPlayedSecond = wholeSecond;

                PlayUiSfx(SfxCue.Countdown_End);
            }

            t -= Time.deltaTime;
            yield return null;
        }

        if (timerText != null)
            timerText.text = "00:00";

        BallInfo losingBall = null;

        foreach (var ball in touchingBalls)
        {
            if (ball != null)
            {
                losingBall = ball;
                break;
            }
        }

        touchingBalls.Clear();
        countdownRoutine = null;

        if (losingBall != null)
        {
            BallEventManager.RaiseBallTouchedGameOverLine(
                losingBall,
                (BallEventManager.GameOverReason)0
            );
        }
        else
        {
            TriggerSaved(alertAnimator);
            TriggerSaved(timerAnimator);
        }
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