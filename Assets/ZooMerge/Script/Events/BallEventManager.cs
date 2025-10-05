using System;
using UnityEngine;

public static class BallEventManager
{
    public static System.Action<BallInfo> OnBallMerged;
    public static System.Action<BallInfo> OnGameOver;
    public static System.Action OnGameOverAnimation; // 🔹 New event

    public static void RaiseBallMerged(BallInfo info) => OnBallMerged?.Invoke(info);
    public static void RaiseGameOver(BallInfo info)
    {
        OnGameOver?.Invoke(info);
        OnGameOverAnimation?.Invoke(); // 🔹 Fire animation event
    }
}


