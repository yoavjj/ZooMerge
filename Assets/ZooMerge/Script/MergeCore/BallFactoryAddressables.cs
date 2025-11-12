using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class BallFactoryAddressables : MonoBehaviour, IBallFactory
{
    public static BallFactoryAddressables Instance { get; private set; }
    public static event Action<BallFactoryAddressables> OnReady;

    [Header("Refs")]
    [SerializeField] private AddressableInstantiator instantiator;
    [SerializeField] private BallSet ballSet;
    [SerializeField] private Transform droppedContainer;

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

    public BallInfo SpawnLevel(BallType type, int level, Vector3 position, Transform parentOverride = null)
    {
        if (!map.TryGetValue(type, out var levels) || !levels.TryGetValue(level, out var entry))
        {
            Debug.LogWarning($"[BallFactory] No prefab for {type} level {level}");
            return null;
        }

        return SpawnEntry(entry, position, parentOverride);
    }

    public BallInfo SpawnEntry(BallSet.Entry entry, Vector3 position, Transform parentOverride = null)
    {
        if (entry == null || entry.prefab == null)
        {
            Debug.LogWarning("[BallFactory] Entry or prefab is null");
            return null;
        }

        var go = instantiator.SpawnAssetAt(entry.prefab, position, parentOverride);
        if (go == null)
        {
            Debug.LogError("[BallFactory] Failed to spawn prefab.");
            return null;
        }

        // ✅ Parent correction
        if (parentOverride == null && droppedContainer != null)
            go.transform.SetParent(droppedContainer, worldPositionStays: true);

        // ✅ BallInfo is on the root (best practice)
        var info = go.GetComponent<BallInfo>();
        if (info == null)
        {
            Debug.LogWarning($"[BallFactory] Spawned prefab '{go.name}' missing BallInfo!");
            return null;
        }

        // ✅ Setup physics
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

        return info;
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
}
