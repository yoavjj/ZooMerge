using System.Collections;
using UnityEngine;

public class LevelArtController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private LevelArtDatabase database;
    public LevelArtDatabase Database => database;

    [Header("Where the art should spawn/live")]
    [SerializeField] private Transform artContainer;
    private DissolveAnimatorDriver currentInstance;

    [Header("Galaxy Art (Background/Theme)")]
    [SerializeField] private Transform galaxyArtContainer;

    [Header("Galaxy Progress")]
    [SerializeField] private GalaxyProgressSlider galaxyProgress;
    [SerializeField] private bool animateGalaxyProgress = true;

    private GalaxyColorAnimator currentGalaxyAnimator;

    private GameObject currentGalaxyInstance;
    private int currentGalaxyShown = -1;

    private int currentStageShown = -1;

    private void Start()
    {
        StartCoroutine(WaitForFirebaseThenRefresh());
    }

    private IEnumerator WaitForFirebaseThenRefresh()
    {
        // Wait until Firebase is ready
        yield return new WaitUntil(() => FirebaseInitializer.IsReady);
    }

    private void HandleLevelChanged(int levelNumber)
    {
        ShowLvlStage(levelNumber);
    }

    public void Refresh()
    {
        ShowLvlStage(MergeLevelManager.CurrentStageId);
        ShowGalaxyArt(MergeLevelManager.CurrentGalaxyId);
        UpdateGalaxyProgress();
    }

    protected virtual void ShowLvlStage(int stageId)
    {
        if (database == null || artContainer == null) return;
        if (currentStageShown == stageId) return;

        currentStageShown = stageId;

        if (currentInstance != null)
            Destroy(currentInstance.gameObject);

        var prefab = database.GetPrefabForLevel(stageId);
        if (prefab == null)
        {
            Debug.LogError($"[LevelArtController] No art prefab found for stageId {stageId}");
            return;
        }

        currentInstance = Instantiate(prefab, artContainer);
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;
        currentInstance.transform.localScale = Vector3.one;
    }

    public void ShowPreviousGalaxyArt()
    {
        // previous in QUEUE order, not galaxyId-1
        int prevGalaxyId = MergeLevelManager.GetGalaxyIdAtOffset(-1);

        if (prevGalaxyId < 0)
            return;

        // force respawn even if it thinks it’s already shown
        currentGalaxyShown = -1;
        ShowGalaxyArt(prevGalaxyId);
    }

    public void ShowCurrentGalaxyArt()
    {
        // force respawn even if it thinks it’s already shown
        currentGalaxyShown = -1;
        ShowGalaxyArt(MergeLevelManager.CurrentGalaxyId);
    }

    public void ShowCurrentGalaxyArtAndReveal(float revealDelay = 0.25f)
    {
        // Spawn WITHOUT replacing, and WITHOUT playing idle (so it doesn't show before reveal)
        ShowGalaxyArt(MergeLevelManager.CurrentGalaxyId, replaceExisting: false, playIdleOnSpawn: false);

        if (currentGalaxyAnimator != null)
            StartCoroutine(DelayedGalaxyReveal(revealDelay, currentGalaxyAnimator));
    }

    private IEnumerator DelayedGalaxyReveal(float delay, GalaxyColorAnimator animatorToReveal)
    {
        yield return new WaitForSeconds(delay);

        // In case something changed/destroyed while waiting
        if (animatorToReveal != null)
            animatorToReveal.PlayReveal();
    }

    public void ShowCurrentStageArt()
    {
        ShowLvlStage(MergeLevelManager.CurrentStageId);
    }

    private void ShowGalaxyArt(int galaxyId, bool replaceExisting = true, bool playIdleOnSpawn = true)
    {
        if (database == null || galaxyArtContainer == null) return;

        if (replaceExisting && currentGalaxyShown == galaxyId) return;

        if (replaceExisting)
        {
            currentGalaxyShown = galaxyId;

            if (currentGalaxyInstance != null)
                Destroy(currentGalaxyInstance);
        }

        var galaxyPrefab = database.GetPrefabForGalaxy(galaxyId);
        if (galaxyPrefab == null)
        {
            Debug.LogWarning($"[LevelArtController] No galaxy prefab found for galaxyId {galaxyId}");
            return;
        }

        var spawned = Instantiate(galaxyPrefab, galaxyArtContainer);

        currentGalaxyInstance = spawned;
        currentGalaxyAnimator = spawned.GetComponent<GalaxyColorAnimator>();

        // UI-safe reset
        var rt = spawned.transform as RectTransform;
        if (rt != null)
        {
            rt.anchoredPosition3D = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }
        else
        {
            spawned.transform.localPosition = Vector3.zero;
            spawned.transform.localRotation = Quaternion.identity;
            spawned.transform.localScale = Vector3.one;
        }

        // ✅ NEW: default behavior = go idle right away
        if (playIdleOnSpawn && currentGalaxyAnimator != null)
            currentGalaxyAnimator.PlayIdle();
    }
    private void UpdateGalaxyProgress()
    {
        if (galaxyProgress == null) return;

        galaxyProgress.SetLevelIndexProgress(
            MergeLevelManager.CurrentLevelInGalaxy,
            MergeLevelManager.LevelsInCurrentGalaxy,
            animateGalaxyProgress
        );
    }

    public void PlayGalaxyDone()
    {
        if (currentGalaxyAnimator != null)
            currentGalaxyAnimator.PlayDone();
    }

    public void ForceShowCurrentStageArt()
    {
        // Force it even if it thinks it's already showing the same stage
        currentStageShown = -1;
        ShowLvlStage(MergeLevelManager.CurrentStageId);
    }

    public void StageReveal()
    {
        if (currentInstance == null)
            return;

        ShowLvlStage(MergeLevelManager.CurrentStageId);

        // This sets the "In" trigger on the Animator inside DissolveAnimatorDriver
        currentInstance.PlayIn();
    }

    public void CallGalaxySlider()
    {
        UpdateGalaxyProgress();
    }

    public void PlayIdle()
    {
        if (currentInstance == null)
            return;

        currentInstance.PlayIdle();
    }

    public void PlayGalaxyOut()
    {
        if (currentGalaxyAnimator == null)
            return;

        currentGalaxyAnimator.PlayOut();
    }
}
