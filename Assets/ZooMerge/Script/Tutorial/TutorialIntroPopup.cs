using System;
using System.Collections;
using UnityEngine;

public class TutorialIntroPopup : MonoBehaviour
{
    [Serializable]
    private class PhaseSettings
    {
        public string trigger;

        [Min(0f)]
        public float autoDelay = 5f;

        [NonSerialized]
        public bool triggered;
    }

    public event Action Finished;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string inTrigger = "In";
    [SerializeField] private string outTrigger = "Out";

    [Header("Phases")]
    [SerializeField]
    private PhaseSettings[] phases =
    {
        new PhaseSettings
        {
            trigger = "Phase_1"
        },

        new PhaseSettings
        {
            trigger = "Phase_2"
        },

        new PhaseSettings
        {
            trigger = "Phase_3"
        }
    };

    [Header("Dialogue")]
    [SerializeField]
    private TutorialAnimatedText animatedText;

    private Coroutine autoContinueRoutine;

    private bool isFinishing;
    private bool finished;

    public void Open()
    {
        isFinishing = false;
        finished = false;

        ResetPhases();
        StopAutoContinue();

        if (animator == null)
            return;

        animator.ResetTrigger(
            outTrigger
        );

        animator.ResetTrigger(
            inTrigger
        );

        animator.SetTrigger(
            inTrigger
        );
    }

    // ============================================================
    // PHASES
    // ============================================================

    public void ContinueToPhase(
        string triggerName)
    {
        PhaseSettings phase =
            GetPhase(triggerName);

        if (phase == null ||
            phase.triggered)
        {
            return;
        }

        phase.triggered = true;

        StopAutoContinue();

        if (animator == null)
            return;

        animator.ResetTrigger(
            phase.trigger
        );

        animator.SetTrigger(
            phase.trigger
        );
    }

    public void StartAutoContinue(
        string triggerName)
    {
        PhaseSettings phase =
            GetPhase(triggerName);

        if (phase == null ||
            phase.triggered)
        {
            return;
        }

        StopAutoContinue();

        autoContinueRoutine =
            StartCoroutine(
                AutoContinue(
                    phase
                )
            );
    }

    private IEnumerator AutoContinue(
        PhaseSettings phase)
    {
        if (phase.autoDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    phase.autoDelay
                );
        }

        autoContinueRoutine = null;

        ContinueToPhase(
            phase.trigger
        );
    }

    private void StopAutoContinue()
    {
        if (autoContinueRoutine == null)
            return;

        StopCoroutine(
            autoContinueRoutine
        );

        autoContinueRoutine = null;
    }

    private PhaseSettings GetPhase(
        string triggerName)
    {
        if (phases == null ||
            string.IsNullOrEmpty(
                triggerName))
        {
            return null;
        }

        foreach (
            PhaseSettings phase
            in phases)
        {
            if (phase != null &&
                phase.trigger ==
                triggerName)
            {
                return phase;
            }
        }

        Debug.LogWarning(
            $"[TutorialIntroPopup] " +
            $"Phase '{triggerName}' was not found."
        );

        return null;
    }

    private void ResetPhases()
    {
        if (phases == null)
            return;

        foreach (
            PhaseSettings phase
            in phases)
        {
            if (phase != null)
                phase.triggered = false;
        }
    }

    // ============================================================
    // FINISH
    // ============================================================

    public void FinishIntro()
    {
        if (isFinishing ||
            finished)
        {
            return;
        }

        StopAutoContinue();

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
            gameObject, 0.25f);
    }

    public void AE_PlayDialogue(
    string dialogueId)
    {
        if (animatedText == null)
        {
            Debug.LogWarning(
                "[TutorialIntroPopup] " +
                "TutorialAnimatedText is missing."
            );

            return;
        }

        animatedText.Play(
            dialogueId
        );
    }
}