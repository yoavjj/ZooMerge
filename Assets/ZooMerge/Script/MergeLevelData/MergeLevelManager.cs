using System;
using UnityEngine;

public static class MergeLevelManager
{
    private static MergeLevelData levelData;
    private static int currentLevelIndex = 0;

    public static void Initialize(MergeLevelData data)
    {
        levelData = data;
        currentLevelIndex = 0;
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
    }

    public static MergeLevel GetCurrentLevel()
    {
        if (levelData == null || levelData.levels.Count == 0)
            throw new Exception("Level data not initialized.");

        return levelData.levels[Mathf.Clamp(currentLevelIndex, 0, levelData.levels.Count - 1)];
    }

    public static void AdvanceLevel()
    {
        currentLevelIndex = Mathf.Min(currentLevelIndex + 1, levelData.levels.Count - 1);
    }

    public static void ResetLevel()
    {
        currentLevelIndex = 0;
    }

    public static int CurrentLevelNumber => GetCurrentLevel().level;
}
