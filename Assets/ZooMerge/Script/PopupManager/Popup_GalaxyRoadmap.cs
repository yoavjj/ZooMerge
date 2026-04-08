using System;
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

    [SerializeField] private LevelProgressBarSlider levelProgressBar;

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

    public event Action OnClosedRoadmap;

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
        if (levelArtController == null || levelArtController.Database == null)
        {
            Debug.LogError("[GalaxyRoadmap] Missing LevelArtController or its LevelArtDatabase");
            return;
        }

        Clear(ref currentInstance);
        Clear(ref nextInstance);
        Clear(ref nextNextInstance);

        int baseGalaxyId = MergeLevelManager.CurrentGalaxyId;
        if (isRevealMode)
            baseGalaxyId += 1;

        int id0 = WrapGalaxyId(baseGalaxyId);
        int id1 = WrapGalaxyId(id0 + 1);
        int id2 = WrapGalaxyId(id1 + 1);

        SpawnGalaxy(id0, currentGalaxyContainer, ref currentInstance, GalaxyRoadmapPrefabConfigurator.Slot.Current);
        SpawnGalaxy(id1, nextGalaxyContainer, ref nextInstance, GalaxyRoadmapPrefabConfigurator.Slot.Next);
        SpawnGalaxy(id2, nextNextGalaxyContainer, ref nextNextInstance, GalaxyRoadmapPrefabConfigurator.Slot.NextNext);

        levelArtController.ShowCurrentGalaxyArt();
    }

    private int WrapGalaxyId(int requestedGalaxyId)
    {
        var db = levelArtController?.Database;
        if (db == null) return requestedGalaxyId;

        // If this one exists, use it
        if (db.GetRoadmapPrefabForGalaxy(requestedGalaxyId) != null)
            return requestedGalaxyId;

        // Otherwise, loop forward until we find something that exists.
        // (Hard safety cap so we never infinite loop)
        const int maxSteps = 1000;
        int id = requestedGalaxyId;

        for (int i = 0; i < maxSteps; i++)
        {
            id++;

            if (db.GetRoadmapPrefabForGalaxy(id) != null)
                return id;
        }

        // Fallback: give back requested if nothing found
        return requestedGalaxyId;
    }

    private void SpawnGalaxy(
    int galaxyId,
    Transform parent,
    ref GalaxyRoadmapPrefabConfigurator instance,
    GalaxyRoadmapPrefabConfigurator.Slot slot)
    {
        if (parent == null) return;

        var prefab = levelArtController.Database.GetRoadmapPrefabForGalaxy(galaxyId);
        if (prefab == null)
        {
            Debug.LogWarning($"[GalaxyRoadmap] No roadmap prefab for galaxyId {galaxyId}");
            return;
        }

        instance = Instantiate(prefab, parent);  // typed instantiate
        instance.Configure(slot, isRevealMode);

        // ✅ Set the correct text for THIS galaxyId (fixes reveal mode)
        string galaxyName = MergeLevelManager.GetGalaxyNameById(galaxyId);
        instance.SetGalaxyName(galaxyName, show: slot == GalaxyRoadmapPrefabConfigurator.Slot.Current);

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

        currentGalaxyAnimator = instance.GetComponent<GalaxyColorAnimator>();
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
            levelArtController?.ShowPreviousGalaxyArt();

            levelArtController?.ShowCurrentStageArt();

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
        yield return new WaitForSeconds(2.5f);

        Destroy(gameObject);
    }

    public void ClosePopup()
    {
        // Prevent double-close calls
        if (!gameObject) return;

        OnClosedRoadmap?.Invoke();
    }

    public void AE_AnimateGalaxyProgress()
    {
        if (galaxyProgress == null) return;

        galaxyProgress.SetCompletedProgress(cachedCompleted, cachedTotal, animate: true);

        // ✅ IMPORTANT — same as working script
        galaxyProgress.OnAnimationComplete = null;
        galaxyProgress.OnAnimationComplete = OnGalaxyProgressFinished;

        levelProgressBar.InitializeCurrentLevel();
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

    public void AE_StageReveal()
    {
        levelArtController?.StageReveal();
    }

    private void OnGalaxyProgressFinished()
    {
        if (cachedCompleted >= cachedTotal)
        {
            levelArtController.PlayGalaxyDone();
        }
    }

    public void AE_UpdateGalaxySlider()
    {
        levelArtController?.PlayGalaxyOut();
        levelArtController?.CallGalaxySlider();

        // ✅ show current galaxy art + reveal animation
        levelArtController?.ShowCurrentGalaxyArtAndReveal();
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