using UnityEngine;

/// Place this on the root of each enemy icon prefab returned by BallSet.
/// Assign the Animator in the prefab inspector.
public class EnemyIconNode : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string doneTrigger = "Done";
    [SerializeField] private string greyTrigger = "Grey";
    [SerializeField] private string restartTrigger = "Restart";

    // Called by LevelProgressBarSlider during build via static context.
    internal void TriggerGrey()
    {
        //Debug.Log($"[TriggerGrey] Triggered on {gameObject.name} at {Time.time:F2}");
        if (animator != null)
            animator.SetTrigger(greyTrigger);
    }

    internal void TriggerDone()
    {
        if (animator != null)
            animator.SetTrigger(doneTrigger);
    }

    internal void TriggerRestart()
    {
        if (animator != null)
            animator.SetTrigger(restartTrigger);
    }

    // Self-register with whichever LevelProgressBarSlider is currently building.
    private void Awake()
    {
        LevelProgressBarSlider.TryRegisterBuildingIcon(this);
    }
}
