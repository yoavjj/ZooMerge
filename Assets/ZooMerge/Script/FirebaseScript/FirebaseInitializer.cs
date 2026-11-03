using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using Firebase.Analytics;
using Firebase.Auth;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public class MergeLevelData
{
    public List<GalaxyData> galaxies = new();
}

[Serializable]
public class GalaxyData
{
    public int galaxyId;
    public string name;
    public List<MergeLevel> levels = new();
}

[Serializable]
public class MergeLevel
{
    // inside a galaxy (1..N)
    public int index;
    public int stageId;

    public LevelCompletionReward completionReward = new();

    public List<EnemyData> enemy_data;
    public List<MergeScoreEntry> scores;
}

[Serializable]
public class EnemyData
{
    public int id;
    public int health;
    public int coins;
}

[Serializable]
public class MergeScoreEntry
{
    public int level;  // merge level (ball level) for scoring
    public int score;
}

public static class FirebaseInitializer
{
    public static bool IsReady { get; private set; } = false;
    public static bool BootComplete { get; set; } = false;

    private static bool initializing = false;
    private static List<Action> onReadyQueue = new();
    private static List<Action<string>> onErrorQueue = new();

    public static int BaseMergeScore { get; private set; } = 2;
    public static float ScoreMultiplier { get; private set; } = 1.0f;

    public static MergeLevelData MergeScoreData { get; private set; } = new();

    public static string UserId { get; private set; }

    public static BallUnlockRemoteConfigData
    BallUnlockConfig {get;private set;} = new();

    public static RetryRefillPricingRemoteConfigData
    RetryRefillPricingConfig {get;private set;} = new();

    public static void WaitForFirebase(Action onReady, Action<string> onError = null)
    {
        if (IsReady)
        {
            onReady?.Invoke();
            return;
        }

        onReadyQueue.Add(onReady);
        if (onError != null) onErrorQueue.Add(onError);

        if (!initializing)
            InitializeFirebaseInternal();
    }

    private static void InitializeFirebaseInternal()
    {
        initializing = true;
        BootComplete = false;

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(async task =>
        {
            if (task.Result != DependencyStatus.Available)
            {
                HandleError($"Firebase dependencies not resolved: {task.Result}");
                return;
            }

            // --- NEW: ANONYMOUS AUTH ---
            try
            {
                var auth = FirebaseAuth.DefaultInstance;

                // ✅ Reuse existing session if available
                if (auth.CurrentUser != null)
                {
                    UserId = auth.CurrentUser.UserId;
                }
                else
                {
                    var result = await auth.SignInAnonymouslyAsync();
                    UserId = result.User.UserId;

                    // only “new persona” logic belongs here (only when we actually created a new user)
                    if (result.User.Metadata.CreationTimestamp == result.User.Metadata.LastSignInTimestamp)
                        AnalyticsEvents.SetInitialUserPersona(UserId);
                }

                // Cache for display/debug if you want (does not restore auth by itself)
                PlayerPrefs.SetString("CachedUserId", UserId);
                PlayerPrefs.Save();

                // Set the ID in Analytics so all events are linked to this persona
                FirebaseAnalytics.SetUserId(UserId);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Auth failed, but continuing: {e.Message}");
            }
            // ---------------------------

            await InitializeRemoteConfig();
            IsReady = true;

            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

            AnalyticsEvents.SessionStart();

            foreach (var a in onReadyQueue) a?.Invoke();
            onReadyQueue.Clear();
            onErrorQueue.Clear();
        });
    }

    private static async Task InitializeRemoteConfig()
    {
        string configKey =
            "merge_levels";

        string ballUnlockConfigKey =
            "ball_unlock_catalog";

        string retryPricingConfigKey =
            "retry_refill_pricing";

        TimeSpan fetchInterval =
            TimeSpan.FromHours(6);

#if UNITY_EDITOR

        configKey =
            "merge_levels_testing";

        ballUnlockConfigKey =
            "ball_unlock_catalog_testing";

        retryPricingConfigKey =
"retry_refill_pricing_testing";

        fetchInterval =
            TimeSpan.Zero;

        Debug.Log(
            $"🛠️ Editor detected: " +
            $"Using '{configKey}' and " +
            $"'{ballUnlockConfigKey}' and " +
            $"'{retryPricingConfigKey}'."
        );

#endif

        var defaults =
            new Dictionary<string, object>
            {
            {
                "base_merge_score",
                2
            },
            {
                "score_multiplier",
                1.0f
            },
            {
                configKey,
                "{\"galaxies\":[]}"
            },
            {
                ballUnlockConfigKey,
                "{\"definitions\":[]}"
            },
            {
                retryPricingConfigKey,
                "{\"costs\":[],\"overflowStep\":3}"
            }
            };

        FirebaseRemoteConfig remoteConfig =
            FirebaseRemoteConfig.DefaultInstance;

        await remoteConfig.SetDefaultsAsync(
            defaults
        );

        try
        {
            await remoteConfig.FetchAsync(
                fetchInterval
            );

            await remoteConfig.ActivateAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                $"⚠️ Remote Config fetch failed: " +
                $"{e.Message}"
            );
        }

        BaseMergeScore =
            (int)remoteConfig
                .GetValue("base_merge_score")
                .LongValue;

        ScoreMultiplier =
            (float)remoteConfig
                .GetValue("score_multiplier")
                .DoubleValue;

        string json =
            remoteConfig
                .GetValue(configKey)
                .StringValue;

        try
        {
            MergeScoreData =
                JsonConvert.DeserializeObject<
                    MergeLevelData
                >(json);

            MergeLevelManager.Initialize(
                MergeScoreData
            );

            Debug.Log(
                $"✅ Loaded " +
                $"{MergeScoreData?.galaxies?.Count ?? 0} " +
                $"galaxies from {configKey}."
            );
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                $"⚠️ Failed to parse " +
                $"{configKey} JSON: {e.Message}"
            );

            MergeScoreData =
                new MergeLevelData();
        }

        // Load ball unlock requirements.
        LoadBallUnlockRemoteConfig(
            remoteConfig,
            ballUnlockConfigKey
        );

        LoadRetryRefillPricingRemoteConfig(
        remoteConfig,
        retryPricingConfigKey
        );
    }

    private static void LoadRetryRefillPricingRemoteConfig(
    FirebaseRemoteConfig remoteConfig,
    string configKey)
    {
        string json =
            remoteConfig
                .GetValue(configKey)
                .StringValue;

        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            RetryRefillPricingRemoteConfigData data =
                JsonConvert.DeserializeObject<
                    RetryRefillPricingRemoteConfigData
                >(json);

            if (data == null ||
                data.costs == null ||
                data.costs.Length == 0)
            {
                Debug.LogWarning(
                    $"[RemoteConfig] Retry pricing " +
                    $"'{configKey}' is empty. " +
                    $"Using local ScriptableObject values."
                );

                return;
            }

            RetryRefillPricingConfig =
                data;

            Debug.Log(
                $"[RemoteConfig] Retry pricing loaded. " +
                $"Costs={data.costs.Length}, " +
                $"OverflowStep={data.overflowStep}"
            );
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                $"[RemoteConfig] Failed to parse " +
                $"'{configKey}': {e.Message}"
            );
        }
    }

    private static void HandleError(string msg)
    {
        Debug.LogError(msg);
        IsReady = false;
        foreach (var e in onErrorQueue)
            e?.Invoke(msg);
        onErrorQueue.Clear();
    }

    public static async void RefreshRemoteConfig(
        Action onComplete = null,
        Action<string> onError = null)
    {
        string configKey =
            "merge_levels";

        string ballUnlockConfigKey =
            "ball_unlock_catalog";

        string retryPricingConfigKey =
            "retry_refill_pricing";

#if UNITY_EDITOR

        configKey =
            "merge_levels_testing";

        ballUnlockConfigKey =
            "ball_unlock_catalog_testing";
        
        retryPricingConfigKey =
"retry_refill_pricing_testing";

#endif

        try
        {
            FirebaseRemoteConfig remoteConfig =
                FirebaseRemoteConfig.DefaultInstance;

            Debug.Log(
                $"[RemoteConfig] Force refreshing " +
                $"'{configKey}' and " +
                $"'{ballUnlockConfigKey}' and " +
                $"'{retryPricingConfigKey}'..."
            );

            await remoteConfig.FetchAsync(
                TimeSpan.Zero
            );

            bool activated =
                await remoteConfig.ActivateAsync();

            BaseMergeScore =
                (int)remoteConfig
                    .GetValue("base_merge_score")
                    .LongValue;

            ScoreMultiplier =
                (float)remoteConfig
                    .GetValue("score_multiplier")
                    .DoubleValue;

            string json =
                remoteConfig
                    .GetValue(configKey)
                    .StringValue;

            MergeScoreData =
                JsonConvert.DeserializeObject<
                    MergeLevelData
                >(json);

            MergeLevelManager.Initialize(
                MergeScoreData
            );

            LoadBallUnlockRemoteConfig(
                remoteConfig,
                ballUnlockConfigKey
            );

            LoadRetryRefillPricingRemoteConfig(
                remoteConfig,
                retryPricingConfigKey
            );

            Debug.Log(
                $"🔁 Remote Config refreshed. " +
                $"Activated={activated}, " +
                $"Galaxies=" +
                $"{MergeScoreData?.galaxies?.Count ?? 0}, " +
                $"BallUnlocks=" +
                $"{BallUnlockConfig?.definitions?.Count ?? 0}"
            );

            onComplete?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                $"⚠️ Remote Config refresh failed: " +
                $"{e.Message}"
            );

            onError?.Invoke(
                e.Message
            );
        }
    }

    private static void LoadBallUnlockRemoteConfig(
        FirebaseRemoteConfig remoteConfig,
        string configKey)
    {
        string json =
            remoteConfig
                .GetValue(configKey)
                .StringValue;

        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            BallUnlockRemoteConfigData data =
                JsonConvert.DeserializeObject<
                    BallUnlockRemoteConfigData
                >(json);

            if (data == null)
                return;

            BallUnlockConfig =
                data;

            if (BallUnlockManager.Instance != null)
            {
                BallUnlockManager.Instance
                    .ApplyRemoteConfig(
                        data
                    );
            }

            Debug.Log(
                $"[RemoteConfig] Ball unlock config loaded. " +
                $"Definitions=" +
                $"{data.definitions?.Count ?? 0}"
            );
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                $"[RemoteConfig] Failed to parse " +
                $"'{configKey}': {e.Message}"
            );
        }
    }
}
