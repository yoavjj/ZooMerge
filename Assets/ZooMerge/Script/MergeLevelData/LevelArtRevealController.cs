using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class LevelArtRevealController : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private LevelArtDatabase levelArtDatabase;

    [Header("Containers")]
    [SerializeField] private Transform currentLevelContainer;
    [SerializeField] private Transform nextLevelContainer;

    

    [Header("Optional Reveal Animator")]
    [SerializeField] private Animator revealAnimator;
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private Material sourceMaterial;          // ✅ material asset to use
    [SerializeField] private bool overrideMaterial = true;     // ✅ if true, always apply sourceMaterial
    [SerializeField] private string verticalDissolveProp = "_VerticalDissolve";
    [SerializeField] private float dissolveAmount = 0f;
    [SerializeField] private string revealTrigger = "Reveal";
    [SerializeField] private string revealOutTrigger = "Out";

    private DissolveAnimatorDriver currentInstance;
    private DissolveAnimatorDriver nextInstance;

    // We keep an instance so we don't edit a shared material asset.
    private Material runtimeMat;

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

    private void OnDidApplyAnimationProperties()
    {
        Apply();
    }

    private void Update()
    {
        // In edit mode, keep preview responsive while scrubbing animation
        if (!Application.isPlaying)
            Apply();
    }

    public void PlayRevealAndSwap()
    {
        // 1) Start the reveal animation (the mask / transition object)
        if (revealAnimator != null && !string.IsNullOrEmpty(revealTrigger))
        {
            revealAnimator.ResetTrigger(revealTrigger);
            revealAnimator.SetTrigger(revealTrigger);
        }

        // 2) Tell the spawned art prefabs to run their own dissolve anims
        if (currentInstance != null)
            currentInstance.PlayOut();

        if (nextInstance != null)
            nextInstance.PlayIn();
    }

    private void EnsureMaterialInstance()
    {
        if (targetGraphic == null) return;

        // Decide what base material we should clone
        Material baseMat = null;

        if (overrideMaterial && sourceMaterial != null)
            baseMat = sourceMaterial;
        else
            baseMat = targetGraphic.material;

        if (baseMat == null) return;

        // If we already created and assigned an instance, keep it.
        if (runtimeMat != null && targetGraphic.material == runtimeMat) return;

        // Create a per-object material instance (prevents affecting other UI using same asset).
        runtimeMat = Instantiate(baseMat);
        targetGraphic.material = runtimeMat;
    }

    private void Apply()
    {
        if (targetGraphic == null) return;
        if (targetGraphic.material == null) return;
        if (!targetGraphic.material.HasProperty(verticalDissolveProp)) return;

        targetGraphic.material.SetFloat(verticalDissolveProp, dissolveAmount);
    }

    public void Prepare(int currentLevel)
    {
        ClearContainer(currentLevelContainer, ref currentInstance);
        ClearContainer(nextLevelContainer, ref nextInstance);

        if (levelArtDatabase == null)
        {
            Debug.LogWarning("[LevelArtRevealController] Missing LevelArtDatabase.");
            return;
        }

        // Spawn current
        var curPrefab = levelArtDatabase.GetPrefabForLevel(currentLevel);
        if (curPrefab != null && currentLevelContainer != null)
        {
            currentInstance = Instantiate(curPrefab, currentLevelContainer);
            ResetLocal(currentInstance.transform);

            // ✅ current art should dissolve OUT
            currentInstance.PlayOut();
        }
        else
        {
            Debug.LogWarning($"[LevelArtRevealController] No art prefab found for current level {currentLevel}.");
        }

        // Spawn next
        var nextPrefab = levelArtDatabase.GetPrefabForLevel(currentLevel + 1);
        if (nextPrefab != null && nextLevelContainer != null)
        {
            nextInstance = Instantiate(nextPrefab, nextLevelContainer);
            ResetLocal(nextInstance.transform);

            // ✅ next art should dissolve IN
            nextInstance.PlayIn();
        }
        else
        {
            Debug.LogWarning($"[LevelArtRevealController] No art prefab found for next level {currentLevel + 1}.");
        }
    }

    private void ClearContainer(Transform container, ref DissolveAnimatorDriver instance)
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
            instance = null;
        }

        if (container == null) return;

        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }

    private void ResetLocal(Transform t)
    {
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    public void PlayRevealOut()
    {
        if (revealAnimator == null || string.IsNullOrEmpty(revealOutTrigger))
            return;

        revealAnimator.ResetTrigger(revealOutTrigger);
        revealAnimator.SetTrigger(revealOutTrigger);
    }
}
