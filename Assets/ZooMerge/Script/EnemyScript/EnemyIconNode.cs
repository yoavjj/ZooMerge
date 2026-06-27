using UnityEngine;

/// Place this on the root of each enemy icon prefab returned by BallSet.
/// Assign the Animator in the prefab inspector.
public class EnemyIconNode : SfxBehaviourTirgger
{
    [SerializeField] private Animator animator;
    [SerializeField] private string doneTrigger = "Done";
    [SerializeField] private string greyTrigger = "Grey";
    [SerializeField] private string restartTrigger = "Restart";

    [SerializeField] private RectTransform rectTransform;
    public RectTransform RectTransform => rectTransform;

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        LevelProgressBarSlider.TryRegisterBuildingIcon(this);
    }

    // Called by LevelProgressBarSlider during build via static context.
    internal void TriggerGrey(bool playSfx = false)
    {
        //Debug.Log($"[TriggerGrey] Triggered on {gameObject.name} at {Time.time:F2}");
        if (animator != null)
            animator.SetTrigger(greyTrigger);
        
        if (playSfx)
            PlayUiSfx(SfxCue.ProgressBar_EnemyGrey);
    }

    internal void TriggerDone()
    {
        if (animator != null)
            animator.SetTrigger(doneTrigger);
    }

    internal void TriggerRestart()
    {
        if (animator == null) return;

        // Reset any pending triggers to avoid state conflicts
        animator.ResetTrigger(doneTrigger);
        animator.ResetTrigger(greyTrigger);
        animator.ResetTrigger(restartTrigger); // Just in case it's still queued

        // Now safely set the restart trigger
        animator.SetTrigger(restartTrigger);
    }

    // Self-register with whichever LevelProgressBarSlider is currently building.
}
