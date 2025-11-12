using UnityEngine;

public class BallStateSaver
{
    private static BallStateSaver _instance;
    public static BallStateSaver Instance => _instance ??= new BallStateSaver();

    public void SaveState(BallInfo[] balls)
    {
        BallRegistry.SaveState(balls);
    }

    public void RestoreState(Transform droppedContainer)
    {
        // ✅ First, destroy existing balls using registry
        foreach (var ball in BallRegistry.ActiveBalls)
        {
            if (ball != null)
                Object.Destroy(ball.gameObject);
        }

        BallRegistry.Clear(); // remove them from registry too

        // ✅ Then restore
        BallRegistry.RestoreState(droppedContainer);
    }

    public void Clear()
    {
        // Only clears the saved list, doesn't touch live scene balls
        BallRegistry.Clear();
    }
}
