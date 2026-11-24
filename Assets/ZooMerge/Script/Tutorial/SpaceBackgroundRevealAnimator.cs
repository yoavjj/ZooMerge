using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class SpaceBackgroundRevealAnimator : MonoBehaviour
{
    // ============================================================
    // SPACE BACKGROUND
    // ============================================================

    [Header("Space Background Target")]
    [SerializeField]
    private Image targetImage;

    [Header("Space Background Material")]
    [SerializeField]
    private Material sourceMaterial;

    [Header("Space Background Shader")]
    [SerializeField]
    private string revealProperty =
        "_Reveal";

    [SerializeField]
    private string centerProperty =
        "_RevealCenter";

    [Header("Space Reveal - Keyframe This")]
    [Range(0f, 1f)]
    [SerializeField]
    private float reveal;

    [Header("Space Reveal Origin")]
    [SerializeField]
    private Vector2 revealCenter =
        new Vector2(
            0.5f,
            0.5f
        );

    // ============================================================
    // TRANSITION REVEAL
    // ============================================================

    [Header("Transition Target")]
    [SerializeField]
    private Image transitionImage;

    [Header("Transition Material")]
    [SerializeField]
    private Material transitionSourceMaterial;

    [Header("Transition Shader")]
    [SerializeField]
    private string transitionRevealInProperty =
        "_Reveal";

    [SerializeField]
    private string transitionRevealOutProperty =
        "_OutReveal";

    [Header("Transition Reveal In - Keyframe This")]
    [Range(0f, 1f)]
    [SerializeField]
    private float transitionRevealIn;

    [Header("Transition Reveal Out - Keyframe This")]
    [Range(0f, 1f)]
    [SerializeField]
    private float transitionRevealOut;

    // ============================================================
    // STAR PARTICLES
    // ============================================================

    [Header("Star Particles")]
    [SerializeField]
    private ParticleSystemRenderer starParticleRenderer;

    [SerializeField]
    private Material starParticleSourceMaterial;

    [SerializeField]
    private string starOpacityProperty =
        "_Opacity";

    [Header("Star Opacity - Keyframe This")]
    [Range(0f, 1f)]
    [SerializeField]
    private float starOpacity;

    // ============================================================
    // ANIMATOR
    // ============================================================

    [Header("Animator")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string revealTrigger =
        "Reveal";

    // ============================================================
    // TUTORIAL TEXT
    // ============================================================

    [Header("Tutorial Opening Text")]
    [SerializeField]
    private TutorialDialogueData dialogueDatabase;

    [SerializeField]
    private TMPWordScaleAnimator wordScaleAnimator;

    // ============================================================
    // RUNTIME MATERIALS
    // ============================================================

    private Material runtimeMaterial;
    private Material runtimeTransitionMaterial;
    private Material runtimeStarMaterial;

    // ============================================================
    // UNITY
    // ============================================================

    private void OnEnable()
    {
        EnsureMaterialInstance();
        EnsureTransitionMaterialInstance();
        EnsureStarMaterialInstance();

        Apply();
    }

    private void OnValidate()
    {
        EnsureMaterialInstance();
        EnsureTransitionMaterialInstance();
        EnsureStarMaterialInstance();

        Apply();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            Apply();
        }
    }

    private void OnDidApplyAnimationProperties()
    {
        Apply();
    }

    private void OnDisable()
    {
        DisposeRuntimeMaterial();
        DisposeRuntimeTransitionMaterial();
        DisposeRuntimeStarMaterial();
    }

    // ============================================================
    // SPACE BACKGROUND MATERIAL
    // ============================================================

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

    private void DisposeRuntimeMaterial()
    {
        if (runtimeMaterial == null)
            return;

        if (Application.isPlaying)
        {
            Destroy(
                runtimeMaterial
            );
        }
        else
        {
            DestroyImmediate(
                runtimeMaterial
            );
        }

        runtimeMaterial = null;
    }

    // ============================================================
    // TRANSITION MATERIAL
    // ============================================================

    private void EnsureTransitionMaterialInstance()
    {
        if (transitionImage == null)
            return;

        Material baseMaterial =
            transitionSourceMaterial != null
                ? transitionSourceMaterial
                : transitionImage.material;

        if (baseMaterial == null)
            return;

        if (runtimeTransitionMaterial != null &&
            transitionImage.material ==
            runtimeTransitionMaterial)
        {
            return;
        }

        DisposeRuntimeTransitionMaterial();

        runtimeTransitionMaterial =
            Instantiate(
                baseMaterial
            );

        runtimeTransitionMaterial.name =
            baseMaterial.name +
            " (Runtime)";

        transitionImage.material =
            runtimeTransitionMaterial;
    }

    private void DisposeRuntimeTransitionMaterial()
    {
        if (runtimeTransitionMaterial == null)
            return;

        if (Application.isPlaying)
        {
            Destroy(
                runtimeTransitionMaterial
            );
        }
        else
        {
            DestroyImmediate(
                runtimeTransitionMaterial
            );
        }

        runtimeTransitionMaterial = null;
    }

    // ============================================================
    // STAR MATERIAL
    // ============================================================

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

    // ============================================================
    // APPLY
    // ============================================================

    private void Apply()
    {
        // --------------------------------
        // SPACE BACKGROUND
        // --------------------------------

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

        // --------------------------------
        // TRANSITION
        // --------------------------------

        if (runtimeTransitionMaterial != null)
        {
            if (runtimeTransitionMaterial.HasProperty(
                    transitionRevealInProperty))
            {
                runtimeTransitionMaterial.SetFloat(
                    transitionRevealInProperty,
                    transitionRevealIn
                );
            }

            if (runtimeTransitionMaterial.HasProperty(
                    transitionRevealOutProperty))
            {
                runtimeTransitionMaterial.SetFloat(
                    transitionRevealOutProperty,
                    transitionRevealOut
                );
            }
        }

        // --------------------------------
        // STARS
        // --------------------------------

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

    // ============================================================
    // SPACE BACKGROUND API
    // ============================================================

    public void SetReveal(
        float value)
    {
        reveal =
            Mathf.Clamp01(
                value
            );

        EnsureMaterialInstance();
        Apply();
    }

    // ============================================================
    // TRANSITION API
    // ============================================================

    public void SetTransitionRevealIn(
        float value)
    {
        transitionRevealIn =
            Mathf.Clamp01(
                value
            );

        EnsureTransitionMaterialInstance();
        Apply();
    }

    public void SetTransitionRevealOut(
        float value)
    {
        transitionRevealOut =
            Mathf.Clamp01(
                value
            );

        EnsureTransitionMaterialInstance();
        Apply();
    }

    public void ResetTransition()
    {
        transitionRevealIn = 0f;
        transitionRevealOut = 0f;

        EnsureTransitionMaterialInstance();
        Apply();
    }

    // ============================================================
    // STARS
    // ============================================================

    public void SetStarOpacity(
        float value)
    {
        starOpacity =
            Mathf.Clamp01(
                value
            );

        EnsureStarMaterialInstance();
        Apply();
    }

    // ============================================================
    // ANIMATOR
    // ============================================================

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

    // ============================================================
    // TUTORIAL TEXT
    // ============================================================

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
            dialogueDatabase.Get(
                id
            );

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