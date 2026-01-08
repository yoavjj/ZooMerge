using UnityEngine;

[System.Serializable]
public class UICanvasContext
{
    public Canvas canvas;

    public Camera Camera =>
        canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
}

public class CollectibleFlyController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private FlyingCollectible collectiblePrefab;
    [SerializeField] private RectTransform container;

    [Header("Canvas Context")]
    [SerializeField] private Canvas rootCanvas;

    [Header("Flight Settings")]
    [SerializeField] private float flyDuration = 0.6f;
    [SerializeField] private float holdDuration = 0.2f; // 🆕 duration to hover before moving
    [SerializeField] private float maxStartOffset = 30f; // 🆕 random spawn radius
    [SerializeField] private float arcHeight = 100f;

    [Header("Easing")]
    [SerializeField] private AnimationCurve easeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve easeOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Spawn Area")]
    [SerializeField] private CollectibleSpawnCircle spawnCircle;

    private Camera uiCam;

    private void Awake()
    {
        uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera
            : null;
    }

    public void Spawn(
        int amount,
        RectTransform startUI,
        Vector2 targetScreenPoint,
        System.Action onEachArrive)
    {
        if (collectiblePrefab == null || container == null || spawnCircle == null)
        {
            Debug.LogError("Missing references on CollectibleFlyController");
            return;
        }

        // Target position in local space
        Vector2 targetLocal = ScreenToContainerLocal(targetScreenPoint);

        for (int i = 0; i < amount; i++)
        {
            var item = Instantiate(collectiblePrefab, container);

            // 🟢 Get spawn offset from the circle
            Vector2 offset = spawnCircle.GetPointOnCircle();
            Vector2 spawnLocal = (Vector2)container.InverseTransformPoint(spawnCircle.transform.position) + offset;

            item.Rect.anchoredPosition = spawnLocal;

            float delay = i * 0.05f + Random.Range(0f, 0.05f);

            item.LaunchToLocalPoint(
                targetLocal,
                flyDuration,
                onEachArrive,
                delay,
                arcHeight,
                holdDuration,
                easeInCurve,
                easeOutCurve
            );
        }
    }

    private Vector2 ScreenToContainerLocal(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            container,
            screenPoint,
            uiCam,
            out Vector2 local
        );
        return local;
    }
}
