using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// Central manager for all game-wide merge/ball/enemy-related events.
/// Use `RaiseXYZ` methods to trigger and listen using corresponding events.
/// </summary>
public static class BallEventManager
{
    // --- Game Over ---
    public enum GameOverReason
    {
        Unknown,
        Won,
        Lost
    }

    // 📌 Session lifecycle
    public static event System.Action OnSessionStarted;
    public static event System.Action OnSessionWonAnimation;
    public static event System.Action OnResetCounters;

    public static void RaiseSessionStarted() => OnSessionStarted?.Invoke();
    public static void RaiseSessionWonAnimation() => OnSessionWonAnimation?.Invoke();
    public static void RaiseResetCounters() => OnResetCounters?.Invoke();


    // 📌 Merge Events
    /// <summary>Called when two balls are successfully merged.</summary>
    public static event System.Action<BallInfo> OnBallMerged;
    public static void RaiseBallMerged(BallInfo info) => OnBallMerged?.Invoke(info);

    /// <summary>Triggered to show popup and update score when merge occurs.</summary>
    public static event System.Action<Vector3, int, int> OnMergeScore;
    public static void RaiseMergeScore(Vector3 position, int score, int level)
        => OnMergeScore?.Invoke(position, score, level);


    // 📌 Enemy Events
    /// <summary>Called when an enemy is hit. Carries GameObject reference.</summary>
    public static event System.Action<GameObject> OnEnemyHit;

    /// <summary>Extended event with score (damage) included.</summary>
    public static event System.Action<GameObject, int> OnEnemyHitWithScore;

    /// <summary>Fired when an enemy is defeated and next one should load.</summary>
    public static event System.Action OnEnemyAdvanced;

    /// <summary>Fired when an enemy session ends (e.g. on defeat, before transition).</summary>
    public static event System.Action OnEnemySessionEnded;

    public static void RaiseEnemyHitWithDamage(GameObject enemy, int damage)
    {
        OnEnemyHit?.Invoke(enemy);
        OnEnemyHitWithScore?.Invoke(enemy, damage);
    }

    public static void RaiseEnemyAdvanced() => OnEnemyAdvanced?.Invoke();
    public static void RaiseEnemySessionEnded() => OnEnemySessionEnded?.Invoke();


    // 📌 Game Over
    /// <summary>Fired when the game ends (won or lost).</summary>

    public static bool WasMidLevelLoss { get; private set; }
    public static event System.Action<BallInfo, GameOverReason> OnGameOver;

    /// <summary> animation sequence to play when game is over.</summary>
    public static event System.Action OnGameOverAnimation;

    public static void ResetMidLevelLossFlag() => WasMidLevelLoss = false;

    public static void RaiseGameOver(BallInfo info, GameOverReason reason)
    {
        // WasMidLevelLoss = reason == GameOverReason.Lost && MergeLevelManager.CurrentEnemyIndex > 0;

        // if (WasMidLevelLoss)
        // {
        //     BallStateSaver.Instance.SaveState(BallRegistry.ActiveBalls.ToArray());
        // }

        OnGameOver?.Invoke(info, reason);
        OnGameOverAnimation?.Invoke();
    }
}
