using System;
using UnityEngine;
using System.Collections;

public class TutorialIntroPopup : MonoBehaviour
{
    public event Action Finished;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [SerializeField]
    private string inTrigger = "In";

    [SerializeField]
    private string outTrigger = "Out";

    [Header("Phases")]
    [SerializeField]
    private string phase2Trigger = "Phase_2";

    [SerializeField]
    private string phase3Trigger = "Phase_3";

    private bool isFinishing;
    private bool finished;

    [Header("Auto Continue")]
    [SerializeField, Min(0f)]
    private float phase2AutoDelay = 5f;

    [SerializeField, Min(0f)]
    private float phase3AutoDelay = 5f;

    private Coroutine phase2AutoRoutine;
    private Coroutine phase3AutoRoutine;

    private bool phase2Triggered;
    private bool phase3Triggered;

    public void Open()
    {
        isFinishing = false;
        finished = false;

        phase2Triggered = false;
        phase3Triggered = false;

        StopAutoContinueTimers();

        if (animator == null)
            return;

        animator.ResetTrigger(outTrigger);
        animator.ResetTrigger(inTrigger);
        animator.SetTrigger(inTrigger);
    }

    // ============================================================
    // PHASES
    // ============================================================

    public void ContinueToPhase2()
    {
        if (phase2Triggered)
            return;

        phase2Triggered = true;

        StopPhase2AutoContinue();

        TriggerPhase(
            phase2Trigger
        );
    }

    public void ContinueToPhase3()
    {
        if (phase3Triggered)
            return;

        phase3Triggered = true;

        StopPhase3AutoContinue();

        TriggerPhase(
            phase3Trigger
        );
    }

    public void StartPhase2AutoContinue()
    {
        if (phase2Triggered)
            return;

        StopPhase2AutoContinue();

        phase2AutoRoutine =
            StartCoroutine(
                AutoContinuePhase2()
            );
    }

    public void StartPhase3AutoContinue()
    {
        if (phase3Triggered)
            return;

        StopPhase3AutoContinue();

        phase3AutoRoutine =
            StartCoroutine(
                AutoContinuePhase3()
            );
    }

    private IEnumerator AutoContinuePhase2()
    {
        if (phase2AutoDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    phase2AutoDelay
                );
        }

        phase2AutoRoutine = null;

        ContinueToPhase2();
    }

    private IEnumerator AutoContinuePhase3()
    {
        if (phase3AutoDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    phase3AutoDelay
                );
        }

        phase3AutoRoutine = null;

        ContinueToPhase3();
    }

    private void StopPhase2AutoContinue()
    {
        if (phase2AutoRoutine == null)
            return;

        StopCoroutine(
            phase2AutoRoutine
        );

        phase2AutoRoutine = null;
    }

    private void StopPhase3AutoContinue()
    {
        if (phase3AutoRoutine == null)
            return;

        StopCoroutine(
            phase3AutoRoutine
        );

        phase3AutoRoutine = null;
    }

    private void StopAutoContinueTimers()
    {
        StopPhase2AutoContinue();
        StopPhase3AutoContinue();
    }

    private void TriggerPhase(
        string triggerName)
    {
        if (animator == null)
            return;

        if (string.IsNullOrEmpty(
                triggerName))
        {
            return;
        }

        animator.ResetTrigger(
            triggerName
        );

        animator.SetTrigger(
            triggerName
        );
    }

    // ============================================================
    // FINISH
    // ============================================================

    public void FinishIntro()
    {
        if (isFinishing || finished)
            return;

        StopAutoContinueTimers();

        isFinishing = true;

        if (animator != null &&
            !string.IsNullOrEmpty(
                outTrigger))
        {
            animator.ResetTrigger(
                inTrigger
            );

            animator.ResetTrigger(
                outTrigger
            );

            animator.SetTrigger(
                outTrigger
            );

            return;
        }

        AE_OutFinished();
    }

    public void AE_OutFinished()
    {
        if (finished)
            return;

        finished = true;
        isFinishing = false;

        Finished?.Invoke();

        Destroy(
            gameObject
        );
    }
}