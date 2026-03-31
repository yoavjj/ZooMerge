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

    private Coroutine galaxySwapRoutine;

    private void Start()
    {
        StartCoroutine(WaitForFirebaseThenRefresh());
    }

    private IEnumerator WaitForFirebaseThenRefresh()
    {
        // Wait until Firebase is ready
        yield return new WaitUntil(() => FirebaseInitializer.IsReady);

        // Now it’s safe
        Refresh();
    }

    private void HandleLevelChanged(int levelNumber)
    {
        ShowLvlStage(levelNumber);
    }

    private void Refresh()
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

    private void ShowGalaxyArt(int galaxyId)
    {
        if (database == null || galaxyArtContainer == null) return;
        if (currentGalaxyShown == galaxyId) return;

        currentGalaxyShown = galaxyId;

        // cleanup old
        if (currentGalaxyInstance != null)
            Destroy(currentGalaxyInstance);

        var galaxyPrefab = database.GetPrefabForGalaxy(galaxyId);
        if (galaxyPrefab == null)
        {
            Debug.LogWarning($"[LevelArtController] No galaxy prefab found for galaxyId {galaxyId}");
            return;
        }

        currentGalaxyInstance = Instantiate(galaxyPrefab, galaxyArtContainer);
        currentGalaxyAnimator = currentGalaxyInstance.GetComponentInChildren<GalaxyColorAnimator>();

        // UI-safe reset (works for both normal Transforms and RectTransforms)
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
        ShowGalaxyArt(MergeLevelManager.CurrentGalaxyId);

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

    public void PlayGalaxyOutAndSwapToNext()
    {
        if (currentGalaxyAnimator == null)
            return;

        // play out (this will Destroy the galaxy GO after 1.5s inside GalaxyColorAnimator)
        currentGalaxyAnimator.PlayOut();

        // schedule spawning the next galaxy after the same delay
        if (galaxySwapRoutine != null)
            StopCoroutine(galaxySwapRoutine);

        galaxySwapRoutine = StartCoroutine(SwapGalaxyAfterDelay(0.5f));
    }

    private IEnumerator SwapGalaxyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Make sure the manager is already updated to the next galaxy BEFORE this runs
        // So this will spawn the correct next galaxy:
        currentGalaxyShown = -1; // force ShowGalaxyArt to run even if id matches
        ShowGalaxyArt(MergeLevelManager.CurrentGalaxyId);

        // play the reveal on the freshly spawned galaxy
        if (currentGalaxyAnimator != null)
            currentGalaxyAnimator.PlayReveal();

        galaxySwapRoutine = null;
    }
}
