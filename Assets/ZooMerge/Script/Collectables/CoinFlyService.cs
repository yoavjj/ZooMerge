using System.Collections;
using UnityEngine;

public class CoinFlyService : MonoBehaviour
{
    public static CoinFlyService Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private FlyingCoinCollectible sessionCoinPrefab;
    [SerializeField] private FlyingCoinCollectible cooldownCoinPrefab;

    [Header("Containers")]
    [SerializeField] private RectTransform defaultSpawnContainer; // e.g. your main menu UI container

    [Header("Settings")]
    [SerializeField] private CollectibleFlightSettings coinSettings;

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

    public void FlyCoins(int amount, Source source, RectTransform overrideSpawnContainer = null)
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

        // Convert target screen -> local in spawn container
        Vector2 targetScreen = coinUI.GetFlyTargetScreenPoint();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spawnContainer, targetScreen, uiCam, out Vector2 targetLocal);

        StartCoroutine(FlyRoutine(amount, prefab, spawnContainer, icon, targetLocal, coinUI));
    }

    private IEnumerator FlyRoutine(
        int amount,
        FlyingCoinCollectible prefab,
        RectTransform spawnContainer,
        Sprite icon,
        Vector2 targetLocal,
        TopBarCoinItemUI coinUI)
    {
        var collectible = Instantiate(prefab, spawnContainer);

        collectible.Rect.anchoredPosition = coinSettings.spawnOffset;
        collectible.SetIcon(icon);

        yield return new WaitForSecondsRealtime(coinSettings.holdDuration);

        collectible.LaunchToLocalPoint(
            targetLocalPosition: targetLocal,
            totalDuration: coinSettings.shortFlyDuration,
            onArrive: () =>
            {
                // ✅ commit economy here
                GameInventory.Instance.Add(CurrencyType.Coins, amount);

                // ✅ animate UI
                coinUI.AddCoins(amount);

                // ✅ server snapshot
                CloudSaveManager.SyncEconomyNow();
            },
            delay: 0f,
            arcHeight: coinSettings.arcHeight,
            holdDuration: 0f,
            easeInCurve: coinSettings.easeInCurve,
            easeOutCurve: coinSettings.easeOutCurve
        );
    }
}