using UnityEngine;
using TMPro;

public class GalaxyRoadmapPrefabConfigurator : MonoBehaviour
{
    public enum Slot { Current, Next, NextNext }

    [Header("Galaxy Image (BW/Color)")]
    [SerializeField] private GalaxyColorAnimator galaxyColorAnimator;

    [Header("Galaxy Name Text")]
    [SerializeField] private TextMeshProUGUI galaxyLevelNameText;

    [Header("Animator Triggers")]
    [SerializeField] private string currentTrigger = "Current";
    [SerializeField] private string nextTrigger = "Next";
    [SerializeField] private string revealTrigger = "Reveal";

    public void SetGalaxyName(string galaxyName, bool show)
    {
        if (galaxyLevelNameText == null) return;
        galaxyLevelNameText.text = show ? (galaxyName ?? string.Empty) : string.Empty;
    }

    public void Configure(Slot slot, bool isReveal)
    {
        if (galaxyColorAnimator == null) return;

        bool isCurrent = slot == Slot.Current;

        if (isReveal)
        {
            galaxyColorAnimator.SetBlend(0f);
            galaxyColorAnimator.PlayState(nextTrigger);
            return;
        }

        galaxyColorAnimator.SetBlend(isCurrent ? 1f : 0f);
        galaxyColorAnimator.PlayState(isCurrent ? currentTrigger : nextTrigger);
    }

    public void PlayReveal()
    {
        if (galaxyColorAnimator != null)
            galaxyColorAnimator.PlayState(revealTrigger);
    }
}