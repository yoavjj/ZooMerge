using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; } // 🔹 Singleton reference

    [Header("Refs")]
    [SerializeField] private BallSet ballSet;           // Contains enemy prefabs
    [SerializeField] private Transform spawnPoint;      // Spawn location
    [SerializeField] private Transform enemyContainer;  // Optional parent

    private GameObject currentEnemy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        //BallEventManager.OnSessionStarted += SpawnSingleEnemy;
    }

    private void OnDisable()
    {
        //BallEventManager.OnSessionStarted -= SpawnSingleEnemy;
    }

    private void SpawnSingleEnemy()
    {
        var level = MergeLevelManager.GetCurrentLevel();

        if (level == null || level.enemy_data == null || level.enemy_data.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] No enemies defined for current level.");
            return;
        }

        int enemyId = level.enemy_data[0].id; // Only take the first enemy's ID
        string idString = enemyId.ToString();

        var enemyRef = ballSet.enemyPrefabs.Find(e => e.id == idString);
        if (enemyRef == null || enemyRef.prefab == null)
        {
            Debug.LogError($"[EnemySpawner] Enemy prefab not found for ID: {enemyId}");
            return;
        }

        var handle = enemyRef.prefab.InstantiateAsync(
            spawnPoint != null ? spawnPoint.position : transform.position,
            Quaternion.identity,
            enemyContainer != null ? enemyContainer : transform
        );

        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                currentEnemy = op.Result;
                Debug.Log($"[EnemySpawner] Spawned enemy ID {enemyId}.");
            }
            else
            {
                Debug.LogError("[EnemySpawner] Failed to spawn enemy prefab.");
            }
        };
    }

    public void ClearEnemy()
    {
        if (currentEnemy != null)
        {
            Destroy(currentEnemy);
            currentEnemy = null;
        }
    }

    public void SpawnEnemy(int enemyId)
    {
        ClearEnemy(); // destroy previous

        string idString = enemyId.ToString();
        var enemyRef = ballSet.enemyPrefabs.Find(e => e.id == idString);
        if (enemyRef == null || enemyRef.prefab == null)
        {
            Debug.LogError($"[EnemySpawner] Enemy prefab not found for ID: {enemyId}");
            return;
        }

        var handle = enemyRef.prefab.InstantiateAsync(
            spawnPoint != null ? spawnPoint.position : transform.position,
            Quaternion.identity,
            enemyContainer != null ? enemyContainer : transform
        );

        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                currentEnemy = op.Result;
                Debug.Log($"[EnemySpawner] Spawned enemy ID {enemyId}.");
            }
        };
    }
}
