using System;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class BallCardRevealAnimator : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator revealAnimator;
    [SerializeField] private string revealTrigger = "Reveal";

    [Header("Shader Target")]
    [SerializeField] private Graphic targetGraphic;

    [Header("Material")]
    [SerializeField] private Material sourceMaterial;
    [SerializeField] private bool overrideMaterial = true;

    [Header("Shader Property")]
    [SerializeField] private string sweepProperty = "_Sweep01";

    [Header("Animated Value")]
    [Tooltip("Keyframe this value in the Reveal animation.")]
    [Range(0f, 1f)]
    [SerializeField] private float sweep01;

    private Material runtimeMaterial;
    private Material originalMaterial;

    public event Action RevealFinished;

    private void Reset()
    {
        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();

        if (revealAnimator == null)
            revealAnimator = GetComponent<Animator>();
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

    public void PlayReveal()
    {
        EnsureMaterialInstance();
        Apply();

        if (revealAnimator == null ||
            string.IsNullOrEmpty(revealTrigger))
        {
            return;
        }

        revealAnimator.ResetTrigger(revealTrigger);
        revealAnimator.SetTrigger(revealTrigger);
    }

    public void SetSweep(float value)
    {
        sweep01 = Mathf.Clamp01(value);

        EnsureMaterialInstance();
        Apply();
    }

    public void ResetSweep(float value = 0f)
    {
        sweep01 = Mathf.Clamp01(value);

        EnsureMaterialInstance();
        Apply();
    }

    private void EnsureMaterialInstance()
    {
        if (targetGraphic == null)
            return;

        if (originalMaterial == null)
            originalMaterial = targetGraphic.material;

        Material baseMaterial =
            overrideMaterial && sourceMaterial != null
                ? sourceMaterial
                : targetGraphic.material;

        if (baseMaterial == null)
            return;

        if (runtimeMaterial != null &&
            targetGraphic.material == runtimeMaterial)
        {
            return;
        }

        CleanupRuntimeMaterial();

        runtimeMaterial = Instantiate(baseMaterial);
        runtimeMaterial.name =
            $"{baseMaterial.name}_{gameObject.name}_Runtime";

        targetGraphic.material = runtimeMaterial;
        targetGraphic.SetMaterialDirty();
    }

    private void Apply()
    {
        if (targetGraphic == null)
            return;

        Material material = targetGraphic.material;

        if (material == null)
            return;

        if (!material.HasProperty(sweepProperty))
            return;

        material.SetFloat(
            sweepProperty,
            sweep01
        );

        targetGraphic.SetMaterialDirty();
    }

    private void OnDisable()
    {
        CleanupRuntimeMaterial();
    }

    private void OnDestroy()
    {
        CleanupRuntimeMaterial();
    }

    private void CleanupRuntimeMaterial()
    {
        if (runtimeMaterial == null)
            return;

        if (targetGraphic != null &&
            targetGraphic.material == runtimeMaterial)
        {
            targetGraphic.material = originalMaterial;
            targetGraphic.SetMaterialDirty();
        }

        if (Application.isPlaying)
            Destroy(runtimeMaterial);
        else
            DestroyImmediate(runtimeMaterial);

        runtimeMaterial = null;
    }

    public void AE_RevealFinished()
    {
        RevealFinished?.Invoke();
    }
}