using System.Collections;
using UnityEngine;

[ExecuteAlways]
public class HeartIntroAnimation : SfxBehaviourTirgger
{
    [SerializeField] private Animator animator;
    [SerializeField] private float delay = 0.05f;   // small delay so UI/layout settles
    [SerializeField] private string triggerName = "In";

    private Coroutine routine;

    private void OnEnable()
    {
        if (animator == null) return;

        // restart if reused / pooled
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PlayInAfterDelay());
    }

    private IEnumerator PlayInAfterDelay()
    {
        // Make sure animator is in a clean state for this spawn
        animator.enabled = true;
        animator.Rebind();
        animator.Update(0f);

        // delay (use unscaled so it works in pause/debug popups)
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);
        animator.Update(0f);
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }
}