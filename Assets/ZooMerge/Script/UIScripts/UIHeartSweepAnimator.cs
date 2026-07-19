using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class UIHeartSweepAnimator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Graphic targetGraphic; // Image / RawImage / etc.

    [Header("Material")]
    [SerializeField] private Material sourceMaterial; // material using Custom/UI/HeartLightSweep
    [SerializeField] private bool overrideMaterial = true;

    [Header("Shader Property")]
    [SerializeField] private string sweepProperty = "_Sweep01";

    [Header("Animated Value (keyframe this)")]
    [Range(0, 1)]
    [SerializeField] private float sweep01 = 0f;

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
        // In edit mode, Update is how you see changes live without entering Play mode.
        if (!Application.isPlaying)
            Apply();
    }

    // ✅ This is the key piece for Animator keyframes
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

        if (mat.HasProperty(sweepProperty))
            mat.SetFloat(sweepProperty, sweep01);
    }

    // Optional helper (same idea as your SetBlend)
    public void SetSweep(float value)
    {
        sweep01 = Mathf.Clamp01(value);
        EnsureMaterialInstance();
        Apply();
    }
}