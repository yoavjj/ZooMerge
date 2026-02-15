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
    [SerializeField] private float itemSpawnDelay = 0.1f;
    [SerializeField] private float countAnimDuration = 0.4f;

    [Header("Collectible Fly")]
    [SerializeField] private CollectibleFlyController flyController;

    private readonly List<MergeCounterItem> spawnedItems = new();
    private Coroutine buildRoutine;

    private int totalCollectiblesPending = 0;
    private System.Action onAllCollectiblesFinished;

    private bool waitingForOpenSignal = false;
    private List<MergeSessionTracker.MergeCounterSnapshot> pendingSnapshot;

    private bool isSummaryBusy = false;
    public bool IsBusy => isSummaryBusy;
    private int countersAnimating = 0;

    public void Build(List<MergeSessionTracker.MergeCounterSnapshot> snapshot)
    {
        Clear();

        // // 🆕 1. Prepare types so TopBar has placeholders immediately
        // List<BallType> typesToShow = new();
        // foreach (var snap in snapshot)
        // {
        //     if (!typesToShow.Contains(snap.type))
        //         typesToShow.Add(snap.type);
        // }

        // topBarMenu.PrepareTypes(typesToShow);

        isSummaryBusy = true; // 🔒 BLOCK play button

        // ✅ Show ALL configured types, not just the ones in the snapshot
        List<BallType> allTypes = MergeSessionTracker.Instance
            .GetTypeConfigs()
            .ConvertAll(config => config.type);

        topBarMenu.PrepareTypes(allTypes);

        // (Optional) If you want to also show current values instantly:
        // topBarMenu.Build(); // ← Only if you want to rebuild from inventory snapshot

        if (buildRoutine != null)
            StopCoroutine(buildRoutine);

        pendingSnapshot = snapshot;
        buildRoutine = StartCoroutine(AnimateItemsRoutine_Part1(snapshot));

        topBarMenu.BuildCoinUI();
    }

    private IEnumerator AnimateItemsRoutine_Part1(
    List<MergeSessionTracker.MergeCounterSnapshot> snapshot)
    {
        var tracker = MergeSessionTracker.Instance;
        if (tracker == null) yield break;

        spawnedItems.Clear();

        // 🔹 1. Instantiate everything immediately
        foreach (var snap in snapshot)
        {
            Sprite icon = tracker.GetIconForType(snap.type);
            if (icon == null) continue;

            var go = Instantiate(summaryItemPrefab, container);

            if (go.TryGetComponent(out MergeCounterItem item))
            {
                item.Initialize(icon);
                item.SetCount(0);
                item.SetType(snap.type);
                item.gameObject.SetActive(true);
                spawnedItems.Add(item);
            }
        }

        // ⏸️ WAIT until animation event tells us to continue
        waitingForOpenSignal = true;
        while (waitingForOpenSignal)
            yield return null;

        // Continue to phase 2
        StartCoroutine(AnimateItemsRoutine_Part2(snapshot));
    }

    private IEnumerator AnimateItemsRoutine_Part2(
        List<MergeSessionTracker.MergeCounterSnapshot> snapshot)
    {
        // ✅ If there are no items to animate, we're done immediately
        if (spawnedItems.Count == 0)
        {
            isSummaryBusy = false;
            onAllCollectiblesFinished?.Invoke();
            buildRoutine = null;
            yield break;
        }

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


    // 🎬 CALLED BY POPUP OPEN ANIMATION EVENT
    public void OnPopupOpenAnimationFinished()
    {
        waitingForOpenSignal = false;
    }

    private void HandleSummaryCountFinished(MergeCounterItem item)
    {
        // 🔒 Prevent duplicate spawning
        if (item.HasTriggeredCollectibles)
            return;


        item.MarkCollectiblesTriggered();

        countersAnimating--;

        // ✅ Make sure the TopBar item exists
        if (!topBarMenu.TryGetOrCreateItem(item.Type, out TopBarMergeItemUI itemUI))
            return;

        Vector2 targetScreen = itemUI.GetFlyTargetScreenPoint();

        int totalCount = item.TargetCount;
        int minToAnimate = item.MinCountToAnimate;

        // 🔑 Decide how many collectibles to spawn
        int collectibleCount = totalCount > minToAnimate
            ? minToAnimate
            : totalCount;

        // 🔑 Only distribute if total is larger than spawned amount
        int valueToDistribute = totalCount > collectibleCount
            ? totalCount
            : 0;

        var flightData = flyController.PrepareFlightData(
            circle: item.SpawnCircle,
            amount: collectibleCount,
            targetScreenPoint: targetScreen,
            icon: item.GetIcon(),
            totalCountToDistribute: valueToDistribute
        );

        // 👇 Track how many collectibles are about to fly
        totalCollectiblesPending += collectibleCount;

        flyController.SpawnFromPreparedData(
            flightData,
            onEachArriveWithCount: (count) =>
            {
                GameInventory.Instance.Add(item.Type, count);
                topBarMenu.RefreshValue(item.Type);

                totalCollectiblesPending--;

                if (totalCollectiblesPending <= 0 && countersAnimating <= 0)
                {
                    isSummaryBusy = false; // ✅ SAFE TO CONTINUE
                    onAllCollectiblesFinished?.Invoke();
                }
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
