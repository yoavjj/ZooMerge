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
    [SerializeField] private RectTransform parentLayout;

    [Header("Canvas Context")]
    [SerializeField] private Canvas rootCanvas;

    [Header("Purchase Reduction Animation")]
    [SerializeField, Min(0f)]
    private float reductionStartDelay = 0.35f;

    [SerializeField, Min(0.01f)]
    private float reductionDuration = 0.8f;

    private readonly Dictionary<BallType, TopBarMergeItemUI> itemsByType = new();
    private Camera uiCam;

    private void Awake()
    {
        uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera
            : null;
    }

    private void OnEnable()
    {
        GameInventory.Instance.OnBallReduced +=
            HandleBallReduced;

        GameInventory.Instance.OnCurrencyReduced +=
            HandleCurrencyReduced;

        if (BallUnlockManager.Instance != null)
        {
            BallUnlockManager.Instance.OnBallUnlocked +=
                HandleBallUnlocked;
        }
    }

    private void OnDisable()
    {
        GameInventory.Instance.OnBallReduced -=
            HandleBallReduced;

        GameInventory.Instance.OnCurrencyReduced -=
            HandleCurrencyReduced;

        if (BallUnlockManager.Instance != null)
        {
            BallUnlockManager.Instance.OnBallUnlocked -=
                HandleBallUnlocked;
        }
    }

    private void HandleBallReduced(
    BallType type,
    int previousValue,
    int newValue)
    {
        if (!itemsByType.TryGetValue(
                type,
                out TopBarMergeItemUI item))
        {
            return;
        }

        if (item == null)
            return;

        item.AnimateCountReduction(
            previousValue,
            newValue,
            reductionStartDelay,
            reductionDuration
        );
    }

    private void HandleCurrencyReduced(
        CurrencyType type,
        int previousValue,
        int newValue)
    {
        if (type != CurrencyType.Coins)
            return;

        if (!TryGetOrCreateCoinItem(
                out TopBarCoinItemUI coinItem))
        {
            return;
        }

        coinItem.AnimateCountReduction(
            previousValue,
            newValue,
            reductionStartDelay,
            reductionDuration
        );
    }

    private void HandleBallUnlocked(BallType type)
    {
        TryGetOrCreateItem(type, out _);
    }

    public void RefreshCoins()
    {
        if (TryGetOrCreateCoinItem(out TopBarCoinItemUI coinUI))
        {
            int coins = GameInventory.Instance.Get(CurrencyType.Coins);
            coinUI.SetCountImmediate(coins); // ✅ no animation/events
        }
    }

    public void BuildAllBallTypesUI()
    {
        if (MergeSessionTracker.Instance == null)
            return;

        List<BallType> allTypes =
            MergeSessionTracker.Instance.GetConfiguredTypes();

        foreach (BallType type in allTypes)
        {
            if (!IsBallUnlocked(type))
                continue;

            TryGetOrCreateItem(type, out _);
        }

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

        if (parentLayout != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                parentLayout
            );
        }

        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// Called whenever inventory changes (e.g. flying collectible arrives)
    /// </summary>
    public void RefreshValue(BallType type)
    {
        if (!IsBallUnlocked(type))
        {
            RemoveItem(type);
            return;
        }

        int value =
            GameInventory.Instance.Get(type);

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

    private void RemoveItem(BallType type)
    {
        if (!itemsByType.TryGetValue(
                type,
                out TopBarMergeItemUI item))
        {
            return;
        }

        itemsByType.Remove(type);

        if (item != null)
            Destroy(item.gameObject);

        RebuildLayoutImmediate();
    }

    private void CreateItem(BallType type, int value)
    {
        if (!IsBallUnlocked(type))
            return;

        Sprite icon =
            MergeSessionTracker.Instance.GetIconForType(type);

        if (icon == null)
            return;

        GameObject go =
            Instantiate(topBarItemPrefab, container);

        if (!go.TryGetComponent(
                out TopBarMergeItemUI item))
        {
            Destroy(go);
            return;
        }

        item.InjectUICamera(uiCam);
        item.Initialize(type, icon, value);

        itemsByType[type] = item;
    }

    private bool IsBallUnlocked(BallType type)
    {
        BallUnlockManager unlockManager =
            BallUnlockManager.Instance;

        if (unlockManager == null)
        {
            Debug.LogWarning(
                "[TopBarMenu] BallUnlockManager.Instance is null."
            );

            return false;
        }

        return unlockManager.IsUnlocked(type);
    }

    private void Clear()
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);

        itemsByType.Clear();
    }

    public bool TryGetItem(BallType type, out TopBarMergeItemUI item)
        => itemsByType.TryGetValue(type, out item);

    public bool TryGetOrCreateItem(
        BallType type,
        out TopBarMergeItemUI item)
    {
        if (itemsByType.TryGetValue(type, out item))
            return true;

        if (!IsBallUnlocked(type))
        {
            item = null;
            return false;
        }

        int value =
            GameInventory.Instance.Get(type);

        Sprite icon =
            MergeSessionTracker.Instance.GetIconForType(type);

        if (icon == null)
        {
            item = null;
            return false;
        }

        GameObject go =
            Instantiate(topBarItemPrefab, container);

        if (!go.TryGetComponent(out item))
        {
            Destroy(go);
            item = null;
            return false;
        }

        item.InjectUICamera(uiCam);
        item.Initialize(type, icon, value);

        itemsByType[type] = item;

        Canvas.ForceUpdateCanvases();
        RebuildLayoutImmediate();

        return true;
    }

    public void PrepareTypes(
        List<BallType> upcomingTypes)
    {
        foreach (BallType type in upcomingTypes)
        {
            if (!IsBallUnlocked(type))
                continue;

            if (!itemsByType.ContainsKey(type))
                TryGetOrCreateItem(type, out _);
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
