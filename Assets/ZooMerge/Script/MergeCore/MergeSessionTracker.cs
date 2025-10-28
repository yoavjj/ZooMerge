using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MergeSessionTracker : MonoBehaviour
{
    [System.Serializable]
    public class MergeCounterUI
    {
        public BallType type;
        public Sprite icon;
    }

    [Header("Setup")]
    [SerializeField] private GameObject counterPrefab; // Prefab with Image + TMP
    [SerializeField] private Transform counterContainer; // Where to instantiate the counters
    [SerializeField] private List<MergeCounterUI> typeConfigs;

    private Dictionary<BallType, MergeCounterItem> counters = new();

    private void OnEnable()
    {
        BallEventManager.OnBallMerged += HandleBallMerged;
        BallEventManager.OnResetCounters += ResetCounters;
    }

    private void OnDisable()
    {
        BallEventManager.OnBallMerged -= HandleBallMerged;
        BallEventManager.OnResetCounters -= ResetCounters;
    }


    private void HandleBallMerged(BallInfo merged)
    {
        BallType type = merged.Type;

        if (!counters.ContainsKey(type))
        {
            var config = typeConfigs.Find(c => c.type == type);
            if (config == null || counterPrefab == null || counterContainer == null)
            {
                Debug.LogWarning($"⚠️ Missing config for {type}");
                return;
            }

            var instance = Instantiate(counterPrefab, counterContainer);
            var counterItem = instance.GetComponent<MergeCounterItem>();
            if (counterItem != null)
            {
                counterItem.Initialize(config.icon);
                counters[type] = counterItem;
            }
        }
        else
        {
            counters[type]?.Increment();
        }
    }

    public void ResetCounters()
    {
        foreach (var item in counters.Values)
            Destroy(item.gameObject);

        counters.Clear();
    }
}
