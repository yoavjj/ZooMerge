using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ParticleOpacityAnimator : MonoBehaviour
{
    [Header("Particle A")]
    [SerializeField]
    private ParticleSystemRenderer targetRendererA;

    [SerializeField]
    private Material sourceMaterialA;

    [Range(0f, 1f)]
    [SerializeField]
    private float opacityA;

    [Header("Particle B")]
    [SerializeField]
    private ParticleSystemRenderer targetRendererB;

    [SerializeField]
    private Material sourceMaterialB;

    [Range(0f, 1f)]
    [SerializeField]
    private float opacityB;

    [Header("Shader")]
    [SerializeField]
    private string opacityProperty =
        "_Opacity";

    [Header("Hologram Reveal")]
    [SerializeField]
    private Image hologramImage;

    [SerializeField]
    private Material hologramSourceMaterial;

    [SerializeField]
    private string hologramRevealProperty =
        "_Reveal";

    [Range(0f, 1f)]
    [SerializeField]
    private float hologramReveal;

    private Material runtimeHologramMaterial;

    private Material runtimeMaterialA;
    private Material runtimeMaterialB;

    private void OnEnable()
    {
        EnsureMaterialInstances();
        EnsureHologramMaterialInstance();

        Apply();
    }

    private void OnValidate()
    {
        EnsureMaterialInstances();
        EnsureHologramMaterialInstance();

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
        DisposeRuntimeMaterials();
    }

    public void SetOpacityA(float value)
    {
        opacityA =
            Mathf.Clamp01(value);

        EnsureMaterialInstances();
        Apply();
    }

    public void SetOpacityB(float value)
    {
        opacityB =
            Mathf.Clamp01(value);

        EnsureMaterialInstances();
        Apply();
    }

    public void SetHologramReveal(
    float value)
    {
        hologramReveal =
            Mathf.Clamp01(value);

        EnsureHologramMaterialInstance();
        Apply();
    }

    public void ResetOpacity()
    {
        opacityA = 0f;
        opacityB = 0f;
        hologramReveal = 0f;

        EnsureMaterialInstances();
        EnsureHologramMaterialInstance();

        Apply();
    }

    private void EnsureHologramMaterialInstance()
    {
        if (hologramImage == null)
            return;

        Material baseMaterial =
            hologramSourceMaterial != null
                ? hologramSourceMaterial
                : hologramImage.material;

        if (baseMaterial == null)
            return;

        if (runtimeHologramMaterial != null &&
            hologramImage.material ==
            runtimeHologramMaterial)
        {
            return;
        }

        DisposeMaterial(
            ref runtimeHologramMaterial
        );

        runtimeHologramMaterial =
            Instantiate(
                baseMaterial
            );

        runtimeHologramMaterial.name =
            baseMaterial.name +
            " (Runtime)";

        hologramImage.material =
            runtimeHologramMaterial;
    }

    private void EnsureMaterialInstances()
    {
        EnsureMaterialInstance(
            targetRendererA,
            sourceMaterialA,
            ref runtimeMaterialA
        );

        EnsureMaterialInstance(
            targetRendererB,
            sourceMaterialB,
            ref runtimeMaterialB
        );
    }

    private void EnsureMaterialInstance(
        ParticleSystemRenderer renderer,
        Material sourceMaterial,
        ref Material runtimeMaterial)
    {
        if (renderer == null)
            return;

        Material baseMaterial =
            sourceMaterial != null
                ? sourceMaterial
                : renderer.sharedMaterial;

        if (baseMaterial == null)
            return;

        if (runtimeMaterial != null &&
            renderer.sharedMaterial ==
            runtimeMaterial)
        {
            return;
        }

        DisposeMaterial(
            ref runtimeMaterial
        );

        runtimeMaterial =
            Instantiate(
                baseMaterial
            );

        runtimeMaterial.name =
            baseMaterial.name +
            " (Runtime)";

        renderer.sharedMaterial =
            runtimeMaterial;
    }

    private void Apply()
    {
        ApplyOpacity(
            runtimeMaterialA,
            opacityA
        );

        ApplyOpacity(
            runtimeMaterialB,
            opacityB
        );

        ApplyHologramReveal();
    }

    private void ApplyHologramReveal()
    {
        if (runtimeHologramMaterial == null)
            return;

        if (!runtimeHologramMaterial.HasProperty(
                hologramRevealProperty))
        {
            return;
        }

        runtimeHologramMaterial.SetFloat(
            hologramRevealProperty,
            hologramReveal
        );
    }

    private void ApplyOpacity(
        Material material,
        float opacity)
    {
        if (material == null)
            return;

        if (!material.HasProperty(
                opacityProperty))
        {
            return;
        }

        material.SetFloat(
            opacityProperty,
            opacity
        );
    }

    private void DisposeRuntimeMaterials()
    {
        DisposeMaterial(
            ref runtimeMaterialA
        );

        DisposeMaterial(
            ref runtimeMaterialB
        );

        DisposeMaterial(
            ref runtimeHologramMaterial
        );
    }

    private void DisposeMaterial(
        ref Material material)
    {
        if (material == null)
            return;

        if (Application.isPlaying)
        {
            Destroy(
                material
            );
        }
        else
        {
            DestroyImmediate(
                material
            );
        }

        material = null;
    }
}