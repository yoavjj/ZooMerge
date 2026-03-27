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
    public static event System.Action<bool> OnResetCounters;

    public static void RaiseSessionStarted()
    {
        SetMergesBlocked(false);
        OnSessionStarted?.Invoke();
    }
    public static void RaiseSessionWonAnimation() => OnSessionWonAnimation?.Invoke();
    public static void RaiseResetCounters(bool keepUI)
    {
        OnResetCounters?.Invoke(keepUI);
    }


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

    /// <summary> Fired when an enemy is about to be defeated, allowing score popups to react (e.g.
    public static event System.Action<ScorePopupInstance, EnemyDefeatType> OnEnemyDefeatImminent;

    public static void RaiseEnemyDefeatImminent(ScorePopupInstance killer, EnemyDefeatType type)
        => OnEnemyDefeatImminent?.Invoke(killer, type);

    public static bool MergesBlocked { get; private set; }

    public static void SetMergesBlocked(bool blocked) => MergesBlocked = blocked;


    // Enemy defeated (but not final one in level)
    public static event System.Action OnEnemyDefeatedMidLevel;
    public static void RaiseEnemyDefeatedMidLevel() => OnEnemyDefeatedMidLevel?.Invoke();

    public static void RaiseEnemyHitWithDamage(GameObject enemy, int damage)
    {
        OnEnemyHit?.Invoke(enemy);
        OnEnemyHitWithScore?.Invoke(enemy, damage);
    }

    public static void RaiseEnemyAdvanced() => OnEnemyAdvanced?.Invoke();

    public static void RaiseEnemySessionEnded()
    {
        SetMergesBlocked(true);
        OnEnemySessionEnded?.Invoke();
    }


    // 📌 Game Over
    /// <summary>Fired when the game ends (won or lost).</summary>
    /// 

    public static bool IsGameOver { get; private set; } = false;
    public static bool WasMidLevelLoss { get; private set; }
    public static event System.Action<BallInfo, GameOverReason> OnGameOver;

    /// <summary> animation sequence to play when game is over.</summary>
    public static event System.Action OnGameOverAnimation;

    public static void ResetMidLevelLossFlag() => WasMidLevelLoss = false;

    public static void RaiseGameOver(BallInfo info, GameOverReason reason)
    {
        SetMergesBlocked(true);
        OnGameOver?.Invoke(info, reason);
        OnGameOverAnimation?.Invoke();
    }

    public static event Action<BallInfo> OnBallTouchedGameOverLine;

    public static void RaiseBallTouchedGameOverLine(BallInfo info, GameOverReason reason)
    {
        SetMergesBlocked(true);
        OnGameOver?.Invoke(info, reason);
        OnGameOverAnimation?.Invoke();
        OnBallTouchedGameOverLine?.Invoke(info);
    }

    public enum EnemyDefeatType
    {
        MidLevel,       // enemy defeated, more enemies remain
        LevelComplete   // final enemy defeated
    }

    public static event Action OnReturnToMainMenu;
    public static void RaiseReturnToMainMenu() => OnReturnToMainMenu?.Invoke();

    /// <summary>
    /// Fired when Spine die animation reached its end event.
    /// </summary>
    public static event System.Action<GameObject> OnEnemyDeathSpineEvent;

    public static void RaiseEnemyDeathSpineEvent(GameObject enemyRoot)
    {
        OnEnemyDeathSpineEvent?.Invoke(enemyRoot);
    }

    // 📌 Pause / Resume
    public static event System.Action OnSessionPaused;
    public static event System.Action OnSessionResumed;

    public static void RaiseSessionPaused() => OnSessionPaused?.Invoke();
    public static void RaiseSessionResumed() => OnSessionResumed?.Invoke();

    private static int pauseBlockCount = 0;
    public static bool PauseBlocked => pauseBlockCount > 0;

    public static void PushPauseBlock()
    {
        pauseBlockCount++;
    }

    public static void PopPauseBlock()
    {
        pauseBlockCount = Mathf.Max(0, pauseBlockCount - 1);
    }
}
