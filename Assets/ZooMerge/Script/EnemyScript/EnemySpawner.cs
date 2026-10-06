using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; } // 🔹 Singleton reference

    private BallFactoryAddressables.SpawnedEnemy warmedEnemy;

    private int warmedEnemyId = -1;
    private int warmingEnemyId = -1;

    private Coroutine warmupRoutine;

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

    public void ClearEnemy( float delay = 2f)
    {
        if (delayedEnterRoutine != null)
        {
            StopCoroutine(delayedEnterRoutine);
            delayedEnterRoutine = null;
        }

        if (currentEnemy.root != null)
        {
            EnemySessionTracker.Unregister(currentEnemy.root);
            Destroy(currentEnemy.root, delay);
        }
        currentEnemy = default;
    }

    public void SpawnEnemy(int enemyId, bool delayEnter = false)
    {
        if (IsEnemyWarm(enemyId))
        {
            currentEnemy = warmedEnemy;

            warmedEnemy = default;
            warmedEnemyId = -1;

            Transform t =
                currentEnemy.root.transform;

            Transform parent =
                enemyContainer != null
                    ? enemyContainer
                    : transform;

            t.SetParent(parent, false);

            t.position =
                spawnPoint != null
                    ? spawnPoint.position
                    : transform.position;

            t.localRotation =
                Quaternion.identity;

            // The expensive instance already exists.
            currentEnemy.root.SetActive(true);

            Debug.Log(
                $"[EnemySpawner] Using prewarmed enemy {enemyId}."
            );
        }
        else
        {
            currentEnemy =
                BallFactoryAddressables.Instance
                    .SpawnEnemyWithRefs(
                        enemyId,
                        spawnPoint != null
                            ? spawnPoint.position
                            : transform.position,
                        enemyContainer != null
                            ? enemyContainer
                            : transform
                    );
        }

        if (currentEnemy.IsValid)
        {
            // Snap to container origin in local space
            var t = currentEnemy.root.transform;
            if (enemyContainer != null)
            {
                if (t.parent != enemyContainer)
                    t.SetParent(enemyContainer, false); // keep local transform

                t.localPosition = new Vector3(0f, 0f, t.localPosition.z);
                t.localRotation = Quaternion.identity; // optional
                                                       // t.localScale = Vector3.one;         // optional
            }

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

    private IEnumerator DelayedEnter(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayEnterOnCurrentEnemy();
        delayedEnterRoutine = null;
    }

    public void NotifyEnemyDestroyed(GameObject root)
    {
        ClearEnemy();
    }

    public bool IsEnemyWarm(int enemyId)
    {
        return
            warmedEnemyId == enemyId &&
            warmedEnemy.IsValid;
    }

    public bool IsWarmingEnemy(int enemyId)
    {
        return
            warmupRoutine != null &&
            warmingEnemyId == enemyId;
    }

    public void WarmupEnemy(int enemyId)
    {
        if (IsEnemyWarm(enemyId))
            return;

        if (IsWarmingEnemy(enemyId))
            return;

        if (warmupRoutine != null)
            return;

        warmingEnemyId = enemyId;

        warmupRoutine =
            StartCoroutine(
                WarmupEnemyRoutine(enemyId)
            );
    }

    private IEnumerator
    WarmupEnemyRoutine(int enemyId)
    {
        BallFactoryAddressables.SpawnedEnemy result =
            default;

        if (BallFactoryAddressables.Instance == null)
        {
            warmingEnemyId = -1;
            warmupRoutine = null;
            yield break;
        }

        yield return BallFactoryAddressables.Instance
            .PrewarmEnemy(
                enemyId,
                enemyContainer,
                enemy =>
                {
                    result = enemy;
                }
            );

        if (result.IsValid)
        {
            warmedEnemy = result;
            warmedEnemyId = enemyId;

            Debug.Log(
                $"[EnemySpawner] Enemy {enemyId} prewarmed."
            );
        }
        else
        {
            Debug.LogWarning(
                $"[EnemySpawner] Failed to prewarm enemy {enemyId}."
            );
        }

        warmingEnemyId = -1;
        warmupRoutine = null;
    }
}
