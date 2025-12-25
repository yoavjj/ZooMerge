using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class BallFactoryAddressables : MonoBehaviour, IBallFactory
{
    /// <summary>
    /// Fully cached references for a spawned ball.
    /// Allows zero GetComponent calls in BallSpawner.
    /// </summary>
    public struct SpawnedBall
    {
        public GameObject root;
        public BallInfo info;
        public CircleDropController controller;
        public Animator animator;
        public Rigidbody2D[] allRigidbodies;
        public Collider2D[] allColliders;

        public bool IsValid =>
            root != null && info != null && controller != null && animator != null;
    }

    public static BallFactoryAddressables Instance { get; private set; }
    public static event Action<BallFactoryAddressables> OnReady;

    [Header("Refs")]
    [SerializeField] private AddressableInstantiator instantiator;
    [SerializeField] private BallSet ballSet;
    [SerializeField] private Transform droppedContainer;

    public BallSet BallSet => ballSet;

    // type -> (level -> prefab)
    private readonly Dictionary<BallType, Dictionary<int, BallSet.Entry>> map = new();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[BallFactoryAddressables] Duplicate instance, destroying.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        map.Clear();
        if (ballSet != null)
        {
            foreach (var e in ballSet.entries)
            {
                if (e == null || e.prefab == null) continue;
                if (!map.TryGetValue(e.type, out var levels))
                {
                    levels = new Dictionary<int, BallSet.Entry>();
                    map[e.type] = levels;
                }
                levels[e.level] = e;
            }
        }
        else
        {
            Debug.LogError("[BallFactoryAddressables] BallSet not assigned.");
        }

        OnReady?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public SpawnedBall SpawnLevelWithRefs(BallType type, int level, Vector3 position, Transform parentOverride = null)
    {
        if (!map.TryGetValue(type, out var levels) || !levels.TryGetValue(level, out var entry))
        {
            Debug.LogWarning($"[BallFactory] No prefab for {type} level {level}");
            return default;
        }

        return SpawnEntryWithRefs(entry, position, parentOverride);
    }

    public SpawnedBall SpawnEntryWithRefs(BallSet.Entry entry, Vector3 position, Transform parentOverride = null)
    {
        SpawnedBall result = new SpawnedBall();

        if (entry == null || entry.prefab == null)
        {
            Debug.LogWarning("[BallFactory] Entry or prefab is null");
            return result;
        }

        GameObject go = instantiator.SpawnAssetAt(entry.prefab, position, parentOverride);

        if (go == null)
        {
            Debug.LogError("[BallFactory] Failed to spawn prefab.");
            return result;
        }

        // Parent correction
        if (parentOverride == null && droppedContainer != null)
            go.transform.SetParent(droppedContainer, worldPositionStays: true);

        // CACHE ALL COMPONENTS ONCE (no GetComponent in BallSpawner)
        var info = go.GetComponent<BallInfo>();
        var controller = go.GetComponentInChildren<CircleDropController>(true);
        var animator = go.GetComponentInChildren<Animator>(true);

        if (info == null || controller == null || animator == null)
        {
            Debug.LogError("[BallFactory] Missing required components on spawned ball!");
            return result;
        }

        // Setup physics
        var physics = ballSet.GetPhysicsFor(entry);
        if (physics != null)
        {
            info.Setup(entry.level, entry.type,
                       physics.finalLinearDamping, physics.finalAngularDamping,
                       physics.gravityStart, physics.gravityEnd,
                       physics.uniformScale);
        }
        else
        {
            Debug.LogError($"[BallFactory] No physics data found for level '{entry.level}'");
        }

        // Fill the struct
        result.root = go;
        result.info = info;
        result.controller = controller;
        result.animator = animator;

        // 🔹 Cache physics components for preview toggling
        result.allRigidbodies = go.GetComponentsInChildren<Rigidbody2D>(true);
        result.allColliders = go.GetComponentsInChildren<Collider2D>(true);

        return result;
    }


    public void Despawn(GameObject go)
    {
        if (go != null) Destroy(go);
    }

    internal object SpawnEntry(BallSet.Entry queuedEntry, object pos, Transform previewContainer)
    {
        throw new NotImplementedException();
    }

    public BallSet.BallPhysicsData GetPhysicsFor(BallType type, int level)
    {
        if (map.TryGetValue(type, out var levels) &&
            levels.TryGetValue(level, out var entry))
        {
            return ballSet.GetPhysicsFor(entry);
        }

        return null;
    }

    // ---------- Enemy Utilities ----------

    public struct SpawnedEnemy
    {
        public GameObject root;
        public EnemyUnit unit;
        public Spine.Unity.SkeletonGraphic spineGraphic;
        public Animator animator;

        public bool IsValid => root != null && unit != null;
    }

    public SpawnedEnemy SpawnEnemyWithRefs(int enemyId, Vector3 position, Transform parentOverride = null)
    {
        var result = new SpawnedEnemy();

        // find by id in BallSet.enemyPrefabs
        string idString = enemyId.ToString();
        var enemyRef = ballSet.enemyPrefabs.Find(e => e.id == idString);
        if (enemyRef == null || enemyRef.prefab == null)
        {
            Debug.LogError($"[BallFactory] Enemy prefab not found for ID: {enemyId}");
            return result;
        }

        var go = instantiator.SpawnAssetAt(enemyRef.prefab, position, parentOverride);
        if (go == null) return result;

        // cache once here (centralized)
        var unit = go.GetComponent<EnemyUnit>();
        var sg = go.GetComponentInChildren<Spine.Unity.SkeletonGraphic>(true);
        var anim = go.GetComponentInChildren<Animator>(true);

        if (unit == null)
        {
            Debug.LogError("[BallFactory] EnemyUnit missing on enemy prefab.");
            return result;
        }

        result.root = go;
        result.unit = unit;
        result.spineGraphic = sg;
        result.animator = anim;
        return result;
    }
}
