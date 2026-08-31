using System;
using UnityEngine;

[ExecuteAlways]
public class SpaceshipSkinRevealAnimator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("Material")]
    [SerializeField] private Material sourceMaterial;
    [SerializeField] private bool overrideMaterial = true;

    [Header("Shader Properties")]
    [SerializeField] private string currentTexProperty = "_MainTex";
    [SerializeField] private string nextTexProperty = "_NextTex";
    [SerializeField] private string blendProperty = "_Blend";

    [Header("Beam Target")]
    [SerializeField] private SpriteRenderer beamRenderer;

    [Header("Beam Material")]
    [SerializeField] private Material beamSourceMaterial;

    [Header("Beam Shader")]
    [SerializeField] private string beamRevealProperty = "_Reveal";

    [Header("Beam Animated Value (keyframe this)")]
    [Range(0f, 1f)]
    [SerializeField] private float beamReveal = 0f;

    private Material runtimeBeamMat;

    [Header("Animated Value (keyframe this)")]
    [Range(0f, 1f)]
    [SerializeField] private float blend = 0f;

    [Header("Animator")]
    [SerializeField] private Animator revealAnimator;
    [SerializeField] private string revealTrigger = "Reveal";
    [SerializeField] private string idleTrigger = "Idle";

    private Material runtimeMat;
    private Sprite pendingFinalSprite;

    private Action revealFinished;

    private void Reset()
    {
        targetRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        EnsureMaterialInstance();
        EnsureBeamMaterialInstance();

        Apply();
    }

    private void OnValidate()
    {
        EnsureMaterialInstance();
        EnsureBeamMaterialInstance();

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
        DisposeRuntimeMaterial();
        DisposeBeamRuntimeMaterial();
    }

    private void DisposeRuntimeMaterial()
    {
        if (runtimeMat == null)
            return;

        if (Application.isPlaying)
            Destroy(runtimeMat);
        else
            DestroyImmediate(runtimeMat);

        runtimeMat = null;
    }

    private void EnsureMaterialInstance()
    {
        if (targetRenderer == null)
            return;

        Material baseMat = overrideMaterial && sourceMaterial != null
            ? sourceMaterial
            : targetRenderer.sharedMaterial;

        if (baseMat == null)
            return;

        if (runtimeMat != null && targetRenderer.sharedMaterial == runtimeMat)
            return;

        DisposeRuntimeMaterial();

        runtimeMat = Instantiate(baseMat);
        runtimeMat.name = baseMat.name + " (Runtime)";
        targetRenderer.sharedMaterial = runtimeMat;
    }

    private void Apply()
    {
        if (runtimeMat != null &&
            runtimeMat.HasProperty(blendProperty))
        {
            runtimeMat.SetFloat(
                blendProperty,
                blend
            );
        }

        if (runtimeBeamMat != null &&
            runtimeBeamMat.HasProperty(
                beamRevealProperty))
        {
            runtimeBeamMat.SetFloat(
                beamRevealProperty,
                beamReveal
            );
        }
    }

    public void SetBlend(float value)
    {
        blend = Mathf.Clamp01(value);
        EnsureMaterialInstance();
        Apply();
    }

    public void PrepareReveal(Sprite oldSprite, Sprite newSprite)
    {
        if (oldSprite == null || newSprite == null)
        {
            Debug.LogWarning("[SpaceshipSkinRevealAnimator] Old or new sprite is null.");
            return;
        }

        EnsureMaterialInstance();

        if (runtimeMat == null)
            return;

        runtimeMat.SetTexture(currentTexProperty, oldSprite.texture);
        runtimeMat.SetTexture(nextTexProperty, newSprite.texture);

        pendingFinalSprite = newSprite;
        blend = 0f;
        Apply();
    }

    public void PlayReveal(Sprite oldSprite, Sprite newSprite, Action onComplete = null)
    {
        PrepareReveal(oldSprite, newSprite);

        revealFinished = onComplete;

        if (revealAnimator != null && !string.IsNullOrEmpty(revealTrigger))
        {
            revealAnimator.ResetTrigger(revealTrigger);
            revealAnimator.SetTrigger(revealTrigger);
        }
        else
        {
            AE_FinishReveal();
        }
    }

    public void PlayIdle()
    {
        if (revealAnimator == null || string.IsNullOrEmpty(idleTrigger))
            return;

        revealAnimator.ResetTrigger(idleTrigger);
        revealAnimator.SetTrigger(idleTrigger);
    }

    // Call this at the END of the animation via Animation Event
    public void AE_FinishReveal()
    {
        if (targetRenderer == null || pendingFinalSprite == null)
            return;

        targetRenderer.sprite = pendingFinalSprite;

        if (runtimeMat != null)
        {
            runtimeMat.SetTexture(currentTexProperty, pendingFinalSprite.texture);
            runtimeMat.SetTexture(nextTexProperty, pendingFinalSprite.texture);
        }

        blend = 0f;
        beamReveal = 0f;

        Apply();
        PlayIdle();

        pendingFinalSprite = null;

        Action callback = revealFinished;
        revealFinished = null;
        callback?.Invoke();
    }

    private void EnsureBeamMaterialInstance()
    {
        if (beamRenderer == null)
            return;

        Material baseMat =
            beamSourceMaterial != null
                ? beamSourceMaterial
                : beamRenderer.sharedMaterial;

        if (baseMat == null)
            return;

        if (runtimeBeamMat != null &&
            beamRenderer.sharedMaterial ==
            runtimeBeamMat)
        {
            return;
        }

        DisposeBeamRuntimeMaterial();

        runtimeBeamMat =
            Instantiate(baseMat);

        runtimeBeamMat.name =
            baseMat.name +
            " (Beam Runtime)";

        beamRenderer.sharedMaterial =
            runtimeBeamMat;
    }

    private void DisposeBeamRuntimeMaterial()
    {
        if (runtimeBeamMat == null)
            return;

        if (Application.isPlaying)
            Destroy(runtimeBeamMat);
        else
            DestroyImmediate(runtimeBeamMat);

        runtimeBeamMat = null;
    }

    public void SetBeamReveal(float value)
    {
        beamReveal =
            Mathf.Clamp01(value);

        EnsureBeamMaterialInstance();

        Apply();
    }

    public SpriteRenderer TargetRenderer => targetRenderer;
}