using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using Solo.MOST_IN_ONE;

public static class CloudSaveManager
{
    private const string PREF_CREATED_AT_SET = "CloudCreatedAtSet";
    private const string PREF_GALAXY_LEVELS_COMPLETED = "TotalGalaxyLevelsCompleted";
    private const string PREF_TOTAL_GAME_LOOPS = "TotalGameLoops";
    private const string PREF_TOTAL_LOSSES = "TotalLosses";
    private const string PREF_TOTAL_MID_LEVELS_COMPLETED = "TotalMidLevelsCompleted";
    private const string PREF_TOTAL_ACTIVE_SECONDS = "TotalActiveSeconds";

    private const string PREF_TOTAL_REWARDED_ADS_COMPLETED =
    "TotalRewardedAdsCompleted";

    private const string PREF_TOTAL_RETRY_PURCHASES_WITH_COINS =
        "TotalRetryPurchasesWithCoins";

    private const string PREF_TOTAL_RETRY_COINS_SPENT =
        "TotalRetryCoinsSpent";

    private static float lastSaveTime = -1f;

    // Prevent double-save spam (loss can fire multiple times)
    private static bool isSaving;
    private static float lastEventSaveTime = -999f;
    private const float EVENT_SAVE_COOLDOWN = 0.5f;

    public static void ForceCloudProgressMap(int galaxyId, int levelInGalaxy, int enemyIndex)
    {
        if (string.IsNullOrEmpty(FirebaseInitializer.UserId))
        {
            Debug.LogWarning("[CloudSave] ForceCloudProgressMap: UserId not ready.");
            return;
        }

        var docRef = FirebaseFirestore.DefaultInstance
            .Collection("players")
            .Document(FirebaseInitializer.UserId);

        var patch = new Dictionary<string, object>
    {
        { "progress", new Dictionary<string, object>
            {
                { "last_played_galaxy", galaxyId },
                { "last_played_level", levelInGalaxy },
                // if you decide to store it in cloud later:
                // { "last_enemy_index", enemyIndex },
            }
        }
    };

        docRef.SetAsync(patch, SetOptions.MergeAll);
    }

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

        PlayerProgress.CaptureFromManagers();

        // Consume retry for the level that was just lost
        PlayerProgress.OnLoss(MergeLevelManager.CurrentGalaxyId, MergeLevelManager.CurrentLevelInGalaxy);

        Debug.Log($"[Retries] Loss. New-level retries remaining: {PlayerProgress.NewLevelRetriesRemaining}");

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
        
        // If app isn't focused, don't accumulate "active playtime"
        if (!Application.isFocused)
        {
            lastSaveTime = now; // avoid a big jump next snapshot
            return;
        }

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

        // Safety: ignore weird spikes/negatives (e.g. device clock quirks or edge cases)
        if (secondsSinceLastSave < 0f) secondsSinceLastSave = 0f;

        // (Optional) cap a single snapshot contribution to something reasonable (like 5 minutes)
        // secondsSinceLastSave = Mathf.Min(secondsSinceLastSave, 300f);

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
        int totalRewardedAdsCompleted = PlayerPrefs.GetInt(PREF_TOTAL_REWARDED_ADS_COMPLETED,0);
        int totalRetryPurchasesWithCoins = PlayerPrefs.GetInt(PREF_TOTAL_RETRY_PURCHASES_WITH_COINS,0);
        int totalRetryCoinsSpent = PlayerPrefs.GetInt(PREF_TOTAL_RETRY_COINS_SPENT,0);

        // --- INVENTORY ---
        int coins = GameInventory.Instance.Get(CurrencyType.Coins);
        Dictionary<string, int> mergedBalls = new Dictionary<string, int>();
        foreach (BallType type in Enum.GetValues(typeof(BallType)))
            mergedBalls[type.ToString()] = GameInventory.Instance.Get(type);

        Dictionary<string, bool> unlockedBalls =
        new Dictionary<string, bool>();

        foreach (BallType type in Enum.GetValues(typeof(BallType)))
        {
            bool unlocked =
                BallUnlockManager.Instance != null &&
                BallUnlockManager.Instance.IsUnlocked(type);

            unlockedBalls[type.ToString()] = unlocked;
        }

        // --- BUILD BASE PAYLOAD ---
        var accountMap = new Dictionary<string, object>
        {
            { "last_login", FieldValue.ServerTimestamp },
            { "days_since_join", daysSinceJoin }
        };

        bool sfxEnabled =
    AudioManager.Instance != null
        ? AudioManager.Instance.IsSfxEnabled
        : PlayerPrefs.GetInt(
            "Audio_SfxMuted",
            0
        ) == 0;

        bool musicEnabled =
            AudioManager.Instance != null
                ? AudioManager.Instance.IsMusicEnabled
                : PlayerPrefs.GetInt(
                    "Audio_MusicMuted",
                    0
                ) == 0;

        bool hapticsEnabled =
            MOST_HapticFeedback.HapticsEnabled;

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
                    { "unlocked_balls", unlockedBalls },

                    { "retries_remaining", PlayerProgress.NewLevelRetriesRemaining },
                    { "retry_cap", PlayerProgress.GetRetryCap() },
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
            },
            { "settings", new Dictionary<string, object>
                {
                    { "sound_fx_enabled", sfxEnabled},
                    { "music_enabled", musicEnabled},
                    { "haptics_enabled", hapticsEnabled}
                }
            },
            { "monetization", new Dictionary<string, object>
                {
                    {"rewarded_ads_completed",totalRewardedAdsCompleted},
                    {"has_completed_rewarded_ad", totalRewardedAdsCompleted > 0},
                    {"retry_purchases_with_coins", totalRetryPurchasesWithCoins},
                    {"retry_coins_spent_total", totalRetryCoinsSpent}
                }
            },
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

    public static void SyncEconomyFromCloud(Action onComplete = null)
    {
        if (string.IsNullOrEmpty(FirebaseInitializer.UserId))
        {
            Debug.LogWarning("[CloudSave] SyncEconomyFromCloud: UserId not ready.");
            onComplete?.Invoke();
            return;
        }

        var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
        var docRef = db.Collection("players").Document(FirebaseInitializer.UserId);

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogWarning($"[CloudSave] SyncEconomyFromCloud failed: {task.Exception}");
                onComplete?.Invoke();
                return;
            }

            var snap = task.Result;
            if (!snap.Exists)
            {
                Debug.Log("[CloudSave] No cloud doc yet, using local economy.");
                onComplete?.Invoke();
                return;
            }

            int cloudCoins = 0;
            int cloudRetries = PlayerProgress.NewLevelRetriesRemaining;
            bool cloudRetriesFound = false;

            Dictionary<string, int> cloudMergedBalls = new Dictionary<string, int>();

            Dictionary<string, bool> cloudUnlockedBalls = new Dictionary<string, bool>();

            if (snap.TryGetValue("economy", out Dictionary<string, object> economyMap))
            {
                if (economyMap.TryGetValue("total_coins_earned", out var cObj))
                    cloudCoins = System.Convert.ToInt32(cObj);

                if (economyMap.TryGetValue("retries_remaining", out var rObj))
                {
                    cloudRetries = Mathf.Clamp(
                        System.Convert.ToInt32(rObj),
                        0,
                        PlayerProgress.GetRetryCap()
                    );

                    cloudRetriesFound = true;
                }

                if (economyMap.TryGetValue("total_merged_balls", out var mbObj) &&
                    mbObj is Dictionary<string, object> mbMap)
                {
                    foreach (var kv in mbMap)
                        cloudMergedBalls[kv.Key] = System.Convert.ToInt32(kv.Value);
                }

                if (economyMap.TryGetValue("unlocked_balls", out object unlockedObject) &&
                    unlockedObject is Dictionary<string, object> unlockedMap)
                        {
                            foreach (
                                KeyValuePair<string, object> pair
                                in unlockedMap)
                            {
                                cloudUnlockedBalls[pair.Key] =
                                    Convert.ToBoolean(pair.Value);
                            }
                        }
            }

            // Apply cloud economy to local inventory.
            GameInventory.Instance.ResetAll();

            if (cloudCoins > 0)
            {
                GameInventory.Instance.Add(
                    CurrencyType.Coins,
                    cloudCoins
                );
            }

            // Restore merged-ball balances.
            foreach (
                KeyValuePair<string, int> pair
                in cloudMergedBalls)
            {
                if (!Enum.TryParse(
                        pair.Key,
                        out BallType ballType))
                {
                    continue;
                }

                if (pair.Value <= 0)
                    continue;

                GameInventory.Instance.Add(
                    ballType,
                    pair.Value
                );
            }

            // Restore unlocked animals only when the cloud field exists.
            // This preserves local unlocks for older cloud saves that do not
            // yet contain the "unlocked_balls" map.
            if (cloudUnlockedBalls.Count > 0)
            {
                BallUnlockManager unlockManager =
                    BallUnlockManager.Instance;

                if (unlockManager != null)
                {
                    unlockManager.ResetUnlocks();

                    foreach (
                        KeyValuePair<string, bool> pair
                        in cloudUnlockedBalls)
                    {
                        if (!Enum.TryParse(
                                pair.Key,
                                out BallType ballType))
                        {
                            Debug.LogWarning(
                                $"[CloudSave] Unknown cloud unlock type: {pair.Key}"
                            );

                            continue;
                        }

                        unlockManager.RestoreUnlockFromCloud(
                            ballType,
                            pair.Value
                        );
                    }
                }
                else
                {
                    Debug.LogWarning(
                        "[CloudSave] Cannot restore unlocked balls because " +
                        "BallUnlockManager.Instance is null."
                    );
                }
            }

            if (cloudRetriesFound)
            {
                PlayerProgress.NewLevelRetriesRemaining =
                    cloudRetries;

                PlayerProgress.SaveNow();
                PlayerProgress.NotifyRetriesChanged();
            }

            Debug.Log(
                $"[CloudSave] Synced economy from cloud: " +
                $"coins={cloudCoins}, " +
                $"balls={cloudMergedBalls.Count}, " +
                $"unlocks={cloudUnlockedBalls.Count}, " +
                $"retries=" +
                $"{(cloudRetriesFound ? cloudRetries.ToString() : "local/default")}"
            );
            onComplete?.Invoke();
        });
    }

    public static void RegisterRewardedAdCompleted()
    {
        int total =
            PlayerPrefs.GetInt(
                PREF_TOTAL_REWARDED_ADS_COMPLETED,
                0
            ) + 1;

        PlayerPrefs.SetInt(
            PREF_TOTAL_REWARDED_ADS_COMPLETED,
            total
        );

        PlayerPrefs.Save();

        Debug.Log(
            $"[CloudSave] Rewarded ads completed: {total}"
        );
    }

    public static void RegisterRetryPurchaseWithCoins(
    int coinsSpent)
    {
        if (coinsSpent <= 0)
            return;

        int purchaseCount =
            PlayerPrefs.GetInt(
                PREF_TOTAL_RETRY_PURCHASES_WITH_COINS,
                0
            ) + 1;

        int totalCoinsSpent =
            PlayerPrefs.GetInt(
                PREF_TOTAL_RETRY_COINS_SPENT,
                0
            ) + coinsSpent;

        PlayerPrefs.SetInt(
            PREF_TOTAL_RETRY_PURCHASES_WITH_COINS,
            purchaseCount
        );

        PlayerPrefs.SetInt(
            PREF_TOTAL_RETRY_COINS_SPENT,
            totalCoinsSpent
        );

        PlayerPrefs.Save();

        Debug.Log(
            "[CloudSave] Retry purchase tracked. " +
            $"Purchases={purchaseCount}, " +
            $"CoinsSpentTotal={totalCoinsSpent}"
        );
    }

    public static void SaveRetriesOnly()
    {
        if (string.IsNullOrEmpty(FirebaseInitializer.UserId))
        {
            Debug.LogWarning("[CloudSave] SaveRetriesOnly: UserId not ready.");
            return;
        }

        var docRef = FirebaseFirestore.DefaultInstance
            .Collection("players")
            .Document(FirebaseInitializer.UserId);

        var patch = new Dictionary<string, object>
    {
        {
            "economy", new Dictionary<string, object>
            {
                { "retries_remaining", PlayerProgress.NewLevelRetriesRemaining },
                { "retry_cap", PlayerProgress.GetRetryCap() }
            }
        }
    };

        docRef.SetAsync(patch, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"[CloudSave] Failed to save retries only: {task.Exception}");
            }
            else
            {
                Debug.Log($"[CloudSave] Saved retries only: {PlayerProgress.NewLevelRetriesRemaining}");
            }
        });
    }

    public static void SaveCoinsOnly()
    {
        if (string.IsNullOrEmpty(FirebaseInitializer.UserId))
        {
            Debug.LogWarning("[CloudSave] SaveCoinsOnly: UserId not ready.");
            return;
        }

        var docRef = FirebaseFirestore.DefaultInstance
            .Collection("players")
            .Document(FirebaseInitializer.UserId);

        int coins = GameInventory.Instance.Get(CurrencyType.Coins);

        var patch = new Dictionary<string, object>
    {
        {
            "economy", new Dictionary<string, object>
            {
                { "total_coins_earned", coins }
            }
        }
    };

        docRef.SetAsync(patch, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"[CloudSave] Failed to save coins only: {task.Exception}");
            }
            else
            {
                Debug.Log($"[CloudSave] Saved coins only: {coins}");
            }
        });
    }

    public static void SyncEconomyNow()
    {
        // Just save a snapshot without incrementing win counters
        SaveSnapshot(incrementMidLevelCompleted: false);
    }

    public static void SaveEconomyStateImmediate(
    Action<bool> onComplete = null)
    {
        if (string.IsNullOrEmpty(FirebaseInitializer.UserId))
        {
            Debug.LogWarning(
                "[CloudSave] SaveEconomyStateImmediate: UserId not ready."
            );

            onComplete?.Invoke(false);
            return;
        }

        int coins =
            GameInventory.Instance.Get(CurrencyType.Coins);

        Dictionary<string, int> mergedBalls =
            new Dictionary<string, int>();

        Dictionary<string, bool> unlockedBalls =
            new Dictionary<string, bool>();

        foreach (
            BallType type
            in Enum.GetValues(typeof(BallType)))
        {
            mergedBalls[type.ToString()] =
                GameInventory.Instance.Get(type);

            bool unlocked =
                BallUnlockManager.Instance != null &&
                BallUnlockManager.Instance.IsUnlocked(type);

            unlockedBalls[type.ToString()] =
                unlocked;
        }

        DocumentReference docRef =
            FirebaseFirestore.DefaultInstance
                .Collection("players")
                .Document(FirebaseInitializer.UserId);

        Dictionary<string, object> patch =
            new Dictionary<string, object>
            {
            {
                "economy",
                new Dictionary<string, object>
                {
                    {
                        "total_coins_earned",
                        coins
                    },
                    {
                        "total_merged_balls",
                        mergedBalls
                    },
                    {
                        "unlocked_balls",
                        unlockedBalls
                    },
                    {
                        "retries_remaining",
                        PlayerProgress.NewLevelRetriesRemaining
                    },
                    {
                        "retry_cap",
                        PlayerProgress.GetRetryCap()
                    }
                }
            }
            };

        docRef.SetAsync(
            patch,
            SetOptions.MergeAll
        ).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(
                    "[CloudSave] Immediate economy save failed: " +
                    task.Exception
                );

                onComplete?.Invoke(false);
                return;
            }

            Debug.Log(
                "[CloudSave] Immediate economy state saved. " +
                $"Coins={coins}, " +
                $"Merges={mergedBalls.Count}, " +
                $"Unlocks={unlockedBalls.Count}"
            );

            onComplete?.Invoke(true);
        });
    }

    public static void OnAppPaused(bool paused)
    {
        // We want to ignore time while backgrounded.
        // So we reset the baseline when coming BACK (resume).
        if (!paused)
            lastSaveTime = Time.realtimeSinceStartup;
    }

    public static void OnAppQuit()
    {
        // Prevent any final save from counting time after the app is already leaving.
        lastSaveTime = Time.realtimeSinceStartup;
    }
}