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
        public string id;
        public AssetReferenceGameObject prefab;
        [Range(0f, 10f)] public float weight = 1f;
        public int level = 0;                // e.g. 0..N (X..Y will refer to these)
        public bool includeInRandom = true;  // toggle participation
    }

    public List<Entry> entries = new List<Entry>();
}