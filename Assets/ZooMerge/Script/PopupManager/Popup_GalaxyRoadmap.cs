using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class Popup_GalaxyRoadmap : MonoBehaviour
{
    [Header("Galaxy Progress")]
    [SerializeField] private GalaxyProgressSlider galaxyProgress;
    [SerializeField] private LevelArtController levelArtController;
    private GalaxyColorAnimator currentGalaxyAnimator;

    [Header("Database")]
    [SerializeField] private LevelArtDatabase levelArtDatabase;

    [Header("Containers")]
    [SerializeField] private Transform currentGalaxyContainer;
    [SerializeField] private Transform nextGalaxyContainer;
    [SerializeField] private Transform nextNextGalaxyContainer;

    private GalaxyRoadmapPrefabConfigurator currentInstance;
    private GalaxyRoadmapPrefabConfigurator nextInstance;
    private GalaxyRoadmapPrefabConfigurator nextNextInstance;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [SerializeField] private string popupTrigger = "Popup";
    [SerializeField] private string revealTrigger = "Reveal";

    [Header("Auto Close (Reveal Only)")]
    [SerializeField] private float autoCloseDelay = 5f;
    [SerializeField] private string closeTrigger = "Close";

    private Coroutine autoCloseRoutine;

    [Header("Optional Dissolve")]
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private Material sourceMaterial;
    [SerializeField] private bool overrideMaterial = true;
    [SerializeField] private string verticalDissolveProp = "_VerticalDissolve";
    [SerializeField] private float dissolveAmount = 0f;

    private Material runtimeMat;
    private Material originalMaterial;

    private int cachedCompleted;
    private int cachedTotal;

    private bool isRevealMode;

    private void Update()
    {
        if (!Application.isPlaying)
        {
            EnsureMaterialInstance();
            ApplyDissolve();
        }
    }

    private void OnEnable()
    {
        EnsureMaterialInstance();
        ApplyDissolve();
    }

    private void OnValidate()
    {
        EnsureMaterialInstance();
        ApplyDissolve();
    }

    public void Initialize()
    {
        if (levelArtDatabase == null)
        {
            Debug.LogError("[GalaxyRoadmap] Missing LevelArtDatabase");
            return;
        }

        Clear(ref currentInstance);
        Clear(ref nextInstance);
        Clear(ref nextNextInstance);

        // Base galaxy
        int baseGalaxyId = MergeLevelManager.CurrentGalaxyId;

        // SHIFT when coming from reveal flow
        if (isRevealMode)
            baseGalaxyId += 1;

        // 🔥 Spawn shifted galaxies
        SpawnGalaxy(baseGalaxyId, currentGalaxyContainer, ref currentInstance, GalaxyRoadmapPrefabConfigurator.Slot.Current);
        SpawnGalaxy(baseGalaxyId + 1, nextGalaxyContainer, ref nextInstance, GalaxyRoadmapPrefabConfigurator.Slot.Next);
        SpawnGalaxy(baseGalaxyId + 2, nextNextGalaxyContainer, ref nextNextInstance, GalaxyRoadmapPrefabConfigurator.Slot.NextNext);
    }

    private void SpawnGalaxy(
    int galaxyId,
    Transform parent,
    ref GalaxyRoadmapPrefabConfigurator instance,
    GalaxyRoadmapPrefabConfigurator.Slot slot)
    {
        if (parent == null) return;

        var prefab = levelArtDatabase.GetRoadmapPrefabForGalaxy(galaxyId);
        if (prefab == null)
        {
            Debug.LogWarning($"[GalaxyRoadmap] No roadmap prefab for galaxyId {galaxyId}");
            return;
        }

        instance = Instantiate(prefab, parent);  // typed instantiate
        instance.Configure(slot, isRevealMode);

        // reset transform (UI safe)
        var rt = instance.transform as RectTransform;
        if (rt != null)
        {
            rt.anchoredPosition3D = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }
        else
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }

        currentGalaxyAnimator = instance.GetComponentInChildren<GalaxyColorAnimator>();
    }

    private void Clear(ref GalaxyRoadmapPrefabConfigurator inst)
    {
        if (inst != null)
        {
            Destroy(inst.gameObject);
            inst = null;
        }
    }

    public void PlayIntro(bool fromLevelFlow)
    {
        if (animator == null)
            return;

        isRevealMode = fromLevelFlow;

        string trigger = fromLevelFlow ? revealTrigger : popupTrigger;

        if (fromLevelFlow)
        {
            EnsureMaterialInstance();

            // ✅ Start auto close
            if (autoCloseRoutine != null)
                StopCoroutine(autoCloseRoutine);

            autoCloseRoutine = StartCoroutine(AutoCloseRoutine());
        }
        else
        {
            RestoreOriginalMaterial();
        }

        animator.SetTrigger(trigger);
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);

        if (animator != null && !string.IsNullOrEmpty(closeTrigger))
        {
            animator.SetTrigger(closeTrigger);
        }

        // optional: small delay to let animation play
        yield return new WaitForSeconds(0.5f);

        Destroy(gameObject);
    }

    public void AE_AnimateGalaxyProgress()
    {
        if (galaxyProgress == null) return;

        galaxyProgress.SetCompletedProgress(cachedCompleted, cachedTotal, animate: true);

        // ✅ IMPORTANT — same as working script
        galaxyProgress.OnAnimationComplete = null;
        galaxyProgress.OnAnimationComplete = OnGalaxyProgressFinished;
    }

    public void AE_PlayGalaxyReveal()
    {
        // Only needed if you want to trigger AFTER spawn
        if (currentInstance != null)
            currentInstance.PlayReveal();
    }

    public void AE_OnRevealFinished()
    {
        PopupManager.Instance?.BeginSession(isNewLevel: true);
        PopupManager.Instance?.InitializeProgressBarNow();
    }

    private void OnGalaxyProgressFinished()
    {
        if (cachedCompleted >= cachedTotal)
        {
            levelArtController.PlayGalaxyDone();
        }
    }

    private void EnsureMaterialInstance()
    {
        if (targetGraphic == null) return;

        // ✅ store original ONCE
        if (originalMaterial == null)
            originalMaterial = targetGraphic.material;

        Material baseMat = null;

        if (overrideMaterial && sourceMaterial != null)
            baseMat = sourceMaterial;
        else
            baseMat = targetGraphic.material;

        if (baseMat == null) return;

        if (runtimeMat != null && targetGraphic.material == runtimeMat)
            return;

        runtimeMat = Instantiate(baseMat);
        targetGraphic.material = runtimeMat;
    }

    private void ApplyDissolve()
    {
        if (runtimeMat == null) return;
        if (!runtimeMat.HasProperty(verticalDissolveProp)) return;

        runtimeMat.SetFloat(verticalDissolveProp, dissolveAmount);
    }

    private void OnDidApplyAnimationProperties()
    {
        ApplyDissolve();
    }

    private void RestoreOriginalMaterial()
    {
        if (targetGraphic == null) return;

        if (originalMaterial != null)
            targetGraphic.material = originalMaterial;

        runtimeMat = null;
    }

    public void PrepareProgressBeforeReveal()
    {
        cachedTotal = Mathf.Max(1, MergeLevelManager.LevelsInCurrentGalaxy);

        cachedCompleted = Mathf.Clamp(
            MergeLevelManager.CurrentLevelInGalaxy,
            0,
            cachedTotal
        );

        if (galaxyProgress != null)
        {
            // ✅ FORCE start state BEFORE animation
            int from = Mathf.Max(0, cachedCompleted - 1);

            galaxyProgress.SetCompletedProgress(from, cachedTotal, animate: false);
        }
    }
}