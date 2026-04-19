using System;
using UnityEngine;

public static class MergeLevelManager
{
    // Optional: keep old signature, but now passes "global" level number
    public static event Action<int> OnLevelChanged;

    private static MergeLevelData data;

    private static int currentGalaxyIndex = 0; // index into data.galaxies
    private static int currentLevelIndex = 0;  // index into galaxy.levels
    private static int currentEnemyIndex = 0;
    private static int pendingEnemyCoins = 0;

    public static bool LevelCompletePending { get; private set; } = false;
    public static int LevelsInCurrentGalaxy => GetCurrentGalaxy().levels?.Count ?? 0;

    public static void Initialize(MergeLevelData newData)
    {
        data = newData;

        currentGalaxyIndex = 0;
        currentLevelIndex = 0;
        currentEnemyIndex = 0;
        LevelCompletePending = false;

        RaiseLevelChanged();
    }

    private static void RaiseLevelChanged()
    {
        OnLevelChanged?.Invoke(CurrentLevelNumber);
    }

    // ---------- Galaxy / Level getters ----------
    public static GalaxyData GetCurrentGalaxy()
    {
        if (data == null || data.galaxies == null || data.galaxies.Count == 0)
            throw new Exception("Galaxy data not initialized.");

        currentGalaxyIndex = Mathf.Clamp(currentGalaxyIndex, 0, data.galaxies.Count - 1);
        return data.galaxies[currentGalaxyIndex];
    }

    public static MergeLevel GetCurrentLevel()
    {
        var galaxy = GetCurrentGalaxy();

        if (galaxy.levels == null || galaxy.levels.Count == 0)
            throw new Exception("Current galaxy has no levels.");

        currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, galaxy.levels.Count - 1);
        return galaxy.levels[currentLevelIndex];
    }

    // Public read-only
    public static int CurrentGalaxyId => GetCurrentGalaxy().galaxyId;
    public static string CurrentGalaxyName => GetCurrentGalaxy().name;
    public static int CurrentLevelInGalaxy => currentLevelIndex + 1; // 1..N from JSON

    /// <summary>
    /// "Global" level number for UI if you still want Level 1..∞ across galaxies.
    /// This assumes global numbering = sum of previous galaxy levels + current index.
    /// </summary>
    public static int CurrentLevelNumber
    {
        get
        {
            if (data == null || data.galaxies == null) return 1;

            int total = 0;
            for (int g = 0; g < currentGalaxyIndex; g++)
                total += data.galaxies[g].levels?.Count ?? 0;

            // current levelIndex is 0-based, display is +1
            return total + currentLevelIndex + 1;
        }
    }

    // ---------- Setting a level ----------
    /// <summary>
    /// Backwards compatible: set by GLOBAL level number.
    /// If you still call SetLevel(CurrentLevelNumber) from main menu, this will work.
    /// </summary>
    public static void SetLevel(int globalLevelNumber)
    {
        if (data == null || data.galaxies == null || data.galaxies.Count == 0)
            throw new Exception("Level data not initialized.");

        int target = Mathf.Max(1, globalLevelNumber);
        int running = 0;

        for (int g = 0; g < data.galaxies.Count; g++)
        {
            int count = data.galaxies[g].levels?.Count ?? 0;
            if (target <= running + count)
            {
                currentGalaxyIndex = g;
                currentLevelIndex = (target - running) - 1; // to 0-based
                currentEnemyIndex = 0;
                pendingEnemyCoins = 0;
                LevelCompletePending = false;
                RaiseLevelChanged();
                return;
            }
            running += count;
        }

        // fallback to first level
        Debug.LogWarning($"⚠️ Global Level {globalLevelNumber} not found. Defaulting to Galaxy 1 Level 1.");
        currentGalaxyIndex = 0;
        currentLevelIndex = 0;
        currentEnemyIndex = 0;
        pendingEnemyCoins = 0;
        LevelCompletePending = false;
        RaiseLevelChanged();
    }

    /// <summary>
    /// New: set by galaxyId + levelIndex inside galaxy (1-based).
    /// </summary>
    public static void SetLevel(int galaxyId, int levelIndexInGalaxy)
    {
        if (data == null || data.galaxies == null || data.galaxies.Count == 0)
            throw new Exception("Level data not initialized.");

        int gIndex = data.galaxies.FindIndex(g => g.galaxyId == galaxyId);
        if (gIndex < 0)
        {
            Debug.LogWarning($"⚠️ Galaxy {galaxyId} not found. Defaulting to Galaxy 1.");
            gIndex = 0;
        }

        var galaxy = data.galaxies[gIndex];
        int lIndex = Mathf.Clamp(levelIndexInGalaxy - 1, 0, Mathf.Max(0, (galaxy.levels?.Count ?? 1) - 1));

        currentGalaxyIndex = gIndex;
        currentLevelIndex = lIndex;

        currentEnemyIndex = 0;
        pendingEnemyCoins = 0;
        LevelCompletePending = false;

        RaiseLevelChanged();
    }

    public static int CurrentStageId
    {
        get
        {
            var level = GetCurrentLevel();
            return level.stageId > 0 ? level.stageId : level.index;
        }
    }

    public static int GetStageIdAtOffset(int offset)
    {
        var galaxy = GetCurrentGalaxy();

        int targetIndex = currentLevelIndex + offset;

        if (galaxy.levels == null || galaxy.levels.Count == 0)
            return -1;

        // Clamp inside galaxy
        targetIndex = Mathf.Clamp(targetIndex, 0, galaxy.levels.Count - 1);

        var level = galaxy.levels[targetIndex];

        return level.stageId > 0 ? level.stageId : level.index;
    }

    public static bool IsLastLevelInCurrentGalaxy
    {
        get
        {
            var galaxy = GetCurrentGalaxy();
            int count = galaxy.levels?.Count ?? 0;
            if (count <= 0) return true; // defensive: treat empty as "last"
            return currentLevelIndex >= count - 1;
        }
    }

    public static int GetGalaxyIdAtOffset(int offset)
    {
        if (data == null || data.galaxies == null || data.galaxies.Count == 0)
            return -1;

        int idx = Mathf.Clamp(currentGalaxyIndex + offset, 0, data.galaxies.Count - 1);
        return data.galaxies[idx].galaxyId;
    }

    public static string GetGalaxyNameById(int galaxyId)
    {
        if (data == null || data.galaxies == null) return string.Empty;

        var g = data.galaxies.Find(x => x.galaxyId == galaxyId);
        return g != null ? g.name : string.Empty;
    }
    
    // ---------- Advancing ----------
    public static void AdvanceLevel()
    {
        var galaxy = GetCurrentGalaxy();

        currentEnemyIndex = 0;
        pendingEnemyCoins = 0;
        LevelCompletePending = false;

        // next level inside this galaxy?
        if (currentLevelIndex + 1 < (galaxy.levels?.Count ?? 0))
        {
            currentLevelIndex++;
            RaiseLevelChanged();
            return;
        }

        // move to next galaxy if exists, else clamp to last level
        if (currentGalaxyIndex + 1 < data.galaxies.Count)
        {
            currentGalaxyIndex++;
            currentLevelIndex = 0;
            RaiseLevelChanged();
            return;
        }

        // already at final galaxy final level
        // ✅ already at final galaxy final level -> LOOP back to start
        ResetLevel();

        RaiseLevelChanged();
    }

    public static void ResetLevel()
    {
        currentGalaxyIndex = 0;
        currentLevelIndex = 0;
        currentEnemyIndex = 0;
        pendingEnemyCoins = 0;
        LevelCompletePending = false;

        RaiseLevelChanged();
    }

    // ---------- Enemy management (unchanged logic, but uses current level) ----------
    public static int GetCurrentEnemyId()
    {
        var level = GetCurrentLevel();
        if (level.enemy_data == null || level.enemy_data.Count == 0) return -1;
        if (currentEnemyIndex < 0 || currentEnemyIndex >= level.enemy_data.Count) return -1;
        return level.enemy_data[currentEnemyIndex].id;
    }

    public static bool TryAdvanceEnemy()
    {
        var level = GetCurrentLevel();
        if (level.enemy_data == null || level.enemy_data.Count == 0) return false;
        if (currentEnemyIndex < 0 || currentEnemyIndex >= level.enemy_data.Count) return false;

        pendingEnemyCoins = level.enemy_data[currentEnemyIndex].coins;

        if (currentEnemyIndex + 1 < level.enemy_data.Count)
        {
            currentEnemyIndex++;
            Debug.Log($"[MergeLevelManager] Advancing to enemy {currentEnemyIndex + 1}/{level.enemy_data.Count} for Galaxy {CurrentGalaxyId} Level {CurrentLevelInGalaxy}");
            return true;
        }

        Debug.Log($"[MergeLevelManager] All enemies defeated for Galaxy {CurrentGalaxyId} Level {CurrentLevelInGalaxy}");
        return false;
    }

    public static int ConsumePendingEnemyCoins()
    {
        int coins = pendingEnemyCoins;
        pendingEnemyCoins = 0;
        return coins;
    }

    public static int GetCurrentEnemyCoins()
    {
        var level = GetCurrentLevel();
        if (level.enemy_data == null || level.enemy_data.Count == 0) return 0;
        if (currentEnemyIndex < 0 || currentEnemyIndex >= level.enemy_data.Count) return 0;
        return level.enemy_data[currentEnemyIndex].coins;
    }

    public static int GetCurrentEnemyHealth()
    {
        var level = GetCurrentLevel();
        if (level.enemy_data == null || level.enemy_data.Count == 0) return -1;
        if (currentEnemyIndex < 0 || currentEnemyIndex >= level.enemy_data.Count) return -1;
        return level.enemy_data[currentEnemyIndex].health;
    }

    public static void ResetEnemyProgress() => currentEnemyIndex = 0;

    public static int CurrentEnemyIndex => currentEnemyIndex;

    public static int TotalEnemiesInLevel => GetCurrentLevel().enemy_data?.Count ?? 0;

    public static void MarkLevelCompletePending() => LevelCompletePending = true;
    public static void ClearLevelCompletePending() => LevelCompletePending = false;
}