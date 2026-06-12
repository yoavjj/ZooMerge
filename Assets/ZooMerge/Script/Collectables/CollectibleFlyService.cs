using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CollectibleFlyService : MonoBehaviour
{
    public static CollectibleFlyService Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private CollectibleFlyDatabaseSO database;

    [Header("Containers")]
    [SerializeField] private RectTransform defaultSpawnContainer;

    [Header("Canvas")]
    [SerializeField] private Canvas rootCanvas;

    private Camera uiCam;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera
            : null;
    }

    private Camera GetCameraForSpawnContainer(RectTransform spawnContainer, bool useSpawnContainerCanvasCamera)
    {
        if (!useSpawnContainerCanvasCamera)
            return uiCam;

        if (spawnContainer == null)
            return uiCam;

        Canvas canvas = spawnContainer.GetComponentInParent<Canvas>();

        if (canvas == null)
            return uiCam;

        return canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
    }

    public void Fly(string id, int amount, IFlyTargetUI targetUI, RectTransform overrideSpawnContainer = null)
    {
        if (amount <= 0 || targetUI == null)
        {
            Debug.LogWarning("[CollectibleFlyService] amount<=0 or targetUI is null");
            return;
        }

        var entry = FindEntry(id);
        if (entry == null)
        {
            Debug.LogWarning($"[CollectibleFlyService] No entry found for id '{id}'.");
            return;
        }

        if (entry.prefabRoot == null)
        {
            Debug.LogWarning($"[CollectibleFlyService] Prefab root is NULL for '{id}'");
            return;
        }

        RectTransform spawnContainer = overrideSpawnContainer;

        if (spawnContainer == null && targetUI is CollectibleFlyTarget typedTarget)
            spawnContainer = typedTarget.GetSpawnContainerOverride();

        if (spawnContainer == null)
            spawnContainer = defaultSpawnContainer;

        if (spawnContainer == null)
        {
            Debug.LogWarning("[CollectibleFlyService] Missing spawn container.");
            return;
        }

        var icon = targetUI.GetIcon();

        if (icon == null)
        {
            Debug.LogWarning($"[CollectibleFlyService] Target icon missing for '{id}'.");
            return;
        }

        Vector2 targetScreen = targetUI.GetFlyTargetScreenPoint();

        Camera conversionCam = GetCameraForSpawnContainer(
            spawnContainer,
            entry.useSpawnContainerCanvasCamera
        );

        bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spawnContainer,
            targetScreen,
            conversionCam,
            out Vector2 targetLocal
        );

        StartCoroutine(FlyRoutine(
            amount,
            entry,
            spawnContainer,
            icon,
            targetLocal,
            targetUI,
            entry.preSpawnDelay,
            entry.useUnscaledTime
        ));
    }

    private CollectibleFlyDatabaseSO.FlyEntry FindEntry(string id)
    {
        if (database == null) return null;
        return database.Find(id);
    }

    private IEnumerator FlyRoutine(
        int amount,
        CollectibleFlyDatabaseSO.FlyEntry entry,
        RectTransform spawnContainer,
        Sprite icon,
        Vector2 targetLocal,
        IFlyTargetUI targetUI,
        float preSpawnDelay,
        bool useUnscaledTime)
    {
        if (entry.prefabRoot == null)
        {
            Debug.LogWarning("[CollectibleFlyService] Entry prefabRoot is null.");
            yield break;
        }

        if (preSpawnDelay > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(preSpawnDelay);
            else yield return new WaitForSeconds(preSpawnDelay);
        }

        yield return null;

        var instanceRoot = Instantiate(entry.prefabRoot, spawnContainer);

        var collectible = instanceRoot.Flying;
        if (collectible == null)
        {
            Debug.LogWarning("[CollectibleFlyService] FlyCollectiblePrefabRoot.flyingBehaviour is missing or does not implement IFlyingCollectible.");
            Destroy(instanceRoot.gameObject);
            yield break;
        }

        var s = entry.settings;
        if (s == null)
        {
            Debug.LogWarning("[CollectibleFlyService] Missing CollectibleFlightSettings on entry.");
            Destroy(instanceRoot.gameObject);
            yield break;
        }

        collectible.Rect.anchoredPosition = s.spawnOffset;
        collectible.Rect.localScale = Vector3.one;
        collectible.Rect.SetAsLastSibling();
        collectible.SetIcon(icon);

        if (s.holdDuration > 0f)
            yield return new WaitForSecondsRealtime(s.holdDuration);

        collectible.LaunchToLocalPoint(
            targetLocalPosition: targetLocal,
            totalDuration: s.shortFlyDuration,
            onArrive: () => targetUI.OnArrive(amount),
            delay: 0f,
            arcHeight: s.arcHeight,
            holdDuration: 0f,
            easeInCurve: s.easeInCurve,
            easeOutCurve: s.easeOutCurve
        );
    }
}