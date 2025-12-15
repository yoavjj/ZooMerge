using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; } // 🔹 Singleton reference

    private Coroutine delayedEnterRoutine;

    [Header("Refs")]
    [SerializeField] private BallSet ballSet;           // Contains enemy prefabs
    [SerializeField] private Transform spawnPoint;      // Spawn location
    [SerializeField] private Transform enemyContainer;

    [Header("Settings")]
    [SerializeField] private float enterDelayEnemySeconds = 1.0f;

    private BallFactoryAddressables.SpawnedEnemy currentEnemy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void PlayEnterOnCurrentEnemy()
    {
        currentEnemy.unit?.PlayEnter();
    }

    public void ClearEnemy()
    {
        if (delayedEnterRoutine != null)
        {
            StopCoroutine(delayedEnterRoutine);
            delayedEnterRoutine = null;
        }

        if (currentEnemy.root != null)
        {
            EnemySessionTracker.Unregister(currentEnemy.root);
            Destroy(currentEnemy.root);
        }
        currentEnemy = default;
    }

    public void SpawnEnemy(int enemyId, bool delayEnter = false)
    {
        currentEnemy = BallFactoryAddressables.Instance
            .SpawnEnemyWithRefs(enemyId,
                spawnPoint != null ? spawnPoint.position : transform.position,
                enemyContainer != null ? enemyContainer : transform);

        if (currentEnemy.IsValid)
        {
            EnemySessionTracker.Register(currentEnemy.root);
            Debug.Log($"[EnemySpawner] Spawned enemy ID {enemyId}.");

            if (delayEnter && enterDelayEnemySeconds > 0f)
            {
                if (delayedEnterRoutine != null) StopCoroutine(delayedEnterRoutine);
                delayedEnterRoutine = StartCoroutine(DelayedEnter(enterDelayEnemySeconds));
            }
            else
            {
                PlayEnterOnCurrentEnemy();
            }
        }
        else
        {
            Debug.LogError("[EnemySpawner] Failed to spawn enemy.");
        }
    }

    private System.Collections.IEnumerator DelayedEnter(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayEnterOnCurrentEnemy();
        delayedEnterRoutine = null;
    }

    public void NotifyEnemyDestroyed(GameObject root)
    {
        ClearEnemy();
    }
}
