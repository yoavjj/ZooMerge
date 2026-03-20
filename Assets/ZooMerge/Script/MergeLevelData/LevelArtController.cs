using System.Collections;
using UnityEngine;

public class LevelArtController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private LevelArtDatabase database;

    [Header("Where the art should spawn/live")]
    [SerializeField] private Transform artContainer;
    private DissolveAnimatorDriver currentInstance;

    [Header("Galaxy Art (Background/Theme)")]
    [SerializeField] private Transform galaxyArtContainer;

    [Header("Galaxy Progress")]
    [SerializeField] private GalaxyProgressSlider galaxyProgress;
    [SerializeField] private bool animateGalaxyProgress = true;

    private GameObject currentGalaxyInstance;
    private int currentGalaxyShown = -1;

    private int currentStageShown = -1;

    private void OnEnable()
    {
        BallEventManager.OnEnemyAdvanced += Refresh; // optional, if your "advance" implies level change elsewhere
        //MergeLevelManager.OnLevelChanged += HandleLevelChanged; // (added below)
    }

    private void OnDisable()
    {
        BallEventManager.OnEnemyAdvanced -= Refresh;
        //MergeLevelManager.OnLevelChanged -= HandleLevelChanged;
    }

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

    private void ShowLvlStage(int stageId)
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

        currentInstance.PlayIdle();
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
}
