using UnityEngine;

public class LevelArtController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private LevelArtDatabase database;

    [Header("Where the art should spawn/live")]
    [SerializeField] private Transform artContainer;

    private DissolveAnimatorDriver currentInstance;

    private int currentLevelShown = -1;

    private void OnEnable()
    {
        BallEventManager.OnEnemyAdvanced += Refresh; // optional, if your "advance" implies level change elsewhere
        MergeLevelManager.OnLevelChanged += HandleLevelChanged; // (added below)
    }

    private void OnDisable()
    {
        BallEventManager.OnEnemyAdvanced -= Refresh;
        MergeLevelManager.OnLevelChanged -= HandleLevelChanged;
    }

    private void Start()
    {
        // In case this object loads after Firebase/level init
        Refresh();
    }

    private void HandleLevelChanged(int levelNumber)
    {
        Show(levelNumber);
    }

    private void Refresh()
    {
        if (!FirebaseInitializer.IsReady) return;
        Show(MergeLevelManager.CurrentLevelNumber);
    }

    private void Show(int levelNumber)
    {
        if (database == null || artContainer == null) return;
        if (currentLevelShown == levelNumber) return;

        currentLevelShown = levelNumber;

        // cleanup old
        if (currentInstance != null)
            Destroy(currentInstance.gameObject);

        var prefab = database.GetPrefabForLevel(levelNumber);
        if (prefab == null)
        {
            Debug.LogError($"[LevelArtController] No art prefab found for level {levelNumber}");
            return;
        }

        currentInstance = Instantiate(prefab, artContainer);
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;
        currentInstance.transform.localScale = Vector3.one;

        currentInstance.PlayIdle();
    }
}
