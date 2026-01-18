using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MergeSessionTracker : MonoBehaviour
{
    public static MergeSessionTracker Instance { get; private set; }

    [System.Serializable]
    public class MergeCounterUI
    {
        public BallType type;
        public Sprite icon;
    }

    [System.Serializable]
    public struct MergeCounterSnapshot
    {
        public BallType type;
        public int count;
    }


    [Header("Setup")]
    [SerializeField] private GameObject counterPrefab; // Prefab with Image + TMP
    [SerializeField] private Transform counterContainer; // Where to instantiate the counters
    [SerializeField] private List<MergeCounterUI> typeConfigs;

    private Dictionary<BallType, MergeCounterItem> counters = new();
    private Dictionary<BallType, int> savedCounters = new();

    private bool isRestoring = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        BallEventManager.OnBallMerged += HandleBallMerged;
        BallEventManager.OnResetCounters += HandleResetCounters;
    }

    private void OnDisable()
    {
        BallEventManager.OnBallMerged -= HandleBallMerged;
        BallEventManager.OnResetCounters -= HandleResetCounters;
    }

    private void HandleBallMerged(BallInfo merged)
    {
        // ⛔ Ignore merges that occur during state restoration
        if (isRestoring) return;

        BallType type = merged.Type;

        if (!counters.ContainsKey(type))
        {
            var config = typeConfigs.Find(c => c.type == type);
            if (config == null)
            {
                Debug.LogWarning($"⚠️ Missing config for {type}");
                return;
            }

            var counterItem = CreateCounterItem(type, config.icon);
            if (counterItem != null)
            {
                counters[type] = counterItem;
            }
        }

        counters[type]?.Increment();

        // Track it in the internal counter map
        if (!savedCounters.ContainsKey(type))
            savedCounters[type] = 0;

        if (!savedCounters.ContainsKey(type))
            savedCounters[type] = 0;

        savedCounters[type]++;
    }

    public List<MergeCounterSnapshot> SaveCounterState()
    {
        var list = new List<MergeCounterSnapshot>();
        foreach (var pair in savedCounters)
        {
            list.Add(new MergeCounterSnapshot
            {
                type = pair.Key,
                count = pair.Value
            });
        }

        Debug.Log($"💾 Saved {list.Count} counter(s): " +
                  string.Join(", ", list.ConvertAll(s => $"{s.type}={s.count}")));

        return list;
    }

    public void RestoreCounterState(List<MergeCounterSnapshot> snapshots)
    {
        isRestoring = true;     // ⛔ stop listening to merge events

        ResetCounters(false);
        savedCounters.Clear();

        foreach (var snap in snapshots)
        {
            var config = typeConfigs.Find(c => c.type == snap.type);
            if (config == null)
            {
                Debug.LogWarning($"⚠️ Missing config for {snap.type}, skipping restore.");
                continue;
            }

            var item = CreateCounterItem(snap.type, config.icon);
            if (item != null)
            {
                item.SetCount(snap.count);
                counters[snap.type] = item;
                savedCounters[snap.type] = snap.count;
            }
        }

        isRestoring = false;    // ✅ restore normal behavior

        Debug.Log($"♻️ Restored {snapshots.Count} counter(s): " +
                  string.Join(", ", snapshots.ConvertAll(s => $"{s.type}={s.count}")));
    }

    public List<MergeCounterSnapshot> GetCurrentSnapshot()
    {
        return SaveCounterState();
    }

    public Sprite GetIconForType(BallType type)
    {
        var config = typeConfigs.Find(c => c.type == type);
        return config != null ? config.icon : null;
    }

    private void HandleResetCounters(bool keepUI = false)
    {
        ResetCounters(keepUI);
    }

    public void ResetCounters(bool keepUI)
    {
        if (!keepUI)
        {
            foreach (var item in counters.Values)
                Destroy(item.gameObject);

            counters.Clear();
        }
        else
        {
            // 🔄 Keep UI, reset values only
            foreach (var item in counters.Values)
                item.SetCount(0);
        }

        savedCounters.Clear();
    }

    private MergeCounterItem CreateCounterItem(BallType type, Sprite icon)
    {
        if (counterPrefab == null || counterContainer == null)
        {
            Debug.LogWarning("Counter prefab or container is missing.");
            return null;
        }

        var instance = Instantiate(counterPrefab, counterContainer);

        if (instance.TryGetComponent(out MergeCounterItem counterItem))
        {
            counterItem.Initialize(icon);
            return counterItem;
        }
        else
        {
            Debug.LogError($"❌ Created counter for {type} but it lacks MergeCounterItem component.");
            Destroy(instance);
            return null;
        }
    }

    public List<MergeCounterUI> GetTypeConfigs()
    {
        return typeConfigs;
    }

    public int GetCurrentCount(BallType type)
    {
        return savedCounters.TryGetValue(type, out var value) ? value : 0;
    }
}
