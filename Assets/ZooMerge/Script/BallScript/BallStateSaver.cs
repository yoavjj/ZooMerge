using System.Collections.Generic;
using UnityEngine;
using static MergeSessionTracker;

public class BallStateSaver
{
    private static BallStateSaver _instance;
    public static BallStateSaver Instance => _instance ??= new BallStateSaver();
    private List<MergeCounterSnapshot> savedCounterSnapshot = new();


    public void SaveState(BallInfo[] balls)
    {
        // ✅ NEW: remove "danger" balls before saving mid-level state
        DestroyBallsTouchingGameOver();

        BallRegistry.SaveState(balls);

        if (MergeSessionTracker.Instance != null)
            savedCounterSnapshot = MergeSessionTracker.Instance.SaveCounterState();
    }

    public void RestoreState(Transform droppedContainer)
    {
        if (droppedContainer == null)
        {
            Debug.LogWarning("[BallStateSaver] RestoreState failed: droppedContainer is null.");
            return;
        }

        Debug.Log($"[BallStateSaver] RestoreState requested. Saved balls={BallRegistry.SavedBallCount}");

        // Copy first, because destroying can modify the registry.
        var liveBalls = new List<BallInfo>(BallRegistry.ActiveBalls);

        foreach (var ball in liveBalls)
        {
            if (ball != null)
                Object.Destroy(ball.gameObject);
        }

        BallRegistry.Clear();

        BallRegistry.RestoreState(droppedContainer);

        if (MergeSessionTracker.Instance != null)
            MergeSessionTracker.Instance.RestoreCounterState(savedCounterSnapshot);
    }

    public void Clear()
    {
        // Only clears the saved list, doesn't touch live scene balls
        BallRegistry.Clear();
    }

    private void DestroyBallsTouchingGameOver()
    {
        var toDestroy = new List<BallInfo>();

        foreach (var ball in BallRegistry.ActiveBalls)
        {
            if (ball == null) continue;

            var dc = ball.DropController;
            if (dc != null && dc.IsTouchingGameOver)
                toDestroy.Add(ball);
        }

        foreach (var ball in toDestroy)
        {
            BallRegistry.Unregister(ball);   // keep registry clean
            if (ball != null)
                Object.Destroy(ball.gameObject);
        }
    }
}
