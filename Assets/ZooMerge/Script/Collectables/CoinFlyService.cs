using System.Collections;
using UnityEngine;

public class CoinFlyService : SfxBehaviourTirgger
{
    public static CoinFlyService Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private FlyingCoinCollectible sessionCoinPrefab;
    [SerializeField] private FlyingCoinCollectible cooldownCoinPrefab;

    [Header("Containers")]
    [SerializeField] private RectTransform defaultSpawnContainer; // e.g. your main menu UI container

    [Header("Settings")]
    [SerializeField] private CollectibleFlightSettings coinSettings;

    [SerializeField] private float mainMenuArcHeight = 300f;

    [Header("Canvas")]
    [SerializeField] private Canvas rootCanvas;

    [Header("Refs")]
    [SerializeField] private TopBarMenu topBarMenu;

    private Camera uiCam;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera
            : null;
    }

    public enum Source { Session, Cooldown }

    public enum FlightPath
    {
        Direct,
        MainMenuCurve
    }

    public void FlyCoins(int amount, Source source, RectTransform overrideSpawnContainer = null, bool useDefaultArc = true)
    {
        if (amount <= 0) return;

        if (topBarMenu == null || !topBarMenu.TryGetOrCreateCoinItem(out TopBarCoinItemUI coinUI))
        {
            Debug.LogWarning("[CoinFlyService] No TopBarCoinItemUI found.");
            return;
        }

        var prefab = (source == Source.Cooldown && cooldownCoinPrefab != null)
            ? cooldownCoinPrefab
            : sessionCoinPrefab;

        var spawnContainer = overrideSpawnContainer != null ? overrideSpawnContainer : defaultSpawnContainer;

        if (prefab == null || spawnContainer == null)
        {
            Debug.LogWarning("[CoinFlyService] Missing prefab or spawn container.");
            return;
        }

        Sprite icon = coinUI.GetIcon();
        if (icon == null)
        {
            Debug.LogWarning("[CoinFlyService] Coin icon missing.");
            return;
        }

        PlayUiSfx(SfxCue.Cooldown_Collect);

        // Convert target screen -> local in spawn container
        Vector2 targetScreen = coinUI.GetFlyTargetScreenPoint();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spawnContainer, targetScreen, uiCam, out Vector2 targetLocal);

        StartCoroutine(FlyRoutine(amount, prefab, spawnContainer, icon, targetLocal, coinUI, useDefaultArc));
    }

    private IEnumerator FlyRoutine(
        int amount,
        FlyingCoinCollectible prefab,
        RectTransform spawnContainer,
        Sprite icon,
        Vector2 targetLocal,
        TopBarCoinItemUI coinUI,
        bool useDefaultArc)
    {
        var collectible = Instantiate(prefab, spawnContainer);

        collectible.Rect.anchoredPosition = coinSettings.spawnOffset;
        collectible.SetIcon(icon);

        yield return new WaitForSecondsRealtime(coinSettings.holdDuration);

        float arcHeight = useDefaultArc
            ? coinSettings.arcHeight
            : mainMenuArcHeight;

        collectible.LaunchToLocalPoint(
            targetLocalPosition: targetLocal,
            totalDuration: coinSettings.shortFlyDuration,
            onArrive: () =>
            {
                GameInventory.Instance.Add(CurrencyType.Coins, amount);

                coinUI.AddCoins(amount);

                CloudSaveManager.SyncEconomyNow();
            },
            delay: 0f,
            arcHeight: arcHeight,
            holdDuration: 0f,
            easeInCurve: coinSettings.easeInCurve,
            easeOutCurve: coinSettings.easeOutCurve
        );
    }
}