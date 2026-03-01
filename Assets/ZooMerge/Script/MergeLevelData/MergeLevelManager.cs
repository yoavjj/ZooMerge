using System;
using UnityEngine;
using static Inventory;

public static class MergeLevelManager
{
    public static event System.Action<int> OnLevelChanged;
    private static void RaiseLevelChanged() => OnLevelChanged?.Invoke(CurrentLevelNumber);

    private static MergeLevelData levelData;
    private static int currentLevelIndex = 0;
    private static int currentEnemyIndex = 0;
    private static int pendingEnemyCoins = 0;

    public static void Initialize(MergeLevelData data)
    {
        RaiseLevelChanged();

        levelData = data;
        currentLevelIndex = 0;
        currentEnemyIndex = 0;
        LevelCompletePending = false;
    }

    public static void SetLevel(int levelNumber)
    {
        if (levelData == null || levelData.levels.Count == 0)
            throw new Exception("Level data not initialized.");

        int index = levelData.levels.FindIndex(l => l.level == levelNumber);
        if (index == -1)
        {
            Debug.LogWarning($"⚠️ Level {levelNumber} not found. Defaulting to level 1.");
            index = 0;
        }

        currentLevelIndex = index;
        currentEnemyIndex = 0; // Reset enemy progression for this level
        LevelCompletePending = false;

        RaiseLevelChanged();
    }

    public static MergeLevel GetCurrentLevel()
    {
        if (levelData == null || levelData.levels.Count == 0)
            throw new Exception("Level data not initialized.");

        return levelData.levels[Mathf.Clamp(currentLevelIndex, 0, levelData.levels.Count - 1)];
    }

    public static int CurrentLevelNumber => GetCurrentLevel().level;

    public static void AdvanceLevel()
    {
        var current = GetCurrentLevel();

        currentLevelIndex = Mathf.Min(currentLevelIndex + 1, levelData.levels.Count - 1);
        currentEnemyIndex = 0;
        LevelCompletePending = false;

        RaiseLevelChanged();
    }

    public static void ResetLevel()
    {
        currentLevelIndex = 0;
        currentEnemyIndex = 0;
        LevelCompletePending = false;

        RaiseLevelChanged();
    }

    // ✅ --- ENEMY MANAGEMENT ---
    public static int GetCurrentEnemyId()
    {
        var level = GetCurrentLevel();
        if (level.enemy_data == null || level.enemy_data.Count == 0)
            return -1;

        if (currentEnemyIndex < 0 || currentEnemyIndex >= level.enemy_data.Count)
            return -1;

        return level.enemy_data[currentEnemyIndex].id;
    }

    /// <summary>
    /// Advances to the next enemy in the current level. 
    /// Returns true if there is another enemy remaining, false if all are done.
    /// </summary>
    public static bool TryAdvanceEnemy()
    {
        var level = GetCurrentLevel();

        if (level.enemy_data == null || level.enemy_data.Count == 0)
            return false;

        if (currentEnemyIndex < 0 || currentEnemyIndex >= level.enemy_data.Count)
            return false;

        // 🔐 Capture coins BEFORE advancing
        pendingEnemyCoins = level.enemy_data[currentEnemyIndex].coins;

        if (currentEnemyIndex + 1 < level.enemy_data.Count)
        {
            currentEnemyIndex++;
            Debug.Log($"[MergeLevelManager] Advancing to enemy {currentEnemyIndex + 1}/{level.enemy_data.Count} for Level {level.level}");
            return true;
        }

        Debug.Log($"[MergeLevelManager] All enemies defeated for Level {level.level}");
        return false;
    }

    public static int ConsumePendingEnemyCoins()
    {
        int coins = pendingEnemyCoins;
        pendingEnemyCoins = 0; // prevent double-claim
        return coins;
    }

    public static int GetCurrentEnemyCoins()
    {
        var level = GetCurrentLevel();

        if (level.enemy_data == null || level.enemy_data.Count == 0)
            return 0;

        if (currentEnemyIndex < 0 || currentEnemyIndex >= level.enemy_data.Count)
            return 0;

        return level.enemy_data[currentEnemyIndex].coins;
    }

    public static int GetCurrentEnemyHealth()
    {
        var level = GetCurrentLevel();
        if (level.enemy_data == null || level.enemy_data.Count == 0)
            return -1;

        if (currentEnemyIndex < 0 || currentEnemyIndex >= level.enemy_data.Count)
            return -1;

        return level.enemy_data[currentEnemyIndex].health;
    }

    /// <summary>
    /// Resets the current enemy index to the first enemy for the current level.
    /// </summary>
    public static void ResetEnemyProgress()
    {
        currentEnemyIndex = 0;
    }

    /// <summary>
    /// Returns the current enemy index (0-based) for tracking UI or debugging.
    /// </summary>
    public static int CurrentEnemyIndex => currentEnemyIndex;

    /// <summary>
    /// Returns the total number of enemies for the current level.
    /// </summary>
    public static int TotalEnemiesInLevel => GetCurrentLevel().enemy_data?.Count ?? 0;

    /// <summary>
    /// Indicates whether the player has defeated all enemies in the current level and is pending level completion.
    /// </summary>

    public static bool LevelCompletePending { get; private set; } = false;

    public static void MarkLevelCompletePending()
    {
        LevelCompletePending = true;
    }

    public static void ClearLevelCompletePending()
    {
        LevelCompletePending = false;
    }
}