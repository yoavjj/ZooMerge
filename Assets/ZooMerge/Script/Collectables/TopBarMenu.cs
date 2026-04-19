using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopBarMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform container;
    [SerializeField] private GameObject topBarItemPrefab;

    [Header("Canvas Context")]
    [SerializeField] private Canvas rootCanvas;

    private readonly Dictionary<BallType, TopBarMergeItemUI> itemsByType = new();
    private Camera uiCam;

    private void Awake()
    {
        uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera
            : null;
    }

    public void BuildAllBallTypesUI()
    {
        if (MergeSessionTracker.Instance == null) return;

        // Get all configured types from your tracker config
        List<BallType> allTypes = MergeSessionTracker.Instance
            .GetTypeConfigs()
            .ConvertAll(c => c.type);

        PrepareTypes(allTypes);   // creates items even if count is 0
        RebuildLayoutImmediate();
    }

    /// <summary>
    /// Build from inventory snapshot (used on popup open / resume)
    /// </summary>
    public void BuildCoinUI()
    {
        if (TryGetOrCreateCoinItem(out TopBarCoinItemUI coinUI))
        {
            coinUI.InjectUICamera(uiCam);

            int coinsFromInventory = GameInventory.Instance.Get(CurrencyType.Coins);
            coinUI.Initialize(coinUI.GetIcon(), coinsFromInventory);
        }

        RebuildLayoutImmediate();
    }

    private void RebuildLayoutImmediate()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            container as RectTransform
        );
    }

    /// <summary>
    /// Called whenever inventory changes (e.g. flying collectible arrives)
    /// </summary>
    public void RefreshValue(BallType type)
    {
        int value = GameInventory.Instance.Get(type);

        // 🔥 First time this currency appears
        if (!itemsByType.TryGetValue(type, out var item))
        {
            if (value <= 0)
                return;

            CreateItem(type, value);
            return;
        }

        // Normal update
        item.SetCount(value);
    }

    private void CreateItem(BallType type, int value)
    {
        var config = MergeSessionTracker.Instance
            .GetTypeConfigs()
            .Find(c => c.type == type);

        if (config == null)
            return;

        var go = Instantiate(topBarItemPrefab, container);

        if (!go.TryGetComponent(out TopBarMergeItemUI item))
        {
            Destroy(go);
            return;
        }

        item.InjectUICamera(uiCam);
        item.Initialize(type, config.icon, value);

        itemsByType[type] = item;
    }

    private void Clear()
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);

        itemsByType.Clear();
    }

    public bool TryGetItem(BallType type, out TopBarMergeItemUI item)
        => itemsByType.TryGetValue(type, out item);

    public bool TryGetOrCreateItem(BallType type, out TopBarMergeItemUI item)
    {
        if (itemsByType.TryGetValue(type, out item))
            return true;

        // Create even if count is 0, because we need a fly target.
        int value = GameInventory.Instance.Get(type);

        var config = MergeSessionTracker.Instance
            .GetTypeConfigs()
            .Find(c => c.type == type);

        if (config == null)
            return false;

        var go = Instantiate(topBarItemPrefab, container);

        if (!go.TryGetComponent(out item))
        {
            Destroy(go);
            return false;
        }

        item.InjectUICamera(uiCam);
        item.Initialize(type, config.icon, value);

        itemsByType[type] = item;

        // Layout must update so the target position is correct
        RebuildLayoutImmediate();

        return true;
    }

    public void PrepareTypes(List<BallType> upcomingTypes)
    {
        foreach (var type in upcomingTypes)
        {
            if (!itemsByType.ContainsKey(type))
            {
                TryGetOrCreateItem(type, out _); // Will show item with current count (even if 0)
            }
        }
    }

    public bool TryGetOrCreateCoinItem(out TopBarCoinItemUI item)
    {
        item = GetComponentInChildren<TopBarCoinItemUI>();
        if (item != null) return true;

        // Optionally create it from prefab if you want
        return false;
    }

    public Sprite GetCoinIcon()
    {
        if (TryGetOrCreateCoinItem(out var coinItem))
        {
            return coinItem.GetIcon();
        }

        return null;
    }
}
