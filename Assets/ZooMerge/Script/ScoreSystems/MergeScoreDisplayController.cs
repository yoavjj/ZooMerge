using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class MergeScoreDisplayController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform poolContainer;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform targetPoint;

    [Header("Popup Prefabs By Level")]
    [SerializeField] private List<ScorePopupInstance> scorePrefabsByLevel = new();

    [Header("Path Randomness")]
    [SerializeField] private float controlPointXRange = 100f;
    [SerializeField] private float controlPointYMin = 100f;
    [SerializeField] private float controlPointYMax = 200f;

    [Header("Center Hop (Phase 1)")]
    [SerializeField] private Transform centerPoint;
    [SerializeField] private Vector2 centerRandomOffsetPx = new(120f, 120f);
    [SerializeField] private float toCenterDuration = 0.35f;
    [SerializeField] private AnimationCurve toCenterCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float centerHoldTime = 0.15f;

    [Header("Target Hop (Phase 2)")]
    [SerializeField] private float moveDuration = 0.6f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private float toTargetDurationMultiplier = 0.65f;

    [Header("Center Anti-Stacking")]
    [SerializeField] private float centerSlotSpacingPx = 42f;   // bigger = more separated
    [SerializeField] private float centerSlotJitterPx = 10f;    // small randomness
    [SerializeField] private int centerMaxSlots = 18;           // how many unique spots before wrapping

    // Keeps a unique offset per active popup so they don't stack
    private readonly Dictionary<ScorePopupInstance, Vector2> activeCenterOffsets = new();

    private readonly Dictionary<int, Queue<ScorePopupInstance>> poolsByIndex = new();

    private bool sessionEnding;

    private void OnEnable()
    {
        BallEventManager.OnMergeScore += ShowScoreAtPosition;

        BallEventManager.OnEnemySessionEnded += HandleSessionEnding;
        BallEventManager.OnSessionStarted += ClearSessionEnding;
        BallEventManager.OnEnemyAdvanced += ClearSessionEnding;
    }

    private void OnDisable()
    {
        BallEventManager.OnMergeScore -= ShowScoreAtPosition;

        BallEventManager.OnEnemySessionEnded -= HandleSessionEnding;
        BallEventManager.OnSessionStarted -= ClearSessionEnding;
        BallEventManager.OnEnemyAdvanced -= ClearSessionEnding;
    }

    private void HandleSessionEnding() => sessionEnding = true;
    private void ClearSessionEnding() => sessionEnding = false;

    private void ShowScoreAtPosition(Vector3 worldPos, int score, int level)
    {
        if (sessionEnding) return;
        
        var popup = GetOrCreatePopup(level);
        popup.SetPoolIndex(Mathf.Clamp(level - 1, 0, scorePrefabsByLevel.Count - 1));
        popup.Text.text = $"+{score}";

        GameObject enemyGO = targetPoint != null ? targetPoint.gameObject : null;

        // Base center (screen)
        Vector3 baseCenterScreen = (centerPoint != null)
            ? mainCamera.WorldToScreenPoint(centerPoint.position)
            : new Vector3(Screen.width * 0.5f, Screen.height * 0.55f, 0f);

        // 1) Big center randomness (your original)
        Vector2 rnd = new Vector2(
            Random.Range(-centerRandomOffsetPx.x, centerRandomOffsetPx.x),
            Random.Range(-centerRandomOffsetPx.y, centerRandomOffsetPx.y)
        );

        // 2) Anti-stacking unique offset (per popup)
        Vector2 slotOffset = GetOrAssignCenterOffset(popup);

        Vector3 centerScreen = baseCenterScreen + new Vector3(rnd.x + slotOffset.x, rnd.y + slotOffset.y, 0f);

        popup.Init(
            screenStart: mainCamera.WorldToScreenPoint(worldPos),
            centerScreen: centerScreen,
            cam: mainCamera,
            target: targetPoint,
            toCenterDuration: toCenterDuration,
            toCenterCurve: toCenterCurve,
            centerHoldTime: centerHoldTime,
            toTargetDuration: moveDuration * toTargetDurationMultiplier,
            toTargetCurve: moveCurve,
            onComplete: ReturnToPool,
            xRange: controlPointXRange,
            yMin: controlPointYMin,
            yMax: controlPointYMax,
            score: score,
            enemy: enemyGO
        );
    }

    private Vector2 GetOrAssignCenterOffset(ScorePopupInstance popup)
    {
        if (activeCenterOffsets.TryGetValue(popup, out var existing))
            return existing;

        // Spiral distribution (golden angle)
        int i = activeCenterOffsets.Count % Mathf.Max(1, centerMaxSlots);
        float goldenAngle = 2.39996323f; // radians
        float angle = i * goldenAngle;

        // radius grows with i -> avoids overlap
        float radius = Mathf.Sqrt(i + 1) * centerSlotSpacingPx;

        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        // small jitter so it feels organic
        offset += Random.insideUnitCircle * centerSlotJitterPx;

        activeCenterOffsets[popup] = offset;
        return offset;
    }

    private ScorePopupInstance GetOrCreatePopup(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, scorePrefabsByLevel.Count - 1);
        var prefab = scorePrefabsByLevel[index];

        if (!poolsByIndex.TryGetValue(index, out var q))
        {
            q = new Queue<ScorePopupInstance>();
            poolsByIndex[index] = q;
        }

        // Dequeue until we find a valid inactive one
        while (q.Count > 0)
        {
            var inst = q.Dequeue();
            if (inst == null) continue;

            // Only reuse if it's truly free
            if (!inst.InUse)
            {
                inst.gameObject.SetActive(true);
                return inst;
            }
        }

        // None available -> create new
        var parent = poolContainer != null ? poolContainer : transform;
        return Instantiate(prefab, parent);
    }

    private void ReturnToPool(ScorePopupInstance popup)
    {
        if (popup == null) return;

        activeCenterOffsets.Remove(popup);

        popup.gameObject.SetActive(false);

        // Put back to the right pool based on prefab index
        // (We store the index on the instance)
        int idx = popup.PoolIndex;
        if (!poolsByIndex.TryGetValue(idx, out var q))
        {
            q = new Queue<ScorePopupInstance>();
            poolsByIndex[idx] = q;
        }

        q.Enqueue(popup);
    }
}
