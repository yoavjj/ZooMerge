using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

public static class CloudSaveManager
{
    private const string PREF_CREATED_AT_SET = "CloudCreatedAtSet";
    private const string PREF_GALAXY_LEVELS_COMPLETED = "TotalGalaxyLevelsCompleted";
    private const string PREF_TOTAL_GAME_LOOPS = "TotalGameLoops";
    private const string PREF_TOTAL_LOSSES = "TotalLosses";
    private const string PREF_TOTAL_MID_LEVELS_COMPLETED = "TotalMidLevelsCompleted";
    private const string PREF_TOTAL_ACTIVE_SECONDS = "TotalActiveSeconds";

    private static float lastSaveTime = -1f;

    // Prevent double-save spam (loss can fire multiple times)
    private static bool isSaving;
    private static float lastEventSaveTime = -999f;
    private const float EVENT_SAVE_COOLDOWN = 0.5f;

    public static void StartPlayTimer()
    {
        lastSaveTime = Time.realtimeSinceStartup;
        Debug.Log("[CloudSave] Play timer started!");
    }

    // Call this from "loop complete" event
    public static void AddGameLoop()
    {
        int loops = PlayerPrefs.GetInt(PREF_TOTAL_GAME_LOOPS, 0) + 1;
        PlayerPrefs.SetInt(PREF_TOTAL_GAME_LOOPS, loops);
        SaveSnapshot(incrementMidLevelCompleted: false); // ✅ don't increment mid levels here
    }

    // Call this after AnalyticsEvents.GalaxyLevelComplete()
    public static void AddGalaxyLevelComplete()
    {
        int total = PlayerPrefs.GetInt(PREF_GALAXY_LEVELS_COMPLETED, 0) + 1;
        PlayerPrefs.SetInt(PREF_GALAXY_LEVELS_COMPLETED, total);
        PlayerPrefs.Save();
    }

    public static void AddLoss(BallEventManager.GameOverReason reason)
    {
        int losses = PlayerPrefs.GetInt(PREF_TOTAL_LOSSES, 0) + 1;
        PlayerPrefs.SetInt(PREF_TOTAL_LOSSES, losses);

        // Optional: store last reason for debugging/analytics
        PlayerPrefs.SetString("LastGameOverReason", reason.ToString());

        SaveSnapshot(incrementMidLevelCompleted: false); // ✅ don't increment mid levels on loss
    }

    // The one true save that never increments "completed" counters
    public static void SaveSnapshot(bool incrementMidLevelCompleted)
    {
        if (string.IsNullOrEmpty(FirebaseInitializer.UserId))
        {
            Debug.LogWarning("[CloudSave] UserId not ready, skipping save.");
            return;
        }

        // --- DEBOUNCE & SAFETY ---
        float now = Time.realtimeSinceStartup;
        if (isSaving) return;
        if (now - lastEventSaveTime < EVENT_SAVE_COOLDOWN) return;
        lastEventSaveTime = now;

        // ✅ Increment ONLY when the caller says this session was a WIN
        if (incrementMidLevelCompleted)
        {
            int mid = PlayerPrefs.GetInt(PREF_TOTAL_MID_LEVELS_COMPLETED, 0) + 1;
            PlayerPrefs.SetInt(PREF_TOTAL_MID_LEVELS_COMPLETED, mid);
            PlayerPrefs.Save();
        }

        // --- TIMER ---
        if (lastSaveTime < 0f) lastSaveTime = now;
        float secondsSinceLastSave = now - lastSaveTime;
        lastSaveTime = now;

        float totalActiveSeconds = PlayerPrefs.GetFloat(PREF_TOTAL_ACTIVE_SECONDS, 0f) + secondsSinceLastSave;
        PlayerPrefs.SetFloat(PREF_TOTAL_ACTIVE_SECONDS, totalActiveSeconds);

        TimeSpan timeSpan = TimeSpan.FromSeconds(totalActiveSeconds);
        string formattedTime = string.Format("{0:D2}h {1:D2}m {2:D2}s",
            timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

        // --- RETENTION DATA ---
        string joinDateStr = PlayerPrefs.GetString("UserJoinDate", DateTime.UtcNow.ToString("yyyy-MM-dd"));
        int daysSinceJoin = 0;

        if (DateTime.TryParse(joinDateStr, out DateTime joinDate))
        {
            daysSinceJoin = (DateTime.UtcNow.Date - joinDate.Date).Days;
        }

        // --- TOTALS ---
        int totalLoops = PlayerPrefs.GetInt(PREF_TOTAL_GAME_LOOPS, 0);
        int totalGalaxyLevelsCompleted = PlayerPrefs.GetInt(PREF_GALAXY_LEVELS_COMPLETED, 0);
        int totalMidLevels = PlayerPrefs.GetInt(PREF_TOTAL_MID_LEVELS_COMPLETED, 0);
        int totalLosses = PlayerPrefs.GetInt(PREF_TOTAL_LOSSES, 0);
        string lastGameOverReason = PlayerPrefs.GetString("LastGameOverReason", "None");

        // --- INVENTORY ---
        int coins = GameInventory.Instance.Get(CurrencyType.Coins);
        Dictionary<string, int> mergedBalls = new Dictionary<string, int>();
        foreach (BallType type in Enum.GetValues(typeof(BallType)))
            mergedBalls[type.ToString()] = GameInventory.Instance.Get(type);

        // --- BUILD BASE PAYLOAD ---
        var accountMap = new Dictionary<string, object>
        {
            { "last_login", FieldValue.ServerTimestamp },
            { "days_since_join", daysSinceJoin }
        };

        Dictionary<string, object> playerData = new Dictionary<string, object>
        {
            { "account", accountMap },

            { "progress", new Dictionary<string, object>
                {
                    { "last_played_galaxy", PlayerProgress.LastGalaxyId },
                    { "last_played_level",  PlayerProgress.LastLevelInGalaxy },
                }
            },

            { "playtime", new Dictionary<string, object>
                {
                    { "total_active_playtime", formattedTime },
                    { "total_active_seconds_raw", Mathf.RoundToInt(totalActiveSeconds) },
                }
            },

            { "economy", new Dictionary<string, object>
                {
                    { "total_coins_earned", coins },
                    { "total_merged_balls", mergedBalls },
                }
            },

            { "totals", new Dictionary<string, object>
                {
                    { "total_mid_levels_completed", totalMidLevels },
                    { "total_galaxy_levels_completed", totalGalaxyLevelsCompleted },
                    { "total_losses", totalLosses },
                    { "last_game_over_reason", lastGameOverReason },
                    { "total_game_loops", totalLoops }
                }
            }
        };

        // --- DECISION POINT: NEW VS RETURNING USER ---
        bool createdAtAlreadySet = PlayerPrefs.GetInt(PREF_CREATED_AT_SET, 0) == 1;

        if (!createdAtAlreadySet)
        {
            // Brand new user: Assign them a global number and a permanent timestamp
            RunNewUserTransaction(playerData, joinDateStr);
        }
        else
        {
            // Returning user: Just update their standard progress
            SyncReturningUser(playerData, daysSinceJoin);
        }
    }

    // 2. Helper Method for First-Time Users Only
    private static void RunNewUserTransaction(Dictionary<string, object> playerData, string joinDateStr)
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference counterRef = db.Collection("metadata").Document("global_counters");
        DocumentReference userRef = db.Collection("players").Document(FirebaseInitializer.UserId);

        isSaving = true;

        db.RunTransactionAsync(transaction =>
        {
            return transaction.GetSnapshotAsync(counterRef).ContinueWith(task =>
            {
                long currentTotal = 0;
                if (task.Result.Exists && task.Result.ContainsField("total_users"))
                {
                    currentTotal = task.Result.GetValue<long>("total_users");
                }

                // Increment the global counter
                long myUserNumber = currentTotal + 1;
                Dictionary<string, object> counterData = new Dictionary<string, object>
                {
                    { "total_users", myUserNumber }
                };
                transaction.Set(counterRef, counterData, SetOptions.MergeAll);

                // Add the one-time new user fields into the account map
                var accountMap = (Dictionary<string, object>)playerData["account"];
                accountMap["global_user_number"] = myUserNumber;
                accountMap["first_login_time"] = FieldValue.ServerTimestamp;

                // Save the player's document
                transaction.Set(userRef, playerData, SetOptions.MergeAll);

                return myUserNumber;
            });
        }).ContinueWithOnMainThread(task =>
        {
            isSaving = false;
            if (task.IsFaulted)
            {
                Debug.LogError($"[CloudSave] New User Transaction failed: {task.Exception}");
            }
            else
            {
                PlayerPrefs.SetInt(PREF_CREATED_AT_SET, 1);
                PlayerPrefs.Save();
                Debug.Log($"[CloudSave] Welcome! Global Account #{task.Result} created.");
            }
        });
    }

    // 3. Helper Method for Returning Users
    private static void SyncReturningUser(Dictionary<string, object> playerData, int daysSinceJoin)
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("players").Document(FirebaseInitializer.UserId);

        isSaving = true;

        docRef.SetAsync(playerData, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
        {
            isSaving = false;

            if (task.IsFaulted)
            {
                Debug.LogError($"[CloudSave] Failed to save: {task.Exception}");
            }
            else
            {
                Debug.Log($"[CloudSave] Saved snapshot. User Day: {daysSinceJoin}");
            }
        });
    }

    public static void SyncProgressFromCloud(Action onComplete = null)
    {
        if (string.IsNullOrEmpty(FirebaseInitializer.UserId))
        {
            Debug.LogWarning("[CloudSave] SyncProgressFromCloud: UserId not ready.");
            onComplete?.Invoke();
            return;
        }

        var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
        var docRef = db.Collection("players").Document(FirebaseInitializer.UserId);

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogWarning($"[CloudSave] SyncProgressFromCloud failed: {task.Exception}");
                onComplete?.Invoke();
                return;
            }

            var snap = task.Result;
            if (!snap.Exists)
            {
                // New user → keep defaults
                Debug.Log("[CloudSave] No cloud doc yet, using local defaults.");
                onComplete?.Invoke();
                return;
            }

            // Expecting structure like: progress.last_played_galaxy / progress.last_played_level
            int cloudGalaxy = PlayerProgress.LastGalaxyId;
            int cloudLevel = PlayerProgress.LastLevelInGalaxy;

            if (snap.TryGetValue("progress", out Dictionary<string, object> progressMap))
            {
                if (progressMap.TryGetValue("last_played_galaxy", out var gObj))
                    cloudGalaxy = Convert.ToInt32(gObj);

                if (progressMap.TryGetValue("last_played_level", out var lObj))
                    cloudLevel = Convert.ToInt32(lObj);
            }

            // Write to PlayerPrefs
            PlayerProgress.LastGalaxyId = cloudGalaxy;
            PlayerProgress.LastLevelInGalaxy = cloudLevel;
            PlayerProgress.SaveNow();

            // Apply into MergeLevelManager
            MergeLevelManager.SetProgressByIds(cloudGalaxy, cloudLevel);

            Debug.Log($"[CloudSave] Synced progress from cloud: Galaxy {cloudGalaxy}, Level {cloudLevel}");
            onComplete?.Invoke();
        });
    }
}