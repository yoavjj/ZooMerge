using UnityEngine;
using System;

public static class BallEventManager
{
    private static int lastScore = 0; // 🔸 Cache the last score

    public static Action<BallInfo> OnBallMerged;
    public static Action<BallInfo> OnGameOver;
    public static Action OnGameOverAnimation;
    public static Action<GameObject> OnEnemyHit;
    public static event Action OnSessionStarted;
    public static event Action<Vector3, int> OnMergeScore;
    public static event Action OnSessionWonAnimation;
    public static event Action OnResetCounters;

    public static void RaiseResetCounters() => OnResetCounters?.Invoke();

    public static void RaiseSessionWonAnimation()
    {
        OnSessionWonAnimation?.Invoke();
    }

    public static void RaiseBallMerged(BallInfo info) => OnBallMerged?.Invoke(info);

    public static void RaiseMergeScore(Vector3 pos, int score)
    {
        lastScore = score; // 🔸 Store it
        OnMergeScore?.Invoke(pos, score);
    }

    public static void RaiseEnemyHit(GameObject enemy)
    {
        OnEnemyHit?.Invoke(enemy);
        OnEnemyHitWithScore?.Invoke(lastScore); // 🔸 Use cached score
    }

    public static void RaiseGameOver(BallInfo info)
    {
        OnGameOver?.Invoke(info);
        OnGameOverAnimation?.Invoke();
    }

    public static void RaiseSessionStarted() => OnSessionStarted?.Invoke(); public static event Action<int> OnEnemyHitWithScore;
}
