using System.Collections.Generic;
using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

public class CollectibleFlyController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private FlyingCollectible collectiblePrefab;
    [SerializeField] private RectTransform container;

    [Header("Canvas Context")]
    [SerializeField] private Canvas rootCanvas;

    [Header("Flight Settings")]
    [SerializeField, Min(0.1f)] private float shortFlyDuration = 1.2f;  // For 3 or fewer
    [SerializeField, Min(0.1f)] private float longFlyDuration = 1.7f;  // For 4 or more
    [SerializeField, Min(0f)] private float holdDuration = 0.2f;
    [SerializeField] private float arcHeight = 100f;
    [SerializeField, Tooltip("Delay between each collectible's arrival callback (seconds)")]
    private float arrivalStaggerDelay = 0.1f;

    [Header("Easing")]
    [SerializeField] private AnimationCurve easeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve easeOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

#if UNITY_EDITOR
    [Header("🧪 Debug Replay (Play Mode Only)")]
    [SerializeField] private bool debugReplay;

    private CollectibleSpawnCircle lastUsedCircle;
    private int lastUsedAmount;
    private Vector2 lastUsedTargetScreenPoint;
    private Sprite lastUsedIcon;
    private bool debugRunning;

    private void OnValidate()
    {
        if (!Application.isPlaying || !debugReplay || debugRunning) return;
        debugRunning = true;
        StartCoroutine(DebugReplayRoutine());
    }
#endif

    private Camera uiCam;

    private void Awake()
    {
        uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera
            : null;
    }

    public void SpawnFromPreparedData(
        List<CollectibleFlightData> flightData,
        System.Action<int> onEachArriveWithCount
    )
    {
        StartCoroutine(SpawnWithDelayRoutine(flightData, onEachArriveWithCount));
    }

    private IEnumerator SpawnWithDelayRoutine(
        List<CollectibleFlightData> flightData,
        System.Action<int> onEachArriveWithCount
    )
    {
        foreach (var data in flightData)
        {
            var item = Instantiate(collectiblePrefab, container);
            item.Rect.anchoredPosition = data.spawnPosition;
            item.SetIcon(data.icon);

            item.LaunchToLocalPoint(
                data.targetPosition,
                data.flyDuration,
                () => onEachArriveWithCount?.Invoke(data.count),
                delay: 0f,
                arcHeight,
                holdDuration,
                easeInCurve,
                easeOutCurve
            );

            // 🔁 Add a short random delay before the next one spawns
            float delay = Random.Range(0.015f, 0.05f); // tweak as needed
            yield return new WaitForSecondsRealtime(delay);
        }
    }

    public List<CollectibleFlightData> PrepareFlightData(
    CollectibleSpawnCircle circle,
    int amount,
    Vector2 targetScreenPoint,
    Sprite icon,
    int totalCountToDistribute = 0
)
    {
        List<CollectibleFlightData> result = new(amount);

        if (circle == null || container == null)
        {
            Debug.LogError("Missing references for flight preparation.");
            return result;
        }

        Vector2 targetLocal = ScreenToContainerLocal(targetScreenPoint);
        Vector2 circleCenter = (Vector2)container.InverseTransformPoint(circle.transform.position);
        List<Vector2> fixedPoints = circle.GetFixedSpawnPoints();
        List<int> availableIndices = GetShuffledIndices(fixedPoints.Count);
        List<int> counts = CalculateCountsPerCollectible(totalCountToDistribute, amount);

        float baseFlyDuration = amount <= 3 ? shortFlyDuration : longFlyDuration;
        float totalStaggerTime = (amount - 1) * arrivalStaggerDelay;
        float safeFlyDuration = Mathf.Max(baseFlyDuration + totalStaggerTime, baseFlyDuration);

        for (int i = 0; i < amount; i++)
        {
            int index = (i < availableIndices.Count) ? availableIndices[i] : i % fixedPoints.Count;
            Vector2 spawn = circleCenter + fixedPoints[index];

            float adjustedFlyDuration = safeFlyDuration - ((amount - 1 - i) * arrivalStaggerDelay);

            result.Add(new CollectibleFlightData
            {
                spawnPosition = spawn,
                targetPosition = targetLocal,
                icon = icon,
                count = counts[i],
                flyDuration = adjustedFlyDuration
            });
        }

        return result;
    }

    private List<int> CalculateCountsPerCollectible(int total, int amount)
    {
        List<int> result = new(amount);

        if (total <= amount || total <= 0)
        {
            for (int i = 0; i < amount; i++) result.Add(1);
        }
        else
        {
            int baseAmount = total / amount;
            int remainder = total % amount;

            for (int i = 0; i < amount; i++)
            {
                int value = baseAmount;
                if (i == amount - 1) value += remainder;
                result.Add(value);
            }
        }

        return result;
    }

    private void SpawnOneCollectible(
        Vector2 spawnLocal,
        Vector2 targetLocal,
        Sprite icon,
        int count,
        float flyTime,
        System.Action<int> onArrive
    )
    {
        var item = Instantiate(collectiblePrefab, container);
        item.Rect.anchoredPosition = spawnLocal;
        item.SetIcon(icon);

        item.LaunchToLocalPoint(
            targetLocal,
            flyTime,
            () => onArrive?.Invoke(count),
            delay: 0f,
            arcHeight,
            holdDuration,
            easeInCurve,
            easeOutCurve
        );
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

#if UNITY_EDITOR
    private IEnumerator DebugReplayRoutine()
    {
        if (lastUsedCircle == null || lastUsedIcon == null)
        {
            Debug.LogWarning("⚠️ No saved spawn data from gameplay. Play a real animation first.");
            ResetDebugToggle();
            yield break;
        }

        var flightData = PrepareFlightData(
            circle: lastUsedCircle,
            amount: lastUsedAmount,
            targetScreenPoint: lastUsedTargetScreenPoint,
            icon: lastUsedIcon,
            totalCountToDistribute: lastUsedAmount
        );

        SpawnFromPreparedData(
            flightData,
            onEachArriveWithCount: (count) =>
            {
                Debug.Log($"🧪 Debug collectible arrived with count {count}");
            }
        );

        yield return new WaitForSecondsRealtime(longFlyDuration + holdDuration + 0.2f);
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
