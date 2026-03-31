using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class GalaxyColorAnimator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Graphic targetGraphic; // Image / RawImage

    [Header("Material")]
    [SerializeField] private Material sourceMaterial; // your BW shader material
    [SerializeField] private bool overrideMaterial = true;

    [Header("Shader Property")]
    [SerializeField] private string blendProperty = "_Blend";

    [Header("Animated Value (keyframe this)")]
    [Range(0, 1)]
    [SerializeField] private float blend = 1f;

    [Header("Animator")]
    [SerializeField] private Animator galaxyAnimator;
    [SerializeField] private string galaxyDoneTrigger = "Done";
    [SerializeField] private string galaxyOutTrigger = "Out";
    [SerializeField] private string galaxyRevealTrigger = "Reveal";

    private Material runtimeMat;

    private void Reset()
    {
        targetGraphic = GetComponent<Graphic>();
    }

    private void OnEnable()
    {
        EnsureMaterialInstance();
        Apply();
    }

    private void OnValidate()
    {
        EnsureMaterialInstance();
        Apply();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            Apply();
    }

    private void OnDidApplyAnimationProperties()
    {
        Apply();
    }

    private void OnDisable()
    {
        if (Application.isPlaying && runtimeMat != null)
        {
            Destroy(runtimeMat);
            runtimeMat = null;
        }
    }

    private void EnsureMaterialInstance()
    {
        if (targetGraphic == null) return;

        Material baseMat = overrideMaterial && sourceMaterial != null
            ? sourceMaterial
            : targetGraphic.material;

        if (baseMat == null) return;

        if (runtimeMat != null && targetGraphic.material == runtimeMat)
            return;

        runtimeMat = Instantiate(baseMat);
        targetGraphic.material = runtimeMat;
    }

    private void Apply()
    {
        if (targetGraphic == null) return;
        if (targetGraphic.material == null) return;

        var mat = targetGraphic.material;

        if (mat.HasProperty(blendProperty))
            mat.SetFloat(blendProperty, blend);
    }

    public void PlayDone()
    {
        if (galaxyAnimator == null || string.IsNullOrEmpty(galaxyDoneTrigger))
            return;

        galaxyAnimator.ResetTrigger(galaxyDoneTrigger);
        galaxyAnimator.SetTrigger(galaxyDoneTrigger);
    }

    // Optional helper if you want to trigger via code
    public void SetBlend(float value)
    {
        blend = value;
        EnsureMaterialInstance(); // <--- add this
        Apply();
    }

    public void PlayState(string triggerName)
    {
        if (galaxyAnimator == null) return;
        if (string.IsNullOrEmpty(triggerName)) return;

        // Optional but helps when switching quickly
        galaxyAnimator.ResetTrigger(triggerName);
        galaxyAnimator.SetTrigger(triggerName);
    }

    public void PlayOut()
    {
        galaxyAnimator?.ResetTrigger(galaxyOutTrigger);
        galaxyAnimator?.SetTrigger(galaxyOutTrigger);

        Destroy(gameObject, 1.5f);
    }

    public void PlayReveal()
    {
        galaxyAnimator?.ResetTrigger(galaxyRevealTrigger);
        galaxyAnimator?.SetTrigger(galaxyRevealTrigger);
    }
}