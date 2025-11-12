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

    [Header("Movement Settings")]
    [SerializeField] private float moveDuration = 0.6f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private float holdDurationBeforeFly = 0.6f;

    [Header("Path Randomness")]
    [SerializeField] private float controlPointXRange = 100f;
    [SerializeField] private float controlPointYMin = 100f;
    [SerializeField] private float controlPointYMax = 200f;

    private Queue<ScorePopupInstance> pool = new();

    private void OnEnable()
    {
        BallEventManager.OnMergeScore += ShowScoreAtPosition;
    }

    private void OnDisable()
    {
        BallEventManager.OnMergeScore -= ShowScoreAtPosition;
    }

    private void ShowScoreAtPosition(Vector3 worldPos, int score, int level)
    {
        var popup = GetOrCreatePopup(level);

        popup.Text.text = $"+{score}";

        popup.Init(
            screenStart: mainCamera.WorldToScreenPoint(worldPos),
            cam: mainCamera,
            target: targetPoint,
            duration: moveDuration,
            holdTime: holdDurationBeforeFly,
            curve: moveCurve,
            onComplete: ReturnToPool,
            xRange: controlPointXRange,
            yMin: controlPointYMin,
            yMax: controlPointYMax,
            score: score
        );
    }

    private ScorePopupInstance GetOrCreatePopup(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, scorePrefabsByLevel.Count - 1);
        var selectedPrefab = scorePrefabsByLevel[index];

        // Check pool for available instance of this prefab
        foreach (var popup in pool)
        {
            if (popup.name.Contains(selectedPrefab.name))
            {
                pool = new Queue<ScorePopupInstance>(pool); // remove from pool safely
                popup.gameObject.SetActive(true);
                return popup;
            }
        }

        // Instantiate new one if not found
        var parent = poolContainer != null ? poolContainer : transform;
        return Instantiate(selectedPrefab, parent);
    }

    private void ReturnToPool(ScorePopupInstance popup)
    {
        popup.gameObject.SetActive(false);
        pool.Enqueue(popup);
    }
}
