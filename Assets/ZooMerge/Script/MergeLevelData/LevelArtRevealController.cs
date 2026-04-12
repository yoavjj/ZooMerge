using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class LevelArtRevealController : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private LevelArtDatabase levelArtDatabase;
    [SerializeField] LevelProgressBarSlider progressBarSlider; // optional, just to trigger slider update when reveal happens

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

    [Header("Galaxy Art (Reveal)")]
    [SerializeField] private Transform galaxyArtContainer;          // where to spawn galaxy prefab inside this reveal
    [SerializeField] private bool showGalaxyArtInReveal = true;

    [Header("Galaxy Progress (Reveal)")]
    [SerializeField] private GalaxyProgressSlider galaxyProgress;   // your helper component
    [SerializeField] private bool animateGalaxyProgressOnEvent = true;

    private GalaxyColorAnimator currentGalaxyAnimator;

    private GameObject currentGalaxyInstance;
    private int currentGalaxyShown = -1;

    // cached progress for animation event
    private int cachedFromCompleted = 0;
    private int cachedToCompleted = 0;
    private int cachedTotalLevels = 1;



    // We keep an instance so we don't edit a shared material asset.
    private Material runtimeMat;

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
        if (revealAnimator != null && !string.IsNullOrEmpty(revealTrigger))
        {
            revealAnimator.ResetTrigger(revealTrigger);
            revealAnimator.SetTrigger(revealTrigger);
        }
    }

    // Animation Event #1
    public void AE_CurrentOut()
    {
        if (currentInstance != null)
            currentInstance.PlayOut();
    }

    // Animation Event #2
    public void AE_NextIn()
    {
        if (nextInstance != null)
            nextInstance.PlayIn();
    }

    // Animation Event: call this from the reveal timeline when you want the bar to fill
    public void AE_AnimateGalaxyProgress()
    {
        if (galaxyProgress == null) return;
        galaxyProgress.SetCompletedProgress(cachedToCompleted, cachedTotalLevels, animate: true);

        // Remove previous to avoid stacking
        galaxyProgress.OnAnimationComplete = null;

        galaxyProgress.OnAnimationComplete = OnGalaxyProgressFinished;
    }

    public void AE_OnRevealFinished()
    {
        PopupManager.Instance?.BeginSessionDeferred(isNewLevel: true);
    }
    
    private void OnGalaxyProgressFinished()
    {
        // Only trigger if fully completed
        if (cachedToCompleted >= cachedTotalLevels)
        {
            if (currentGalaxyAnimator != null)
                currentGalaxyAnimator.PlayDone();
        }
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

    public void Prepare(int currentLevel, bool afterCompletion)
    {
        ClearContainer(currentLevelContainer, ref currentInstance);
        ClearContainer(nextLevelContainer, ref nextInstance);

        if (levelArtDatabase == null)
        {
            Debug.LogWarning("[LevelArtRevealController] Missing LevelArtDatabase.");
            return;
        }

        // ✅ Get CURRENT stageId (offset 0)
        int currentStageId = MergeLevelManager.GetStageIdAtOffset(0);

        var curPrefab = levelArtDatabase.GetPrefabForLevel(currentStageId);
        if (curPrefab != null && currentLevelContainer != null)
        {
            currentInstance = Instantiate(curPrefab, currentLevelContainer);
            ResetLocal(currentInstance.transform);

            currentInstance.PrimeAsOut();
        }
        else
        {
            Debug.LogWarning($"[LevelArtRevealController] No art prefab found for stageId {currentStageId}.");
        }

        // ✅ Get NEXT stageId (offset 1)
        int nextStageId = MergeLevelManager.GetStageIdAtOffset(1);

        var nextPrefab = levelArtDatabase.GetPrefabForLevel(nextStageId);
        if (nextPrefab != null && nextLevelContainer != null)
        {
            nextInstance = Instantiate(nextPrefab, nextLevelContainer);
            ResetLocal(nextInstance.transform);

            nextInstance.PrimeAsIn();
        }
        else
        {
            Debug.LogWarning($"[LevelArtRevealController] No art prefab found for stageId {nextStageId}.");
        }

        PrepareGalaxyArtAndProgress(afterCompletion);
    }

    private void PrepareGalaxyArtAndProgress(bool afterCompletion)
    {
        int total = MergeLevelManager.LevelsInCurrentGalaxy;        // e.g. 3
        int levelInGalaxy = MergeLevelManager.CurrentLevelInGalaxy; // 1..N (CURRENT)

        cachedTotalLevels = Mathf.Max(1, total);

        // ✅ completed levels for display:
        // afterCompletion=true  -> just finished current level => completed = levelInGalaxy (1 -> "1/3")
        // afterCompletion=false -> not completed yet => completed = levelInGalaxy - 1 (1 -> "0/3")
        int toCompleted = afterCompletion ? levelInGalaxy : Mathf.Max(0, levelInGalaxy - 1);
        int fromCompleted = Mathf.Max(0, toCompleted - 1);

        // ✅ cache ints so AE_AnimateGalaxyProgress does NOT read updated level state later
        cachedFromCompleted = Mathf.Clamp(fromCompleted, 0, cachedTotalLevels);
        cachedToCompleted = Mathf.Clamp(toCompleted, 0, cachedTotalLevels);

        // ✅ Set slider/text to FROM so the animation event goes from FROM -> TO
        if (galaxyProgress != null)
        {
            galaxyProgress.SetCompletedProgress(cachedFromCompleted, cachedTotalLevels, animate: false);
        }

        // ---- Galaxy art prefab spawning (unchanged) ----
        if (!showGalaxyArtInReveal) return;
        if (galaxyArtContainer == null || levelArtDatabase == null) return;

        int galaxyId = MergeLevelManager.CurrentGalaxyId;
        if (currentGalaxyShown == galaxyId) return;

        currentGalaxyShown = galaxyId;

        if (currentGalaxyInstance != null)
            Destroy(currentGalaxyInstance);

        var galaxyPrefab = levelArtDatabase.GetPrefabForGalaxy(galaxyId);
        if (galaxyPrefab == null)
        {
            Debug.LogWarning($"[LevelArtRevealController] No galaxy prefab found for galaxyId {galaxyId}.");
            return;
        }

        currentGalaxyInstance = Instantiate(galaxyPrefab, galaxyArtContainer);

        // ✅ NO GetComponent — use serialized reference on prefab root
        currentGalaxyAnimator = currentGalaxyInstance.GetComponent<GalaxyColorAnimator>();
        if (currentGalaxyAnimator != null)
            currentGalaxyAnimator.PlayIdle();

        var rt = currentGalaxyInstance.transform as RectTransform;
        if (rt != null)
        {
            rt.anchoredPosition3D = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }
        else
        {
            currentGalaxyInstance.transform.localPosition = Vector3.zero;
            currentGalaxyInstance.transform.localRotation = Quaternion.identity;
            currentGalaxyInstance.transform.localScale = Vector3.one;
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

    public void updateProgressBarSlider()
    {
        progressBarSlider.InitializeCurrentLevel();
    }
}
