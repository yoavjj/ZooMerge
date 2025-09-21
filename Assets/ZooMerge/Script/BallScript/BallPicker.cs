using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class BallPicker : MonoBehaviour
{
    [Header("Catalog")]
    [SerializeField] private BallSet ballSet;

    [Header("Random Range (levels)")]
    public int minLevel = 0;  // X
    public int maxLevel = 1;  // Y (inclusive)

    public AssetReferenceGameObject PickRandom()
    {
        if (ballSet == null || ballSet.entries == null || ballSet.entries.Count == 0)
            return null;

        // Filter by include + level range
        var pool = new List<BallSet.Entry>();
        float totalWeight = 0f;

        foreach (var e in ballSet.entries)
        {
            if (e == null || e.prefab == null) continue;
            if (!e.includeInRandom) continue;
            if (e.level < minLevel || e.level > maxLevel) continue;
            if (e.weight <= 0f) continue;

            pool.Add(e);
            totalWeight += e.weight;
        }

        if (pool.Count == 0 || totalWeight <= 0f) return null;

        // Weighted pick
        float r = Random.value * totalWeight;
        foreach (var e in pool)
        {
            r -= e.weight;
            if (r <= 0f)
                return e.prefab;
        }

        // Fallback
        return pool[pool.Count - 1].prefab;
    }

    public bool TryPickRandomEntry(out BallSet.Entry entry, out string reason)
    {
        reason = "";
        entry = null;

        if (ballSet == null || ballSet.entries == null || ballSet.entries.Count == 0)
        {
            reason = "No entries in ballSet.";
            return false;
        }

        var valid = ballSet.entries.FindAll(e => e != null && e.includeInRandom && e.prefab != null);
        if (valid.Count == 0)
        {
            reason = "No valid ball entries.";
            return false;
        }

        entry = valid[Random.Range(0, valid.Count)];
        return true;
    }

    public bool TryPickRandom(out AssetReferenceGameObject prefab, out string reason)
    {
        prefab = null;
        reason = "";

        if (ballSet == null) { reason = "BallSet not assigned."; return false; }
        if (ballSet.entries == null || ballSet.entries.Count == 0) { reason = "BallSet has no entries."; return false; }

        var pool = new List<BallSet.Entry>();
        float totalWeight = 0f;

        foreach (var e in ballSet.entries)
        {
            if (e == null) continue;
            if (e.prefab == null) continue;
            if (!e.includeInRandom) continue;
            if (e.level < minLevel || e.level > maxLevel) continue;
            if (e.weight <= 0f) continue;

            pool.Add(e);
            totalWeight += e.weight;
        }

        if (pool.Count == 0) { reason = $"No entries in level range [{minLevel}..{maxLevel}] with includeInRandom=true and weight>0."; return false; }
        if (totalWeight <= 0f) { reason = "Total weight is 0."; return false; }

        float r = Random.value * totalWeight;
        foreach (var e in pool)
        {
            r -= e.weight;
            if (r <= 0f) { prefab = e.prefab; return true; }
        }

        prefab = pool[pool.Count - 1].prefab;
        return true;
    }

    public float GetScaleForEntry(BallSet.Entry entry)
    {
        if (entry == null || ballSet == null) return 1f;

        var data = ballSet.GetPhysicsFor(entry);
        if (data == null) return 1f;

        return data.uniformScale > 0f ? data.uniformScale : 1f;
    }
}
