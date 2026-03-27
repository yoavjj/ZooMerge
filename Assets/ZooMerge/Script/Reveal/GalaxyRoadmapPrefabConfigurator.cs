using UnityEngine;

public class GalaxyRoadmapPrefabConfigurator : MonoBehaviour
{
    public enum Slot { Current, Next, NextNext }

    [Header("Galaxy Image (BW/Color)")]
    [SerializeField] private GalaxyColorAnimator galaxyColorAnimator;

    [Header("Animator Triggers")]
    [SerializeField] private string currentTrigger = "Current";
    [SerializeField] private string nextTrigger = "Next";
    [SerializeField] private string RevealTrigger = "Reveal";

    public void Configure(Slot slot, bool isReveal)
    {
        if (galaxyColorAnimator == null) return;

        bool isCurrent = slot == Slot.Current;

        if (isReveal)
        {
            if (isCurrent)
            {
                // ✅ ONLY current behaves like NEXT
                galaxyColorAnimator.SetBlend(0f);
                galaxyColorAnimator.PlayState(nextTrigger);
            }
            else
            {
                // others stay as normal "next style"
                galaxyColorAnimator.SetBlend(0f);
                galaxyColorAnimator.PlayState(nextTrigger);
            }

            return;
        }

        // Normal mode
        galaxyColorAnimator.SetBlend(isCurrent ? 1f : 0f);
        galaxyColorAnimator.PlayState(isCurrent ? currentTrigger : nextTrigger);
    }

    public void PlayReveal()
    {
        if (galaxyColorAnimator != null)
            galaxyColorAnimator.PlayState(RevealTrigger);
    }
}