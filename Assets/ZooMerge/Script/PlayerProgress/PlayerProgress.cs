using UnityEngine;

public static class PlayerProgress
{
    // -------- Local resume --------
    private const string KEY_LAST_GALAXY = "PROG_LastGalaxyId";
    private const string KEY_LAST_LEVEL_IN_GALAXY = "PROG_LastLevelInGalaxy";
    private const string KEY_LAST_ENEMY_INDEX = "PROG_LastEnemyIndex";

    public static int LastGalaxyId
    {
        get => PlayerPrefs.GetInt(KEY_LAST_GALAXY, 1);
        set => PlayerPrefs.SetInt(KEY_LAST_GALAXY, Mathf.Max(1, value));
    }

    public static int LastLevelInGalaxy
    {
        get => PlayerPrefs.GetInt(KEY_LAST_LEVEL_IN_GALAXY, 1);
        set => PlayerPrefs.SetInt(KEY_LAST_LEVEL_IN_GALAXY, Mathf.Max(1, value));
    }

    public static int LastEnemyIndex
    {
        get => PlayerPrefs.GetInt(KEY_LAST_ENEMY_INDEX, 0);
        set => PlayerPrefs.SetInt(KEY_LAST_ENEMY_INDEX, Mathf.Max(0, value));
    }

    public static void SaveNow() => PlayerPrefs.Save();

    public static void CaptureFromManagers()
    {
        LastGalaxyId = MergeLevelManager.CurrentGalaxyId;
        LastLevelInGalaxy = MergeLevelManager.CurrentLevelInGalaxy;
        LastEnemyIndex = MergeLevelManager.CurrentEnemyIndex;
        SaveNow();
    }

    public static void SetResumePoint(int galaxyId, int levelInGalaxy, int enemyIndex)
    {
        LastGalaxyId = galaxyId;
        LastLevelInGalaxy = levelInGalaxy;
        LastEnemyIndex = enemyIndex;
        SaveNow();
    }

    // -------- Checkpoint + gated new-level retries --------
    private const string KEY_CHECKPOINT_GALAXY = "PROG_CheckpointGalaxy";
    private const string KEY_CHECKPOINT_LEVEL = "PROG_CheckpointLevel";
    private const string KEY_NEWLEVEL_RETRIES = "PROG_NewLevelRetriesRemaining";

    // ✅ Dynamic hook (later you can read Remote Config here)
    public static int GetNewLevelRetryLimit() => 3;

    public static int CheckpointGalaxyId
    {
        get => PlayerPrefs.GetInt(KEY_CHECKPOINT_GALAXY, 1);
        set => PlayerPrefs.SetInt(KEY_CHECKPOINT_GALAXY, Mathf.Max(1, value));
    }

    public static int CheckpointLevelInGalaxy
    {
        get => PlayerPrefs.GetInt(KEY_CHECKPOINT_LEVEL, 1);
        set => PlayerPrefs.SetInt(KEY_CHECKPOINT_LEVEL, Mathf.Max(1, value));
    }

    public static int NewLevelRetriesRemaining
    {
        get => PlayerPrefs.GetInt(KEY_NEWLEVEL_RETRIES, GetNewLevelRetryLimit());
        set => PlayerPrefs.SetInt(KEY_NEWLEVEL_RETRIES, Mathf.Max(0, value));
    }

    public static bool IsOnCheckpoint(int galaxyId, int levelInGalaxy)
        => galaxyId == CheckpointGalaxyId && levelInGalaxy == CheckpointLevelInGalaxy;

    // ✅ Only the "next level after checkpoint" is gated (simple friendly loop)
    public static bool IsOnNewLevel(int galaxyId, int levelInGalaxy)
    {
        // Tutorial level is always unlimited
        if (galaxyId == 1 && levelInGalaxy == 1)
            return false;

        bool sameGalaxy = galaxyId == CheckpointGalaxyId;
        bool isNextLevel = sameGalaxy && levelInGalaxy == CheckpointLevelInGalaxy + 1;

        return isNextLevel;
    }

    public static bool HasRetryLimitForCurrentLevel()
        => IsOnNewLevel(LastGalaxyId, LastLevelInGalaxy);

    // ✅ What WinLosePopup should check for gating
    public static int CurrentLevelRetriesRemaining()
    {
        if (!HasRetryLimitForCurrentLevel())
            return int.MaxValue; // unlimited (checkpoint/tutorial)

        return NewLevelRetriesRemaining;
    }

    public static void RefillRetriesForCurrentNewLevel()
    {
        NewLevelRetriesRemaining = GetNewLevelRetryLimit();
        SaveNow();
    }

    // Call when a run starts
    public static void OnLevelStarted(int galaxyId, int levelInGalaxy)
    {
        // Keep local resume in sync
        LastGalaxyId = galaxyId;
        LastLevelInGalaxy = levelInGalaxy;

        // If this is the gated new level, ensure retries are initialized/clamped
        if (IsOnNewLevel(galaxyId, levelInGalaxy))
        {
            int limit = GetNewLevelRetryLimit();
            if (NewLevelRetriesRemaining > limit)
                NewLevelRetriesRemaining = limit;
        }

        SaveNow();
    }

    // Call on loss
    public static void OnLoss(int galaxyId, int levelInGalaxy)
    {
        if (!IsOnNewLevel(galaxyId, levelInGalaxy))
            return; // unlimited retries for checkpoint/tutorial

        NewLevelRetriesRemaining = Mathf.Max(0, NewLevelRetriesRemaining - 1);
        SaveNow();
    }

    // Call on FULL level completion (end of level)
    public static void OnLevelCompleted(int galaxyId, int levelInGalaxy)
    {
        CheckpointGalaxyId = galaxyId;
        CheckpointLevelInGalaxy = levelInGalaxy;

        // reset retries for the next new-level attempt
        NewLevelRetriesRemaining = GetNewLevelRetryLimit();
        SaveNow();
    }

    // Called when player can't pay at 0 retries
    public static void FallbackToCheckpoint()
    {
        LastGalaxyId = CheckpointGalaxyId;
        LastLevelInGalaxy = CheckpointLevelInGalaxy;
        LastEnemyIndex = 0;

        SaveNow();

        MergeLevelManager.SetProgress(LastGalaxyId, LastLevelInGalaxy, LastEnemyIndex);
    }

    public static void ResetProgressToStart()
    {
        LastGalaxyId = 1;
        LastLevelInGalaxy = 1;
        LastEnemyIndex = 0;

        CheckpointGalaxyId = 1;
        CheckpointLevelInGalaxy = 1;

        NewLevelRetriesRemaining = GetNewLevelRetryLimit();

        SaveNow();

        MergeLevelManager.SetProgress(1, 1, 0);
        CloudSaveManager.ForceCloudProgressMap(1, 1, 0);
    }

    public static int PeekRetriesAfterLoss(int galaxyId, int levelInGalaxy)
    {
        if (!IsOnNewLevel(galaxyId, levelInGalaxy))
            return int.MaxValue; // unlimited (checkpoint/tutorial)

        return Mathf.Max(0, NewLevelRetriesRemaining - 1);
    }
}