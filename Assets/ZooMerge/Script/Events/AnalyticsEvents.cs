using System;
using UnityEngine;
using Firebase.Analytics;

public static class AnalyticsEvents
{
    public static bool Enabled = true;

    static float mainMenuEnterTime = -1f;
    static bool mainMenuActive = false;

    // --- GAMEPLAY TIME TRACKING (per level / per segment) ---

    static float levelStartTime = -1f;
    static bool levelActive = false;

    // ---- Helpers ----

    public static void SessionStart()
    {
        Log("session_start_custom");
    }

    public static void GameLoopCompleted()
    {
        Debug.Log("[ANALYTICS] GameLoopCompleted");

        Log("game_loop_completed",
        new Parameter("final_level", MergeLevelManager.CurrentLevelNumber),
        new Parameter("final_galaxy", MergeLevelManager.CurrentGalaxyId)
);
    }

    private static bool ShouldSend()
    {
        if (!Enabled) return false;

        // ✅ Never send from Editor
        if (Application.isEditor) return false;

        return true;
    }

    private static Parameter[] WithDeviceParams(Parameter[] parameters)
    {
        // Add a few useful fields for filtering/QA/“investor” reporting
        // Keep names short + consistent.
        var extra = new[]
        {
            new Parameter("platform", Application.platform.ToString()),                 // Android / IPhonePlayer
            new Parameter("device_model", SystemInfo.deviceModel ?? "unknown"),        // iPhone15,3 / Pixel 7 / etc.
            new Parameter("os", SystemInfo.operatingSystem ?? "unknown"),              // iOS 17.x / Android 14 / etc.
            new Parameter("app_version", Application.version ?? "unknown"),            // your app version
        };

        if (parameters == null || parameters.Length == 0)
            return extra;

        var merged = new Parameter[parameters.Length + extra.Length];
        Array.Copy(parameters, merged, parameters.Length);
        Array.Copy(extra, 0, merged, parameters.Length, extra.Length);
        return merged;
    }

    public static void Log(string eventName, params Parameter[] parameters)
    {
#if UNITY_EDITOR
        // ✅ In Editor: don’t send, just log so you can test flows.
        Debug.Log($"[ANALYTICS-EDITOR] {eventName}");
        return;
#else
        if (!ShouldSend()) return;

        FirebaseAnalytics.LogEvent(eventName, WithDeviceParams(parameters));
#endif
    }

    // --- MAIN MENU TIME TRACKING ---

    public static void MainMenuEnter(string source = "start")
    {
        mainMenuEnterTime = Time.realtimeSinceStartup;
        mainMenuActive = true;

        Log("main_menu_enter",
            new Parameter("source", source)
        );
    }

    public static void MainMenuExit(string reason)
    {
        if (!mainMenuActive || mainMenuEnterTime < 0f) return;

        float duration = Time.realtimeSinceStartup - mainMenuEnterTime;
        mainMenuActive = false;
        mainMenuEnterTime = -1f;

        Log("main_menu_exit",
            new Parameter("reason", reason),
            new Parameter("duration_seconds", (long)Mathf.RoundToInt(duration))
        );
    }

    public static void OnAppPaused(bool paused)
    {
        if (paused && mainMenuActive)
            MainMenuExit("app_paused");
    }

    public static void OnAppQuit()
    {
        if (mainMenuActive)
            MainMenuExit("app_quit");
    }

    // Call when a level session begins (when gameplay becomes active)
    public static void LevelStart(string source = "begin_session")
    {
        levelStartTime = Time.realtimeSinceStartup;
        levelActive = true;

        FirebaseAnalytics.SetUserProperty(
        "current_level",
        MergeLevelManager.CurrentLevelNumber.ToString()
        );

        Log("level_start",
            new Parameter("source", source),
            new Parameter("level", MergeLevelManager.CurrentLevelNumber),
            new Parameter("galaxy_id", MergeLevelManager.CurrentGalaxyId),
            new Parameter("level_in_galaxy", MergeLevelManager.CurrentLevelInGalaxy)
        );
    }

    // Call when the player completes a mid-level segment (enemy defeated, moving to next enemy)
    public static void MidLevelComplete(int completedEnemyIndex0Based)
    {
        if (!levelActive || levelStartTime < 0f)
            return;

        long durationSec = (long)Mathf.RoundToInt(
            Time.realtimeSinceStartup - levelStartTime
        );

        Debug.Log($"[ANALYTICS] MidLevelComplete duration={durationSec}s");

        Log("mid_level_complete",
            new Parameter("level_time_sec", durationSec),
            new Parameter("level", MergeLevelManager.CurrentLevelNumber),
            new Parameter("galaxy_id", MergeLevelManager.CurrentGalaxyId),
            new Parameter("level_in_galaxy", MergeLevelManager.CurrentLevelInGalaxy)
        );
    }

    // Call when a full level/galaxy is completed (Win)
    public static void GalaxyLevelComplete()
    {
        long levelSec = 0;
        if (levelActive && levelStartTime >= 0f)
            levelSec = (long)Mathf.RoundToInt(Time.realtimeSinceStartup - levelStartTime);

        levelActive = false;
        levelStartTime = -1f;

        Log("galaxy_level_complete",
            new Parameter("galaxy_id", MergeLevelManager.CurrentGalaxyId),
            new Parameter("galaxy_name", MergeLevelManager.CurrentGalaxyName),
            new Parameter("level_in_galaxy", MergeLevelManager.CurrentLevelInGalaxy),
            new Parameter("global_level", MergeLevelManager.CurrentLevelNumber),
            new Parameter("level_duration_sec", levelSec)
        );
    }

    // Call when the level ends by losing / quitting / app background
    public static void LevelEnd(string reason)
    {
        long levelSec = 0;
        if (levelActive && levelStartTime >= 0f)
            levelSec = (long)Mathf.RoundToInt(Time.realtimeSinceStartup - levelStartTime);

        levelActive = false;
        levelStartTime = -1f;

        Log("level_end",
            new Parameter("reason", reason),
            new Parameter("galaxy_id", MergeLevelManager.CurrentGalaxyId),
            new Parameter("level_in_galaxy", MergeLevelManager.CurrentLevelInGalaxy),
            new Parameter("global_level", MergeLevelManager.CurrentLevelNumber),
            new Parameter("level_duration_sec", levelSec)
        );
    }

    public static void LogRoadmapView(bool isManual, string galaxyId, int globalLevel)
    {
        // We log the source so you can distinguish between "Automatic" (end of galaxy) 
        // and "Manual" (player clicked the button).
        string source = isManual ? "manual_click" : "automatic_flow";

        Log("roadmap_view",
            new Parameter("source", source),
            new Parameter("galaxy_id", galaxyId),
            new Parameter("global_level", globalLevel)
        );
    }
}