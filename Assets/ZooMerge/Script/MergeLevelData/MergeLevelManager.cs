using System;
using UnityEngine;

public static class MergeLevelManager
{
    private static MergeLevelData levelData;
    private static int currentLevelIndex = 0;
    private static int currentEnemyIndex = 0;

    public static void Initialize(MergeLevelData data)
    {
        levelData = data;
        currentLevelIndex = 0;
        currentEnemyIndex = 0;
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
        currentLevelIndex = Mathf.Min(currentLevelIndex + 1, levelData.levels.Count - 1);
        currentEnemyIndex = 0;
    }

    public static void ResetLevel()
    {
        currentLevelIndex = 0;
        currentEnemyIndex = 0;
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

        if (currentEnemyIndex + 1 < level.enemy_data.Count)
        {
            currentEnemyIndex++;
            Debug.Log($"[MergeLevelManager] Advancing to enemy {currentEnemyIndex + 1}/{level.enemy_data.Count} for Level {level.level}");
            return true;
        }

        Debug.Log($"[MergeLevelManager] All enemies defeated for Level {level.level}");
        return false;
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
}