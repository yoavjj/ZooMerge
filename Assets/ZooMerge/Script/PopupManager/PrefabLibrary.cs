using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Prefab Library", fileName = "PrefabLibrary")]
public class PrefabLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id;
        public GameObject prefab; // ✅ now generic
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<string, GameObject> cache;

    private void OnEnable()
    {
        RebuildCache();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildCache();
    }
#endif

    private void RebuildCache()
    {
        cache ??= new Dictionary<string, GameObject>();
        cache.Clear();

        if (entries == null) return;

        foreach (var e in entries)
        {
            if (e == null) continue;
            if (string.IsNullOrEmpty(e.id)) continue;
            if (e.prefab == null) continue;

            cache[e.id] = e.prefab;
        }
    }

    // 🔹 Raw access (if needed)
    public GameObject GetRaw(string id)
    {
        if (cache == null) RebuildCache();

        if (cache.TryGetValue(id, out var prefab) && prefab != null)
            return prefab;

        Debug.LogError($"[PrefabLibrary] No prefab found for id: {id}");
        return null;
    }

    // 🔹 Typed: Win/Lose content
    public WinLoseContentBase GetWinLose(string id)
    {
        var go = GetRaw(id);
        return go != null ? go.GetComponent<WinLoseContentBase>() : null;
    }

    // 🔹 Typed: Level Reveal
    public LevelArtRevealController GetLevelReveal(string id)
    {
        var go = GetRaw(id);
        return go != null ? go.GetComponent<LevelArtRevealController>() : null;
    }
}