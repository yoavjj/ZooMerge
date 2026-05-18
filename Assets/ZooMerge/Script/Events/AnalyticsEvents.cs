using System;
using UnityEngine;
using Firebase.Analytics;
using System.Collections.Generic;

public static class AnalyticsEvents
{
    public static bool Enabled = true;

    static float mainMenuEnterTime = -1f;
    static bool mainMenuActive = false;

    // --- GAMEPLAY TIME TRACKING (per level / per segment) ---

    static bool levelActive = false;

    // --- LEVEL TIMER STATE (exclude background time) ---
    static float levelAccumulated = 0f;      // seconds actually spent in-app
    static float levelResumeTime = -1f;      // realtimeSinceStartup when we last resumed
    static bool levelPausedByApp = false;

    // ---- Helpers ----

    static bool ShouldSend()
    {
        // Gate analytics globally and avoid sending in editor
        if (!Enabled) return false;
        if (Application.isEditor) return false;

        return true;
    }

    static Parameter[] WithDeviceParams(params Parameter[] parameters)
    {
        var list = new List<Parameter>(parameters ?? Array.Empty<Parameter>());

        // Add common device/app params to every event
        list.Add(new Parameter("platform", Application.platform.ToString()));
        list.Add(new Parameter("app_version", Application.version));
        list.Add(new Parameter("unity_version", Application.unityVersion));
        list.Add(new Parameter("device_model", SystemInfo.deviceModel));
        list.Add(new Parameter("os", SystemInfo.operatingSystem));

        return list.ToArray();
    }

    public static void SessionStart()
    {
        // Get the join date from PlayerPrefs (save it during SetInitialUserPersona)
        string joinDateStr = PlayerPrefs.GetString("UserJoinDate", DateTime.UtcNow.ToString("yyyy-MM-dd"));
        DateTime joinDate = DateTime.Parse(joinDateStr);
        int daysSinceJoin = (DateTime.UtcNow - joinDate).Days;

        Log("session_start_custom",
            new Parameter("days_since_join", daysSinceJoin),
            new Parameter("level_at_session_start", MergeLevelManager.CurrentLevelNumber)
        );

        // Explicitly log retention milestones
        if (daysSinceJoin == 1) Log("retention_d1");
        if (daysSinceJoin == 3) Log("retention_d3");
        if (daysSinceJoin == 7) Log("retention_d7");
    }

    public static void GameLoopCompleted()
    {
        Debug.Log("[ANALYTICS] GameLoopCompleted");

        Log("game_loop_completed",
        new Parameter("final_level", MergeLevelManager.CurrentLevelNumber),
        new Parameter("final_galaxy", MergeLevelManager.CurrentGalaxyId)
);
    }

    static float GetLevelDurationSeconds()
    {
        if (!levelActive) return 0f;

        float total = levelAccumulated;

        // If currently running (not paused), add current segment
        if (!levelPausedByApp && levelResumeTime >= 0f)
            total += (Time.realtimeSinceStartup - levelResumeTime);

        return Mathf.Max(0f, total);
    }

    public static void Log(string eventName, params Parameter[] parameters)
    {
#if UNITY_EDITOR
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
        // Existing main menu behavior
        if (paused && mainMenuActive)
            MainMenuExit("app_paused");

        // ✅ Pause/resume LEVEL timer so background time is excluded
        if (!levelActive) return;

        if (paused)
        {
            // Only pause once
            if (!levelPausedByApp)
            {
                // Add time spent since last resume
                if (levelResumeTime >= 0f)
                    levelAccumulated += (Time.realtimeSinceStartup - levelResumeTime);

                levelPausedByApp = true;
                levelResumeTime = -1f;
            }
        }
        else
        {
            // Resume once
            if (levelPausedByApp)
            {
                levelPausedByApp = false;
                levelResumeTime = Time.realtimeSinceStartup;
            }
        }
    }

    public static void OnAppQuit()
    {
        if (mainMenuActive)
            MainMenuExit("app_quit");

        // ✅ If quitting during a level, finalize its timer and end it
        if (levelActive)
        {
            if (!levelPausedByApp && levelResumeTime >= 0f)
                levelAccumulated += (Time.realtimeSinceStartup - levelResumeTime);

            levelPausedByApp = true;
            levelResumeTime = -1f;

            // Treat quit as a real end
            LevelEnd("app_quit");
        }
    }

    // Call when a level session begins (when gameplay becomes active)
    public static void LevelStart(string source = "begin_session")
    {
        levelActive = true;
        levelAccumulated = 0f;
        levelResumeTime = Time.realtimeSinceStartup;
        levelPausedByApp = false;

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
        if (!levelActive)
            return;

        long durationSec = (long)Mathf.RoundToInt(GetLevelDurationSeconds());

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
        if (levelActive)
            levelSec = (long)Mathf.RoundToInt(GetLevelDurationSeconds());

        levelActive = false;

        // optional: reset your new timer state so the next level starts clean
        levelAccumulated = 0f;
        levelResumeTime = -1f;
        levelPausedByApp = false;

        Log("galaxy_level_complete",
            new Parameter("galaxy_id", MergeLevelManager.CurrentGalaxyId),
            new Parameter("galaxy_name", MergeLevelManager.CurrentGalaxyName),
            new Parameter("level_in_galaxy", MergeLevelManager.CurrentLevelInGalaxy),
            new Parameter("level", MergeLevelManager.CurrentLevelNumber),
            new Parameter("level_duration_sec", levelSec)
        );
    }

    // Call when the level ends by losing / quitting / app background
    public static void LevelEnd(string reason)
    {
        long levelSec = 0;
        if (levelActive)
            levelSec = (long)Mathf.RoundToInt(GetLevelDurationSeconds());

        levelActive = false;

        // optional: reset your new timer state
        levelAccumulated = 0f;
        levelResumeTime = -1f;
        levelPausedByApp = false;

        Log("level_end",
            new Parameter("reason", reason),
            new Parameter("galaxy_id", MergeLevelManager.CurrentGalaxyId),
            new Parameter("level_in_galaxy", MergeLevelManager.CurrentLevelInGalaxy),
            new Parameter("level", MergeLevelManager.CurrentLevelNumber),
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

    public static void SetInitialUserPersona(string uid)
    {
        if (Application.isEditor) return;

        // 1. Create a sortable timestamp
        string joinDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // 2. Save locally so we can calculate 'daysSinceJoin' later
        PlayerPrefs.SetString("UserJoinDate", joinDate);
        PlayerPrefs.Save();

        // 3. Set permanent User Properties
        FirebaseAnalytics.SetUserProperty("persona_id", uid);
        FirebaseAnalytics.SetUserProperty("join_date", joinDate);
        FirebaseAnalytics.SetUserProperty("acquisition_platform", Application.platform.ToString());

        // 4. Log a specific "Profile Created" event for funnel tracking
        Log("first_profile_created",
            new Parameter("user_id", uid),
            new Parameter("date", joinDate)
        );
    }
}