using UnityEngine;

public static class PlayerProgress
{
    private const string KEY_LAST_GALAXY = "PROG_LastGalaxyId";
    private const string KEY_LAST_LEVEL_IN_GALAXY = "PROG_LastLevelInGalaxy";

    public static int LastGalaxyId
    {
        get => PlayerPrefs.GetInt(KEY_LAST_GALAXY, 1); // default galaxy 1
        set => PlayerPrefs.SetInt(KEY_LAST_GALAXY, Mathf.Max(1, value));
    }

    public static int LastLevelInGalaxy
    {
        get => PlayerPrefs.GetInt(KEY_LAST_LEVEL_IN_GALAXY, 1); // default level 1
        set => PlayerPrefs.SetInt(KEY_LAST_LEVEL_IN_GALAXY, Mathf.Max(1, value));
    }

    public static void SaveNow()
    {
        PlayerPrefs.Save();
    }

    public static void CaptureFromManagers()
    {
        LastGalaxyId = MergeLevelManager.CurrentGalaxyId;
        LastLevelInGalaxy = MergeLevelManager.CurrentLevelInGalaxy;
        SaveNow();
    }

    public static void SetResumePoint(int galaxyId, int levelInGalaxy)
    {
        LastGalaxyId = galaxyId;
        LastLevelInGalaxy = levelInGalaxy;
        SaveNow();
    }

    public static void ResetProgressToStart()
    {
        LastGalaxyId = 1;
        LastLevelInGalaxy = 1;
        SaveNow();

        // If you want it to apply immediately in the current session:
        MergeLevelManager.SetProgress(1, 1);
    }
}