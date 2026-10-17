using System;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class SpaceshipSkinRevealAnimator : SfxBehaviourTirgger
{
    [Header("Target")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Image targetImage;

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
        if (targetRenderer == null &&
            targetImage == null)
        {
            return;
        }

        Material targetMaterial = null;

        if (targetImage != null)
            targetMaterial = targetImage.material;
        else if (targetRenderer != null)
            targetMaterial = targetRenderer.sharedMaterial;

        Material baseMat =
            overrideMaterial &&
            sourceMaterial != null
                ? sourceMaterial
                : targetMaterial;

        if (baseMat == null)
            return;

        bool alreadyAssigned =
            runtimeMat != null &&
            (
                (targetImage != null &&
                 targetImage.material == runtimeMat) ||

                (targetRenderer != null &&
                 targetRenderer.sharedMaterial == runtimeMat)
            );

        if (alreadyAssigned)
            return;

        DisposeRuntimeMaterial();

        runtimeMat =
            Instantiate(baseMat);

        runtimeMat.name =
            baseMat.name +
            " (Runtime)";

        if (targetImage != null)
            targetImage.material = runtimeMat;
        else
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

    public bool PrepareReveal(Sprite oldSprite, Sprite newSprite)
    {
        if (oldSprite == null || newSprite == null)
        {
            Debug.LogWarning("[SpaceshipSkinRevealAnimator] Old or new sprite is null.");
            return false;
        }

        EnsureMaterialInstance();
        EnsureBeamMaterialInstance();

        if (runtimeMat == null)
        {
            Debug.LogWarning("[SpaceshipSkinRevealAnimator] Runtime material is missing.");
            return false;
        }

        // Force the renderer to visually start from the OLD skin.
        SetTargetSprite(oldSprite);

        runtimeMat.SetTexture(currentTexProperty, oldSprite.texture);
        runtimeMat.SetTexture(nextTexProperty, newSprite.texture);

        pendingFinalSprite = newSprite;

        blend = 0f;
        beamReveal = 0f;

        Apply();

        Debug.Log(
            $"[SpaceshipSkinRevealAnimator] Prepared reveal: " +
            $"{oldSprite.name} -> {newSprite.name}"
        );

        return true;
    }

    public void PlayReveal(Sprite oldSprite, Sprite newSprite, Action onComplete = null)
    {
        if (!PrepareReveal(oldSprite, newSprite))
        {
            onComplete?.Invoke();
            return;
        }

        revealFinished = onComplete;

        if (revealAnimator != null && !string.IsNullOrEmpty(revealTrigger))
        {
            revealAnimator.ResetTrigger(revealTrigger);
            revealAnimator.SetTrigger(revealTrigger);

            Debug.Log("[SpaceshipSkinRevealAnimator] Reveal trigger fired.");
        }
        else
        {
            AE_FinishReveal();
        }
    }

    // Call this at the END of the animation via Animation Event
    public void AE_FinishReveal()
    {
        if (pendingFinalSprite == null)
        {
            FinishRevealCallback();
            return;
        }

        SetTargetSprite(
            pendingFinalSprite
        );

        if (runtimeMat != null)
        {
            runtimeMat.SetTexture(
                currentTexProperty,
                pendingFinalSprite.texture
            );

            runtimeMat.SetTexture(
                nextTexProperty,
                pendingFinalSprite.texture
            );
        }

        blend = 0f;
        beamReveal = 0f;

        Apply();

        pendingFinalSprite = null;

        FinishRevealCallback();
    }

    private void FinishRevealCallback()
    {
        Action callback =
            revealFinished;

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

    private Sprite GetCurrentSprite()
    {
        if (targetImage != null)
            return targetImage.sprite;

        if (targetRenderer != null)
            return targetRenderer.sprite;

        return null;
    }

    private void SetTargetSprite(
        Sprite sprite)
    {
        if (targetImage != null)
        {
            targetImage.sprite = sprite;
            return;
        }

        if (targetRenderer != null)
            targetRenderer.sprite = sprite;
    }

    private bool HasTarget()
    {
        return
            targetImage != null ||
            targetRenderer != null;
    }

    public void AE_PlayRevealSfx()
    {
        PlayUiSfx(SfxCue.Skin_Reveal);
    } 

    public void AE_PlayWooshInSfx()
    {
        PlayUiSfx(SfxCue.Woosh_In);
    } 

    public void AE_PlayWooshOutSfx()
    {
        PlayUiSfx(SfxCue.Woosh_Out);
    }

    public SpriteRenderer TargetRenderer => targetRenderer;
}