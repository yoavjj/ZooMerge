using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/* ---------------------------------------------------
   Supporting Types
--------------------------------------------------- */

[System.Serializable]
public class UICanvasContext
{
    public Canvas canvas;

    public Camera Camera =>
        canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
}

public struct CollectibleFlightData
{
    public Vector2 spawnPosition;
    public Vector2 targetPosition;
    public Sprite icon;
    public int count;
    public float flyDuration;
}

/* ---------------------------------------------------
   CollectibleFlyController
--------------------------------------------------- */

public class CollectibleFlyController : MonoBehaviour
{
    /* ---------------------------
       Inspector References
    --------------------------- */

    [Header("Merge Collectible Prefab")]
    [SerializeField] private FlyingCollectible mergeCollectiblePrefab;

    [SerializeField, Tooltip("UI container for collectibles")]
    private RectTransform mergePrefabContainer;

    [SerializeField, Tooltip("Flight settings for Merge collectibles")]
    private CollectibleFlightSettings MergeSettings;

    [Header("Coin Collectible Prefab")]
    [SerializeField] private FlyingCoinCollectible coinCollectiblePrefab;
    [SerializeField] private FlyingCoinCollectible cooldownCoinCollectiblePrefab;  // cooldown version

    [SerializeField, Tooltip("UI container for collectibles")]
    private RectTransform coinPrefabContainer;
    [SerializeField, Tooltip("Flight settings for Coin collectibles")]
    private CollectibleFlightSettings coinSettings;
    
    [Header("Optional Fly Target Overrides")]
    [SerializeField] private CollectibleFlyTarget heartFlyTarget; // target on your UI (tries/heart icon)
    [SerializeField] private string heartWinLoseEntryId = "Heart_winlose";
    [SerializeField] private int heartWinLoseAmount = 1;

    [Header("UI references")]
    [SerializeField] private LevelProgressBarSlider progressBar;
    [SerializeField] private TopBarMenu topBarMenu;

    [Header("Canvas Context")]
    [SerializeField] private Canvas rootCanvas;

    public enum CoinSpawnSource
    {
        Session,
        Cooldown
    }

    /* ---------------------------
       Runtime
    --------------------------- */

    private Camera uiCam;
    private List<CollectibleFlightData> lastFlightData;

#if UNITY_EDITOR
    /* ---------------------------
       Debug Replay
    --------------------------- */

    [Header("🧪 Debug Replay (Play Mode Only)")]
    [SerializeField] private bool debugReplay;

    private bool debugRunning;

    private void OnValidate()
    {
        if (!Application.isPlaying || !debugReplay || debugRunning)
            return;

        debugRunning = true;
        StartCoroutine(DebugReplayRoutine());
    }
#endif

    /* ---------------------------------------------------
       Unity Lifecycle
    --------------------------------------------------- */

    private void Awake()
    {
        uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera
            : null;
    }

    /* ---------------------------------------------------
       Public API
    --------------------------------------------------- */

    /// <summary>
    /// Main entry for spawning collectibles.
    /// </summary>
    public void SpawnFromPreparedData(
        List<CollectibleFlightData> flightData,
        System.Action<int> onEachArriveWithCount)
    {
        lastFlightData = flightData;
        StartCoroutine(SpawnRoutine(flightData, onEachArriveWithCount));
    }

    /// <summary>
    /// Prepares flight data but does not spawn.
    /// </summary>
    public List<CollectibleFlightData> PrepareFlightData(
        CollectibleSpawnCircle circle,
        int amount,
        Vector2 targetScreenPoint,
        Sprite icon,
        int totalCountToDistribute = 0)
    {
        List<CollectibleFlightData> result = new(amount);

        if (circle == null || mergePrefabContainer == null)
        {
            Debug.LogError("CollectibleFlyController: Missing references.");
            return result;
        }

        // Convert target to local UI space
        Vector2 targetLocal = ScreenToContainerLocal(targetScreenPoint);

        // Convert circle to local UI space
        Vector2 circleCenter = mergePrefabContainer.InverseTransformPoint(circle.transform.position);

        List<Vector2> spawnOffsets = circle.GetFixedSpawnPoints();
        List<int> shuffled = GetShuffledIndices(spawnOffsets.Count);
        List<int> counts = CalculateCountsPerCollectible(totalCountToDistribute, amount);

        // Determine duration based on amount
        float baseFlyDuration = amount <= 3
            ? MergeSettings.shortFlyDuration
            : MergeSettings.longFlyDuration;

        float totalStaggerTime = (amount - 1) * MergeSettings.arrivalStaggerDelay;
        float safeFlyDuration = Mathf.Max(baseFlyDuration + totalStaggerTime, baseFlyDuration);

        // Build flight data list
        for (int i = 0; i < amount; i++)
        {
            int spawnIndex = (i < shuffled.Count) ? shuffled[i] : i % spawnOffsets.Count;
            Vector2 spawnPos = circleCenter + spawnOffsets[spawnIndex];

            float adjustedDuration = safeFlyDuration - ((amount - 1 - i) * MergeSettings.arrivalStaggerDelay);

            result.Add(new CollectibleFlightData
            {
                spawnPosition = spawnPos,
                targetPosition = targetLocal,
                icon = icon,
                count = counts[i],
                flyDuration = adjustedDuration
            });
        }

        return result;
    }

    /* ---------------------------------------------------
       Spawn Routine
    --------------------------------------------------- */

    private IEnumerator SpawnRoutine(
        List<CollectibleFlightData> flightData,
        System.Action<int> arrivalCallback)
    {
        foreach (var data in flightData)
        {
            FlyingCollectible item = Instantiate(mergeCollectiblePrefab, mergePrefabContainer);

            item.Rect.anchoredPosition = data.spawnPosition + MergeSettings.spawnOffset;
            item.SetIcon(data.icon);

            item.LaunchToLocalPoint(
                targetLocalPosition: data.targetPosition,
                totalDuration: data.flyDuration,
                onArrive: () => arrivalCallback?.Invoke(data.count),
                delay: 0f,
                MergeSettings.arcHeight,
                MergeSettings.holdDuration,
                MergeSettings.easeInCurve,
                MergeSettings.easeOutCurve
            );

            // Small random delay before next spawn
            yield return new WaitForSecondsRealtime(Random.Range(0.015f, 0.05f));
        }
    }

    public void SpawnPendingEnemyCoinsToTopBar() // session enemy coins
    {
        int totalCoins = MergeLevelManager.ConsumePendingEnemyCoins();
        SpawnCoinsToTopBar(totalCoins, null, CoinSpawnSource.Session);
    }

    public void SpawnCoinsToTopBar(int amount, RectTransform overrideSpawnContainer = null, CoinSpawnSource source = CoinSpawnSource.Session)
    {
        if (amount <= 0) return;

        RectTransform spawnContainer = overrideSpawnContainer != null ? overrideSpawnContainer : coinPrefabContainer;

        FlyingCoinCollectible prefabToUse =
            (source == CoinSpawnSource.Cooldown && cooldownCoinCollectiblePrefab != null)
                ? cooldownCoinCollectiblePrefab
                : coinCollectiblePrefab;

        if (prefabToUse == null || spawnContainer == null)
        {
            Debug.LogWarning("⚠️ Coin prefab or container not set.");
            return;
        }

        if (topBarMenu == null || !topBarMenu.TryGetOrCreateCoinItem(out TopBarCoinItemUI coinUI))
        {
            Debug.LogWarning("⚠️ Could not find TopBarCoinItemUI to fly coins to.");
            return;
        }

        Sprite coinIcon = coinUI.GetIcon();
        if (coinIcon == null)
        {
            Debug.LogWarning("⚠️ Coin icon not found on TopBarCoinItemUI.");
            return;
        }

        Vector2 targetScreenPoint = coinUI.GetFlyTargetScreenPoint();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spawnContainer,
            targetScreenPoint,
            uiCam,
            out Vector2 targetLocalPoint
        );

        StartCoroutine(
            SpawnCoinRoutine(
                amount,
                coinIcon,
                targetLocalPoint,
                coinSettings.holdDuration,
                coinSettings.shortFlyDuration,
                coinUI,
                spawnContainer,
                prefabToUse
            )
        );
    }

    private IEnumerator SpawnCoinRoutine(
        int totalCoins,
        Sprite icon,
        Vector2 targetLocal,
        float holdDuration,
        float flyDuration,
        TopBarCoinItemUI coinUI,
        RectTransform spawnContainer,
        FlyingCoinCollectible prefabToUse)
    {
        var collectible = Instantiate(prefabToUse, spawnContainer);

        collectible.Rect.anchoredPosition = coinSettings.spawnOffset;
        collectible.SetIcon(icon);

        yield return new WaitForSecondsRealtime(holdDuration);

        collectible.LaunchToLocalPoint(
            targetLocalPosition: targetLocal,
            totalDuration: flyDuration,
            onArrive: () =>
            {
                GameInventory.Instance.Add(CurrencyType.Coins, totalCoins); // delta
                coinUI.AddCoins(totalCoins);                               // visual
            },
            delay: 0f,
            arcHeight: coinSettings.arcHeight,
            holdDuration: 0f,
            easeInCurve: coinSettings.easeInCurve,
            easeOutCurve: coinSettings.easeOutCurve
        );
    }

    /* ---------------------------------------------------
       Helpers
    --------------------------------------------------- */

    private Vector2 ScreenToContainerLocal(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mergePrefabContainer, screenPoint, uiCam, out Vector2 local);

        return local;
    }

    private List<int> GetShuffledIndices(int count)
    {
        List<int> list = new(count);
        for (int i = 0; i < count; i++) list.Add(i);

        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    private List<int> CalculateCountsPerCollectible(int total, int amount)
    {
        List<int> result = new(amount);

        if (total <= amount || total <= 0)
        {
            for (int i = 0; i < amount; i++)
                result.Add(1);

            return result;
        }

        int baseAmount = total / amount;
        int remainder = total % amount;

        for (int i = 0; i < amount; i++)
        {
            int value = baseAmount;
            if (i == amount - 1) value += remainder;

            result.Add(value);
        }

        return result;
    }

    public void PositionCoinContainerToIndex(int index)
    {
        RectTransform target = progressBar.GetEnemyIconRect(index);
        if (target == null)
            return;

        PositionCoinContainerToIcon(target); // your existing helper
    }

    public void PositionCoinContainerToIcon(RectTransform target)
    {
        if (target == null || coinPrefabContainer == null)
            return;

        // Copy anchors/pivot
        coinPrefabContainer.anchorMin = target.anchorMin;
        coinPrefabContainer.anchorMax = target.anchorMax;
        coinPrefabContainer.pivot = target.pivot;

        // Copy size
        coinPrefabContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, target.rect.width);
        coinPrefabContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, target.rect.height);

        // Copy position
        coinPrefabContainer.anchoredPosition = target.anchoredPosition;
    }

    public void FlyHeartWinLose()
    {
        if (CollectibleFlyService.Instance == null)
        {
            Debug.LogWarning("[CollectibleFlyController] CollectibleFlyService.Instance is null.");
            return;
        }

        if (heartFlyTarget == null)
        {
            Debug.LogWarning("[CollectibleFlyController] heartFlyTarget not assigned.");
            return;
        }

        // Use default spawn container (pass null)
        CollectibleFlyService.Instance.Fly(heartWinLoseEntryId, heartWinLoseAmount, heartFlyTarget, null);
    }


#if UNITY_EDITOR
    /* ---------------------------------------------------
       Debug Replay
    --------------------------------------------------- */

    private IEnumerator DebugReplayRoutine()
    {
        if (lastFlightData == null || lastFlightData.Count == 0)
        {
            Debug.LogWarning("⚠️ Cannot replay collectibles: No previous flight data.");
            ResetDebugToggle();
            yield break;
        }

        Debug.Log($"🧪 Replaying last collectible flight with {lastFlightData.Count} items...");

        SpawnFromPreparedData(
            lastFlightData,
            count => Debug.Log($"🧪 Debug collectible arrived with count {count}")
        );

        yield return new WaitForSecondsRealtime(MergeSettings.longFlyDuration + MergeSettings.holdDuration + 0.5f);

        ResetDebugToggle();
    }

    private void ResetDebugToggle()
    {
        debugReplay = false;
        debugRunning = false;
        EditorUtility.SetDirty(this);
    }
#endif
}