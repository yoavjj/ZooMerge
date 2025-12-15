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
        BallRegistry.SaveState(balls);

        if (MergeSessionTracker.Instance != null)
            savedCounterSnapshot = MergeSessionTracker.Instance.SaveCounterState();
    }

    public void RestoreState(Transform droppedContainer)
    {
        foreach (var ball in BallRegistry.ActiveBalls)
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
}
