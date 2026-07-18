using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Game/Prefab Library",
    fileName = "PrefabLibrary"
)]
public class PrefabLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id;
        public GameObject prefab;
    }

    [Header("Prefabs")]
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

        if (entries == null)
            return;

        foreach (Entry entry in entries)
        {
            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.id))
                continue;

            if (entry.prefab == null)
                continue;

            if (cache.ContainsKey(entry.id))
            {
                Debug.LogWarning(
                    $"[PrefabLibrary] Duplicate prefab id: {entry.id}. " +
                    "The later entry will replace the earlier one."
                );
            }

            cache[entry.id] = entry.prefab;
        }
    }

    public GameObject GetRaw(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogError(
                "[PrefabLibrary] Cannot retrieve a prefab with an empty id."
            );

            return null;
        }

        if (cache == null)
            RebuildCache();

        if (cache.TryGetValue(
                id,
                out GameObject prefab) &&
            prefab != null)
        {
            return prefab;
        }

        Debug.LogError(
            $"[PrefabLibrary] No prefab found for id: {id}"
        );

        return null;
    }

    public BallUnlockPopup GetBallUnlockPopup(string id)
    {
        GameObject prefab = GetRaw(id);

        if (prefab == null)
            return null;

        if (prefab.TryGetComponent(
                out BallUnlockPopup unlockPopup))
        {
            return unlockPopup;
        }

        Debug.LogError(
            $"[PrefabLibrary] Prefab with id '{id}' " +
            $"does not have {nameof(BallUnlockPopup)} on its root."
        );

        return null;
    }

    public WinLoseContentBase GetWinLose(string id)
    {
        GameObject prefab = GetRaw(id);

        if (prefab == null)
            return null;

        if (prefab.TryGetComponent(
                out WinLoseContentBase content))
        {
            return content;
        }

        Debug.LogError(
            $"[PrefabLibrary] Prefab with id '{id}' " +
            $"does not have {nameof(WinLoseContentBase)} on its root."
        );

        return null;
    }

    public LevelArtRevealController GetLevelReveal(string id)
    {
        GameObject prefab = GetRaw(id);

        if (prefab == null)
            return null;

        if (prefab.TryGetComponent(
                out LevelArtRevealController controller))
        {
            return controller;
        }

        Debug.LogError(
            $"[PrefabLibrary] Prefab with id '{id}' " +
            $"does not have {nameof(LevelArtRevealController)} on its root."
        );

        return null;
    }

    public Popup_GalaxyRoadmap GetGalaxyRoadmap(string id)
    {
        GameObject prefab = GetRaw(id);

        if (prefab == null)
            return null;

        if (prefab.TryGetComponent(
                out Popup_GalaxyRoadmap roadmap))
        {
            return roadmap;
        }

        Debug.LogError(
            $"[PrefabLibrary] Prefab with id '{id}' " +
            $"does not have {nameof(Popup_GalaxyRoadmap)} on its root."
        );

        return null;
    }
}