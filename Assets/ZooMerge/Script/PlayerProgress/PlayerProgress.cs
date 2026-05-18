using UnityEngine;

public static class PlayerProgress
{
    private const string KEY_LAST_GALAXY = "PROG_LastGalaxyId";
    private const string KEY_LAST_LEVEL_IN_GALAXY = "PROG_LastLevelInGalaxy";
    private const string KEY_LAST_ENEMY_INDEX = "PROG_LastEnemyIndex"; // NEW (0-based)

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

    // NEW: enemy index inside the level (0..N-1)
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

        // ✅ Save enemy progress too
        LastEnemyIndex = MergeLevelManager.CurrentEnemyIndex;

        SaveNow();
    }

    public static void ResetProgressToStart()
    {
        LastGalaxyId = 1;
        LastLevelInGalaxy = 1;
        LastEnemyIndex = 0;
        SaveNow();

        // Apply immediately (see below: you'll need a SetProgress overload that supports enemy index)
        MergeLevelManager.SetProgress(1, 1, 0);
    }

    public static void SetResumePoint(int galaxyId, int levelInGalaxy, int enemyIndex)
    {
        LastGalaxyId = galaxyId;
        LastLevelInGalaxy = levelInGalaxy;
        LastEnemyIndex = enemyIndex;
        SaveNow();
    }
}