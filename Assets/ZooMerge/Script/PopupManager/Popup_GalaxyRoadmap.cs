using UnityEngine;

public class Popup_GalaxyRoadmap : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private LevelArtDatabase levelArtDatabase;

    [Header("Containers")]
    [SerializeField] private Transform currentGalaxyContainer;
    [SerializeField] private Transform nextGalaxyContainer;
    [SerializeField] private Transform nextNextGalaxyContainer;

    private GameObject currentInstance;
    private GameObject nextInstance;
    private GameObject nextNextInstance;

    public void Initialize()
    {
        if (levelArtDatabase == null)
        {
            Debug.LogError("[GalaxyRoadmap] Missing LevelArtDatabase");
            return;
        }

        Clear(ref currentInstance);
        Clear(ref nextInstance);
        Clear(ref nextNextInstance);

        int currentGalaxyId = MergeLevelManager.CurrentGalaxyId;

        // ✅ current
        SpawnGalaxy(currentGalaxyId, currentGalaxyContainer, ref currentInstance);

        // ✅ next
        SpawnGalaxy(currentGalaxyId + 1, nextGalaxyContainer, ref nextInstance);

        // ✅ next next
        SpawnGalaxy(currentGalaxyId + 2, nextNextGalaxyContainer, ref nextNextInstance);
    }

    private void SpawnGalaxy(int galaxyId, Transform parent, ref GameObject instance)
    {
        if (parent == null) return;

        var prefab = levelArtDatabase.GetRoadmapPrefabForGalaxy(galaxyId);
        if (prefab == null)
        {
            Debug.LogWarning($"[GalaxyRoadmap] No roadmap prefab for galaxyId {galaxyId}");
            return;
        }

        instance = Instantiate(prefab, parent);

        // reset transform (UI safe)
        var rt = instance.transform as RectTransform;
        if (rt != null)
        {
            rt.anchoredPosition3D = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }
        else
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }
    }

    private void Clear(ref GameObject go)
    {
        if (go != null)
        {
            Destroy(go);
            go = null;
        }
    }
}