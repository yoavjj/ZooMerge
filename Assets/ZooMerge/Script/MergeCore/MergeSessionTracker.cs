using System.Collections.Generic;
using UnityEngine;

public class MergeSessionTracker : MonoBehaviour
{
    public static MergeSessionTracker Instance { get; private set; }

    [System.Serializable]
    public struct MergeCounterSnapshot
    {
        public BallType type;
        public int count;
    }

    [Header("Setup")]
    [SerializeField] private BallSet ballSet;
    [SerializeField] private GameObject counterPrefab;
    [SerializeField] private Transform counterContainer;

    private readonly Dictionary<BallType, MergeCounterItem> counters =
        new();

    private readonly Dictionary<BallType, int> savedCounters =
        new();

    private bool isRestoring;

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
        if (isRestoring || merged == null)
            return;

        BallType type = merged.Type;

        if (!counters.ContainsKey(type))
        {
            Sprite icon = GetIconForType(type);

            if (icon == null)
            {
                Debug.LogWarning(
                    $"[MergeSessionTracker] Missing merge icon for {type}."
                );

                return;
            }

            MergeCounterItem counterItem =
                CreateCounterItem(type, icon);

            if (counterItem != null)
                counters[type] = counterItem;
        }

        counters[type]?.Increment();

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

        return list;
    }

    public void RestoreCounterState(
        List<MergeCounterSnapshot> snapshots)
    {
        isRestoring = true;

        ResetCounters(false);
        savedCounters.Clear();

        if (snapshots != null)
        {
            foreach (var snap in snapshots)
            {
                Sprite icon = GetIconForType(snap.type);

                if (icon == null)
                {
                    Debug.LogWarning(
                        $"[MergeSessionTracker] Missing merge icon for " +
                        $"{snap.type}, skipping restore."
                    );

                    continue;
                }

                MergeCounterItem item =
                    CreateCounterItem(snap.type, icon);

                if (item == null)
                    continue;

                item.SetCount(snap.count);
                counters[snap.type] = item;
                savedCounters[snap.type] = snap.count;
            }
        }

        isRestoring = false;
    }

    public List<MergeCounterSnapshot> GetCurrentSnapshot()
    {
        return SaveCounterState();
    }

    public Sprite GetIconForType(BallType type)
    {
        return ballSet != null
            ? ballSet.GetMergeIcon(type)
            : null;
    }

    public List<BallType> GetConfiguredTypes()
    {
        var result = new List<BallType>();

        if (ballSet == null ||
            ballSet.ballTypeUIData == null)
        {
            return result;
        }

        foreach (BallSet.BallTypeUIData data
                 in ballSet.ballTypeUIData)
        {
            if (data == null)
                continue;

            if (!result.Contains(data.type))
                result.Add(data.type);
        }

        return result;
    }

    private void HandleResetCounters(bool keepUI = false)
    {
        ResetCounters(keepUI);
    }

    public void ResetCounters(bool keepUI)
    {
        if (!keepUI)
        {
            foreach (MergeCounterItem item in counters.Values)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }

            counters.Clear();
        }
        else
        {
            foreach (MergeCounterItem item in counters.Values)
            {
                if (item != null)
                    item.SetCount(0);
            }
        }

        savedCounters.Clear();
    }

    private MergeCounterItem CreateCounterItem(
        BallType type,
        Sprite icon)
    {
        if (counterPrefab == null ||
            counterContainer == null)
        {
            Debug.LogWarning(
                "[MergeSessionTracker] Counter prefab or container is missing."
            );

            return null;
        }

        GameObject instance =
            Instantiate(counterPrefab, counterContainer);

        if (instance.TryGetComponent(
                out MergeCounterItem counterItem))
        {
            counterItem.Initialize(icon);
            return counterItem;
        }

        Debug.LogError(
            $"[MergeSessionTracker] Created counter for {type}, " +
            $"but it lacks MergeCounterItem."
        );

        Destroy(instance);
        return null;
    }

    public int GetCurrentCount(BallType type)
    {
        return savedCounters.TryGetValue(
            type,
            out int value
        )
            ? value
            : 0;
    }
}