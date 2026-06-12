using UnityEngine;

[CreateAssetMenu(
    fileName = "CollectibleFlyDatabase",
    menuName = "ZooMerge/Collectibles/Fly Database"
)]
public class CollectibleFlyDatabaseSO : ScriptableObject
{
    [System.Serializable]
    public class FlyEntry
    {
        public string id;
        public FlyCollectiblePrefabRoot prefabRoot;
        public CollectibleFlightSettings settings;

        [Min(0f)] public float preSpawnDelay = 0.15f;
        public bool useUnscaledTime = true;

        [Header("Canvas Conversion")]
        public bool useSpawnContainerCanvasCamera = true;
    }

    public FlyEntry[] entries;

    public FlyEntry Find(string id)
    {
        if (entries == null) return null;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && entries[i].id == id)
                return entries[i];
        }

        return null;
    }
}