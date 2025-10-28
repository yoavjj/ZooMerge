using UnityEngine;
using System.Collections.Generic;

public class ParticleEffectPooler : MonoBehaviour
{
    [System.Serializable]
    public class EffectEntry
    {
        public string key;
        public GameObject prefab;
    }

    [Header("Setup")]
    [SerializeField] private List<EffectEntry> effects;
    [SerializeField] private Transform poolContainer;

    private Dictionary<string, Queue<GameObject>> pool = new();
    private Dictionary<string, GameObject> prefabLookup = new();

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
            }
        }
    }

    private void Play(string key, Vector3 position)
    {
        if (!prefabLookup.TryGetValue(key, out var prefab)) return;

        var go = GetOrCreateFromPool(key, prefab);
        go.transform.position = position;
        go.SetActive(true);

        float duration = 2f;
        var ps = go.GetComponent<ParticleSystem>();
        if (ps) duration = ps.main.duration + ps.main.startLifetime.constantMax;

        StartCoroutine(DisableAfter(go, duration, key));
    }

    private GameObject GetOrCreateFromPool(string key, GameObject prefab)
    {
        if (pool[key].Count > 0)
        {
            return pool[key].Dequeue();
        }

        var parent = poolContainer != null ? poolContainer : transform;
        return Instantiate(prefab, parent);
    }

    private System.Collections.IEnumerator DisableAfter(GameObject go, float delay, string key)
    {
        yield return new WaitForSeconds(delay);
        go.SetActive(false);
        pool[key].Enqueue(go);
    }
}

