using UnityEngine;
using System.Collections.Generic;

public class ParticleEffectPooler : MonoBehaviour
{
    [System.Serializable]
    public class EffectEntry
    {
        public string key;
        public GameObject prefab;
        
        public bool useRequestedPosition = true;   // true = move to requested position, false = stay at container
        public Transform containerOverride;
    }

    [Header("Setup")]
    [SerializeField] private List<EffectEntry> effects;
    [SerializeField] private Transform poolContainer;
    private Dictionary<string, Transform> containerLookup = new();

    private Dictionary<string, Queue<GameObject>> pool = new();
    private Dictionary<string, GameObject> prefabLookup = new();

    private Dictionary<string, EffectEntry> entryLookup = new();

    private void OnEnable()
    {
        ParticleEvents.OnParticleRequested += Play;
    }

    private void OnDisable()
    {
        ParticleEvents.OnParticleRequested -= Play;
    }

    private void Awake()
    {
        foreach (var e in effects)
        {
            if (!string.IsNullOrEmpty(e.key) && e.prefab != null)
            {
                prefabLookup[e.key] = e.prefab;
                pool[e.key] = new Queue<GameObject>();

                // ✅ ADDED
                entryLookup[e.key] = e;
            }
        }
    }

    private void Play(string key, Vector3 position)
    {
        if (!prefabLookup.TryGetValue(key, out var prefab)) return;

        // ✅ ADDED
        entryLookup.TryGetValue(key, out var entry);

        var go = GetOrCreateFromPool(key, prefab, entry);

        // ✅ CHANGED: only move if allowed
        if (entry == null || entry.useRequestedPosition)
            go.transform.position = position;

        go.SetActive(true);

        float duration = 2f;
        var ps = go.GetComponent<ParticleSystem>();
        if (ps) duration = ps.main.duration + ps.main.startLifetime.constantMax;

        StartCoroutine(DisableAfter(go, duration, key));
    }

    private GameObject GetOrCreateFromPool(string key, GameObject prefab, EffectEntry entry)
    {
        if (pool[key].Count > 0)
        {
            var go = pool[key].Dequeue();

            // ✅ ADDED: ensure correct parent for this effect
            var parent = (entry != null && entry.containerOverride != null)
                ? entry.containerOverride
                : (poolContainer != null ? poolContainer : transform);

            go.transform.SetParent(parent, worldPositionStays: true);
            return go;
        }

        // ✅ CHANGED: use per-effect container if provided
        var parentNew = (entry != null && entry.containerOverride != null)
            ? entry.containerOverride
            : (poolContainer != null ? poolContainer : transform);

        return Instantiate(prefab, parentNew);
    }

    private System.Collections.IEnumerator DisableAfter(GameObject go, float delay, string key)
    {
        yield return new WaitForSeconds(delay);
        go.SetActive(false);
        pool[key].Enqueue(go);
    }
}

