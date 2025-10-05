using System.Collections.Generic;
using UnityEngine;

public static class BallRegistry
{
    private static readonly HashSet<BallInfo> activeBalls = new();

    public static IReadOnlyCollection<BallInfo> ActiveBalls => activeBalls;

    public static void Register(BallInfo ball)
    {
        if (ball != null)
            activeBalls.Add(ball);
        Debug.Log($"Ball registered: {ball.name}. Total active balls: {activeBalls.Count}");
    }

    public static void Unregister(BallInfo ball)
    {
        if (ball != null)
            activeBalls.Remove(ball);
        Debug.Log($"Ball unregistered: {ball.name}. Total active balls: {activeBalls.Count}");
    }

    public static void Clear()
    {
        activeBalls.Clear();
    }
}
