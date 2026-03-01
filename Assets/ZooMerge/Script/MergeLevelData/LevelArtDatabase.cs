using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Merge/Level Art Database", fileName = "LevelArtDatabase")]
public class LevelArtDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public int level;
        public DissolveAnimatorDriver artPrefab; // component on the prefab asset
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<int, DissolveAnimatorDriver> cache;

    private void OnEnable() => RebuildCache();

#if UNITY_EDITOR
    private void OnValidate() => RebuildCache();
#endif

    private void RebuildCache()
    {
        cache ??= new Dictionary<int, DissolveAnimatorDriver>();
        cache.Clear();

        if (entries == null) return;

        foreach (var e in entries)
        {
            if (e == null) continue;
            if (e.level <= 0) continue;
            if (e.artPrefab == null) continue;

            cache[e.level] = e.artPrefab;
        }
    }

    public DissolveAnimatorDriver GetPrefabForLevel(int level)
    {
        // 1) Try cache
        if (cache == null) RebuildCache();
        if (cache != null && cache.TryGetValue(level, out var prefab) && prefab != null)
            return prefab;

        // 2) ✅ Hard fallback: scan entries (this will work even if cache is stale)
        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null) continue;
                if (e.level != level) continue;
                if (e.artPrefab == null) continue;

                // keep cache in sync
                cache ??= new Dictionary<int, DissolveAnimatorDriver>();
                cache[level] = e.artPrefab;

                return e.artPrefab;
            }
        }

        // 3) 🔎 Debug: show what the asset *actually* contains at runtime
        Debug.LogError(
            $"[LevelArtDatabase] No prefab for level {level}. " +
            $"EntriesCount={(entries == null ? 0 : entries.Count)}. " +
            $"LevelsInAsset=[{GetLevelsDebug()}]"
        );

        return null;
    }

    private string GetLevelsDebug()
    {
        if (entries == null) return "";
        var list = new List<string>(entries.Count);
        foreach (var e in entries)
        {
            if (e == null) continue;
            list.Add($"{e.level}:{(e.artPrefab != null ? "OK" : "NULL")}");
        }
        return string.Join(", ", list);
    }
}
