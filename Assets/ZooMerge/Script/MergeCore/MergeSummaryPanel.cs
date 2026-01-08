using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergeSummaryPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform container;
    [SerializeField] private GameObject summaryItemPrefab;
    [SerializeField] private TopBarMenu topBarMenu;

    [Header("Timing")]
    [SerializeField] private float initialDelay = 0.35f;
    [SerializeField] private float itemSpawnDelay = 0.1f;
    [SerializeField] private float countAnimDuration = 0.4f;

    [Header("Collectible Fly")]
    [SerializeField] private CollectibleFlyController flyController;

    private readonly List<MergeCounterItem> spawnedItems = new();
    private Coroutine buildRoutine;

    public void Build(List<MergeSessionTracker.MergeCounterSnapshot> snapshot)
    {
        Clear();

        // 🆕 1. Prepare types so TopBar has placeholders immediately
        List<BallType> typesToShow = new();
        foreach (var snap in snapshot)
        {
            if (!typesToShow.Contains(snap.type))
                typesToShow.Add(snap.type);
        }

        topBarMenu.PrepareTypes(typesToShow);

        // (Optional) If you want to also show current values instantly:
        // topBarMenu.Build(); // ← Only if you want to rebuild from inventory snapshot

        if (buildRoutine != null)
            StopCoroutine(buildRoutine);

        buildRoutine = StartCoroutine(AnimateItemsRoutine(snapshot));
    }

    private IEnumerator AnimateItemsRoutine(List<MergeSessionTracker.MergeCounterSnapshot> snapshot)
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
                item.SetType(snap.type);
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
            item.OnCountAnimationFinished = HandleSummaryCountFinished;

            if (itemSpawnDelay > 0f)
                yield return new WaitForSeconds(itemSpawnDelay);
        }

        buildRoutine = null;
    }

    private void HandleSummaryCountFinished(MergeCounterItem item)
    {
        // ✅ Make sure the TopBar item exists (even if inventory is 0)
        if (!topBarMenu.TryGetOrCreateItem(item.Type, out TopBarItemUI itemUI))
            return;

        Vector2 targetScreen = itemUI.GetFlyTargetScreenPoint();

        flyController.Spawn(
            amount: item.TargetCount,
            startUI: item.transform as RectTransform,
            targetScreenPoint: targetScreen,
            onEachArrive: () =>
            {
                // ✅ Inventory is truth
                GameInventory.Instance.Add(item.Type, 1);

                // ✅ UI reflects truth (and creates item if ever missing)
                topBarMenu.RefreshValue(item.Type);
            }
        );
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
