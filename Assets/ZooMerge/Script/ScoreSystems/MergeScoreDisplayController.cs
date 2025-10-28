using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class MergeScoreDisplayController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform poolContainer;
    [SerializeField] private ScorePopupInstance popupPrefab;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform targetPoint;

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

    private void ShowScoreAtPosition(Vector3 worldPos, int score)
    {
        var popup = GetOrCreatePopup();

        // 🟢 Set the score text!
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
            yMax: controlPointYMax
        );
    }

    private ScorePopupInstance GetOrCreatePopup()
    {
        if (pool.Count > 0)
        {
            var popup = pool.Dequeue();
            popup.gameObject.SetActive(true);
            return popup;
        }

        var parent = poolContainer != null ? poolContainer : transform;
        return Instantiate(popupPrefab, parent);
    }

    private void ReturnToPool(ScorePopupInstance popup)
    {
        popup.gameObject.SetActive(false);
        pool.Enqueue(popup);
    }
}
