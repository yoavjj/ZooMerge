using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "BallSet", menuName = "Game/Ball Set")]
public class BallSet : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id;                              // e.g., "Prefab_Doggy_1"
        public AssetReferenceGameObject prefab;
        [Range(0f, 10f)] public float weight = 1f;
        public int level = 0;                          // <-- the ONLY key we use for physics
        public bool includeInRandom = true;
        public BallType type = BallType.Bug;
    }

    [Serializable]
    public class BallPhysicsData
    {
        public int id; // <-- level number (0..N)
        public float finalLinearDamping = 5f;
        public float finalAngularDamping = 5f;

        [Header("Gravity")]
        [Min(0f)] public float gravityStart = 0.3f; // starting gravity when settle begins
        [Min(0f)] public float gravityEnd = 0.45f; // final gravity after settle

        [Header("Scale")]
        [Min(0.01f)] public float uniformScale = 1f;
    }

    public List<Entry> entries = new List<Entry>();
    public List<BallPhysicsData> physicsData = new List<BallPhysicsData>();

    /// Find physics strictly by level.
    public BallPhysicsData GetPhysicsFor(Entry entry)
    {
        return physicsData.Find(p => p != null && p.id == entry.level);
    }

#if UNITY_EDITOR
    // Optional: keep physics data in sync with levels you use
    private void OnValidate()
    {
        // collect used levels
        var usedLevels = new HashSet<int>();
        foreach (var e in entries)
            if (e != null) usedLevels.Add(e.level);

        // add missing physics rows with defaults
        foreach (var lvl in usedLevels)
        {
            if (!physicsData.Exists(p => p != null && p.id == lvl))
            {
                physicsData.Add(new BallPhysicsData { id = lvl });
            }
        }

        // (nice-to-have) sort by id
        physicsData.Sort((a, b) => (a?.id ?? -1).CompareTo(b?.id ?? -1));
    }
#endif
}

public enum BallType { Bug, Turtle, Doggy }
