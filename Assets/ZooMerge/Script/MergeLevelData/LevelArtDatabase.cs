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
        public DissolveAnimatorDriver artPrefab; // level art prefab (has DissolveAnimatorDriver)
    }

    [Serializable]
    public class GalaxyEntry
    {
        public int galaxyId;
        public GameObject galaxyPrefab; // any prefab you want (background, VFX, etc.)
        public GameObject galaxyRoadmapPrefab;
    }

    [Header("Level Art")]
    [SerializeField] private List<Entry> entries = new();

    [Header("Galaxy Art")]
    [SerializeField] private List<GalaxyEntry> galaxyEntries = new();

    private Dictionary<int, DissolveAnimatorDriver> levelCache;
    private Dictionary<int, GameObject> galaxyCache;
    private Dictionary<int, GameObject> galaxyRoadmapCache;

    private void OnEnable() => RebuildCaches();

#if UNITY_EDITOR
    private void OnValidate() => RebuildCaches();
#endif

    private void RebuildCaches()
    {
        // ---- level cache ----
        levelCache ??= new Dictionary<int, DissolveAnimatorDriver>();
        levelCache.Clear();

        if (entries != null)
        {
            foreach (var e in entries)
            {
                if (e == null) continue;
                if (e.level <= 0) continue;
                if (e.artPrefab == null) continue;

                levelCache[e.level] = e.artPrefab;
            }
        }

        // ---- galaxy cache ----
        galaxyCache ??= new Dictionary<int, GameObject>();
        galaxyCache.Clear();

        if (galaxyEntries != null)
        {
            foreach (var e in galaxyEntries)
            {
                if (e == null) continue;
                if (e.galaxyId <= 0) continue;
                if (e.galaxyPrefab == null) continue;

                galaxyCache[e.galaxyId] = e.galaxyPrefab;
            }
        }

        // ---- galaxy roadmap cache ----
        galaxyRoadmapCache ??= new Dictionary<int, GameObject>();
        galaxyRoadmapCache.Clear();

        if (galaxyEntries != null)
        {
            foreach (var e in galaxyEntries)
            {
                if (e == null) continue;
                if (e.galaxyId <= 0) continue;
                if (e.galaxyRoadmapPrefab == null) continue;

                galaxyRoadmapCache[e.galaxyId] = e.galaxyRoadmapPrefab;
            }
        }
    }

    // ---------- LEVEL ----------
    public DissolveAnimatorDriver GetPrefabForLevel(int level)
    {
        if (levelCache == null) RebuildCaches();

        if (levelCache.TryGetValue(level, out var prefab) && prefab != null)
            return prefab;

        // fallback scan
        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null) continue;
                if (e.level != level) continue;
                if (e.artPrefab == null) continue;

                levelCache ??= new Dictionary<int, DissolveAnimatorDriver>();
                levelCache[level] = e.artPrefab;
                return e.artPrefab;
            }
        }

        Debug.LogError($"[LevelArtDatabase] No LEVEL prefab for level {level}. LevelsInAsset=[{GetLevelsDebug()}]");
        return null;
    }

    // ---------- GALAXY ----------
    public GameObject GetPrefabForGalaxy(int galaxyId)
    {
        if (galaxyCache == null) RebuildCaches();

        if (galaxyCache.TryGetValue(galaxyId, out var prefab) && prefab != null)
            return prefab;

        // fallback scan
        if (galaxyEntries != null)
        {
            for (int i = 0; i < galaxyEntries.Count; i++)
            {
                var e = galaxyEntries[i];
                if (e == null) continue;
                if (e.galaxyId != galaxyId) continue;
                if (e.galaxyPrefab == null) continue;

                galaxyCache ??= new Dictionary<int, GameObject>();
                galaxyCache[galaxyId] = e.galaxyPrefab;
                return e.galaxyPrefab;
            }
        }

        Debug.LogError($"[LevelArtDatabase] No GALAXY prefab for galaxyId {galaxyId}. GalaxiesInAsset=[{GetGalaxiesDebug()}]");
        return null;
    }

    public GameObject GetRoadmapPrefabForGalaxy(int galaxyId)
    {
        if (galaxyRoadmapCache == null) RebuildCaches();

        if (galaxyRoadmapCache.TryGetValue(galaxyId, out var prefab) && prefab != null)
            return prefab;

        // fallback scan
        if (galaxyEntries != null)
        {
            for (int i = 0; i < galaxyEntries.Count; i++)
            {
                var e = galaxyEntries[i];
                if (e == null) continue;
                if (e.galaxyId != galaxyId) continue;
                if (e.galaxyRoadmapPrefab == null) continue;

                galaxyRoadmapCache ??= new Dictionary<int, GameObject>();
                galaxyRoadmapCache[galaxyId] = e.galaxyRoadmapPrefab;
                return e.galaxyRoadmapPrefab;
            }
        }

        Debug.LogWarning($"[LevelArtDatabase] No ROADMAP prefab for galaxyId {galaxyId}");
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

    private string GetGalaxiesDebug()
    {
        if (galaxyEntries == null) return "";
        var list = new List<string>(galaxyEntries.Count);
        foreach (var e in galaxyEntries)
        {
            if (e == null) continue;
            list.Add($"{e.galaxyId}:{(e.galaxyPrefab != null ? "OK" : "NULL")}");
        }
        return string.Join(", ", list);
    }
}