using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergeSummaryPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform container;
    [SerializeField] private GameObject summaryItemPrefab;

    [Header("Timing")]
    [SerializeField] private float initialDelay = 0.35f;
    [SerializeField] private float itemSpawnDelay = 0.1f;
    [SerializeField] private float countAnimDuration = 0.4f;

    private readonly List<MergeCounterItem> spawnedItems = new();
    private Coroutine buildRoutine;

    public void Build(List<MergeSessionTracker.MergeCounterSnapshot> snapshot)
    {
        Clear();

        if (buildRoutine != null)
            StopCoroutine(buildRoutine);

        buildRoutine = StartCoroutine(BuildRoutine(snapshot));
    }

    private IEnumerator BuildRoutine(List<MergeSessionTracker.MergeCounterSnapshot> snapshot)
    {
        var tracker = MergeSessionTracker.Instance;
        if (tracker == null) yield break;

        // 🔹 1. Instantiate everything immediately (NO delay)
        foreach (var snap in snapshot)
        {
            Sprite icon = tracker.GetIconForType(snap.type);
            if (icon == null) continue;

            var go = Instantiate(summaryItemPrefab, container);

            if (go.TryGetComponent(out MergeCounterItem item))
            {
                item.Initialize(icon);
                item.SetCount(0); // ensure hidden start state
                spawnedItems.Add(item);

                // store target count on the item via closure
                item.gameObject.SetActive(true);
            }
        }

        // 🔹 2. Wait for popup open animation
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        // 🔹 3. Stagger ONLY the animation triggers
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            var item = spawnedItems[i];

            item.PlayIn();
            item.PrepareCountAnimation(snapshot[i].count, countAnimDuration, this);

            if (itemSpawnDelay > 0f)
                yield return new WaitForSeconds(itemSpawnDelay);
        }

        buildRoutine = null;
    }

    private void Clear()
    {
        if (buildRoutine != null)
        {
            StopCoroutine(buildRoutine);
            buildRoutine = null;
        }

        foreach (var item in spawnedItems)
            Destroy(item.gameObject);

        spawnedItems.Clear();
    }
}
