using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class SpaceBackgroundRevealAnimator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image targetImage;

    [Header("Material")]
    [SerializeField] private Material sourceMaterial;

    [Header("Star Particles")]
    [SerializeField] private ParticleSystemRenderer starParticleRenderer;
    [SerializeField] private Material starParticleSourceMaterial;

    [SerializeField]
    private string starOpacityProperty =
        "_Opacity";

    [Header("Star Opacity - Keyframe This")]
    [Range(0f, 1f)]
    [SerializeField] private float starOpacity;

    [Header("Shader")]
    [SerializeField]
    private string revealProperty =
        "_Reveal";

    [SerializeField]
    private string centerProperty =
        "_RevealCenter";

    [Header("Animated Value - Keyframe This")]
    [Range(0f, 1f)]
    [SerializeField] private float reveal;

    [Header("Reveal Origin")]
    [SerializeField]
    private Vector2 revealCenter =
        new Vector2(
            0.5f,
            0.5f
        );

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField]
    private string revealTrigger =
        "Reveal";

    [Header("Tutorial Opening Text")]
    [SerializeField]
    private TutorialDialogueData dialogueDatabase;

    [SerializeField]
    private TMPWordScaleAnimator wordScaleAnimator;


    private Material runtimeMaterial;

    private Material runtimeStarMaterial;

    private void OnEnable()
    {
        EnsureMaterialInstance();
        EnsureStarMaterialInstance();

        Apply();
    }

    private void OnValidate()
    {
        EnsureMaterialInstance();
        EnsureStarMaterialInstance();

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
        DisposeRuntimeStarMaterial();
    }

    private void EnsureStarMaterialInstance()
    {
        if (starParticleRenderer == null)
            return;

        Material baseMaterial =
            starParticleSourceMaterial != null
                ? starParticleSourceMaterial
                : starParticleRenderer.sharedMaterial;

        if (baseMaterial == null)
            return;

        if (runtimeStarMaterial != null &&
            starParticleRenderer.sharedMaterial ==
            runtimeStarMaterial)
        {
            return;
        }

        DisposeRuntimeStarMaterial();

        runtimeStarMaterial =
            Instantiate(
                baseMaterial
            );

        runtimeStarMaterial.name =
            baseMaterial.name +
            " (Runtime)";

        starParticleRenderer.sharedMaterial =
            runtimeStarMaterial;
    }

    private void DisposeRuntimeStarMaterial()
    {
        if (runtimeStarMaterial == null)
            return;

        if (Application.isPlaying)
        {
            Destroy(
                runtimeStarMaterial
            );
        }
        else
        {
            DestroyImmediate(
                runtimeStarMaterial
            );
        }

        runtimeStarMaterial = null;
    }

    private void EnsureMaterialInstance()
    {
        if (targetImage == null)
            return;

        Material baseMaterial =
            sourceMaterial != null
                ? sourceMaterial
                : targetImage.material;

        if (baseMaterial == null)
            return;

        if (runtimeMaterial != null &&
            targetImage.material ==
            runtimeMaterial)
        {
            return;
        }

        DisposeRuntimeMaterial();

        runtimeMaterial =
            Instantiate(
                baseMaterial
            );

        runtimeMaterial.name =
            baseMaterial.name +
            " (Runtime)";

        targetImage.material =
            runtimeMaterial;
    }

    private void Apply()
    {
        if (runtimeMaterial != null)
        {
            if (runtimeMaterial.HasProperty(
                    revealProperty))
            {
                runtimeMaterial.SetFloat(
                    revealProperty,
                    reveal
                );
            }

            if (runtimeMaterial.HasProperty(
                    centerProperty))
            {
                runtimeMaterial.SetVector(
                    centerProperty,
                    new Vector4(
                        revealCenter.x,
                        revealCenter.y,
                        0f,
                        0f
                    )
                );
            }
        }

        if (runtimeStarMaterial != null &&
            runtimeStarMaterial.HasProperty(
                starOpacityProperty))
        {
            runtimeStarMaterial.SetFloat(
                starOpacityProperty,
                starOpacity
            );
        }
    }

    public void SetReveal(float value)
    {
        reveal =
            Mathf.Clamp01(
                value
            );

        EnsureMaterialInstance();
        Apply();
    }

    public void SetStarOpacity(float value)
    {
        starOpacity =
            Mathf.Clamp01(
                value
            );

        EnsureStarMaterialInstance();
        Apply();
    }

    public void PlayReveal()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(
            revealTrigger
        );

        animator.SetTrigger(
            revealTrigger
        );
    }

    public void ResetReveal()
    {
        reveal = 0f;

        EnsureMaterialInstance();
        Apply();
    }

    private void DisposeRuntimeMaterial()
    {
        if (runtimeMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(
                runtimeMaterial
            );
        else
            DestroyImmediate(
                runtimeMaterial
            );

        runtimeMaterial = null;
    }

    public void AE_PlayTutorialText(
        string id)
    {
        if (dialogueDatabase == null)
        {
            Debug.LogWarning(
                "[SpaceBackgroundRevealAnimator] " +
                "Dialogue database is missing."
            );

            return;
        }

        if (wordScaleAnimator == null)
        {
            Debug.LogWarning(
                "[SpaceBackgroundRevealAnimator] " +
                "TMPWordScaleAnimator is missing."
            );

            return;
        }

        TutorialDialogueData.Entry entry =
            dialogueDatabase.Get(id);

        if (entry == null)
        {
            Debug.LogWarning(
                $"[SpaceBackgroundRevealAnimator] " +
                $"Dialogue ID '{id}' was not found."
            );

            return;
        }

        wordScaleAnimator.Play(
            id,
            entry
        );
    }
}