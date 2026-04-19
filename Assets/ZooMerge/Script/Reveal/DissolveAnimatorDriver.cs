using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class DissolveAnimatorDriver : MonoBehaviour
{
    [Header("Target (UI)")]
    [SerializeField] private Graphic targetGraphic; // Image / RawImage / TMP's Graphic
    [SerializeField] private string dissolveProp = "_DissolveAmount";
    [SerializeField] private Material inMaterial;    // ✅ material to use when playing IN
    [SerializeField] private Material outMaterial;   // ✅ material to use when playing OUT
    [SerializeField, HideInInspector] private Material lastBaseMaterial;
    [SerializeField] private bool overrideMaterial = true; // keep this
    public enum StartMaterialMode { In, Out }
    [SerializeField] private StartMaterialMode startMode = StartMaterialMode.In; // ✅ which material to clone on enable

    [Header("Animated Value (keyframe this in Animator)")]
    [SerializeField] private float dissolveAmount = 0f;

    [Header("Animator (assign in prefab, no GetComponent)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string inTrigger = "In";
    [SerializeField] private string outTrigger = "Out";
    [SerializeField] private string idleTrigger = "Idle";

    // We keep an instance so we don't edit a shared material asset.
    private Material runtimeMat;

    private void Reset()
    {
        // Auto-pick on add (you already had this)
        targetGraphic = GetComponent<Graphic>();
    }

    private void OnEnable()
    {
        EnsureMaterialInstance();
        Apply();
    }

    private void OnDisable()
    {
        // Optional: cleanup in play mode
        if (Application.isPlaying && runtimeMat != null)
        {
            Destroy(runtimeMat);
            runtimeMat = null;
        }
    }

    private void OnValidate()
    {
        EnsureMaterialInstance();
        Apply();
    }

    private void Update()
    {
        // In edit mode, keep preview responsive while scrubbing animation
        if (!Application.isPlaying)
            Apply();
    }

    private void OnDidApplyAnimationProperties()
    {
        // Animator updated dissolveAmount this frame -> push it to the material
        Apply();
    }

    private void EnsureMaterialInstance()
    {
        if (targetGraphic == null) return;

        Material baseMat = null;

        if (overrideMaterial)
        {
            // ✅ Choose base material by startMode (THIS is the key change)
            if (startMode == StartMaterialMode.Out)
                baseMat = outMaterial != null ? outMaterial : inMaterial;
            else
                baseMat = inMaterial != null ? inMaterial : outMaterial;
        }

        if (baseMat == null)
            baseMat = targetGraphic.material;

        if (baseMat == null) return;

        if (runtimeMat != null && targetGraphic.material == runtimeMat && lastBaseMaterial == baseMat)
            return;

        runtimeMat = Instantiate(baseMat);
        targetGraphic.material = runtimeMat;
        lastBaseMaterial = baseMat;
    }

    private void SetMaterialBase(Material baseMat)
    {
        if (targetGraphic == null || baseMat == null) return;

        // ✅ Only skip if we're already using this exact base material
        if (runtimeMat != null && targetGraphic.material == runtimeMat && lastBaseMaterial == baseMat)
            return;

        if (Application.isPlaying && runtimeMat != null)
            Destroy(runtimeMat);
        else if (!Application.isPlaying && runtimeMat != null)
            DestroyImmediate(runtimeMat);

        runtimeMat = Instantiate(baseMat);
        targetGraphic.material = runtimeMat;
        lastBaseMaterial = baseMat;
        Apply(); // push current dissolveAmount onto the new mat
    }

    public void PrimeAsIn()
    {
        startMode = StartMaterialMode.In;
        if (overrideMaterial && inMaterial != null) SetMaterialBase(inMaterial);
        PlayIdle();
    }

    public void PrimeAsOut()
    {
        startMode = StartMaterialMode.Out;
        if (overrideMaterial && outMaterial != null) SetMaterialBase(outMaterial);
        PlayIdle();
    }

    // Keyframe this if you want, or call via Animation Event
    public void SetDissolve(float v)
    {
        dissolveAmount = v;
        Apply();
    }

    public void PlayIn()
    {
        if (animator == null || string.IsNullOrEmpty(inTrigger)) return;
        animator.ResetTrigger(outTrigger);
        animator.ResetTrigger(inTrigger);
        animator.SetTrigger(inTrigger);
    }

    public void PlayOut()
    {
        if (animator == null || string.IsNullOrEmpty(outTrigger)) return;
        animator.ResetTrigger(inTrigger);
        animator.ResetTrigger(outTrigger);
        animator.SetTrigger(outTrigger);
    }

    private void Apply()
    {
        if (targetGraphic == null) return;
        if (targetGraphic.material == null) return;
        if (!targetGraphic.material.HasProperty(dissolveProp)) return;

        targetGraphic.material.SetFloat(dissolveProp, dissolveAmount);
    }

    public void PlayIdle()
    {
        if (animator == null || string.IsNullOrEmpty(idleTrigger)) return;

        // Optional: clear other triggers so Idle is deterministic
        if (!string.IsNullOrEmpty(inTrigger)) animator.ResetTrigger(inTrigger);
        if (!string.IsNullOrEmpty(outTrigger)) animator.ResetTrigger(outTrigger);

        animator.ResetTrigger(idleTrigger);
        animator.SetTrigger(idleTrigger);
    }
}
