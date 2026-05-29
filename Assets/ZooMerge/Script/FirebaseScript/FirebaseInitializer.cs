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
        // 1. Determine which key and fetch interval to use
        string configKey = "merge_levels";
        TimeSpan fetchInterval = TimeSpan.FromHours(6); // Default for Production

#if UNITY_EDITOR
        configKey = "merge_levels_testing";
        fetchInterval = TimeSpan.Zero; // Instant for Editor testing
        Debug.Log($"🛠️ Editor detected: Using '{configKey}' and instant fetch.");
#endif

        // 2. Set defaults dynamically
        var defaults = new Dictionary<string, object>
    {
        { "base_merge_score", 2 },
        { "score_multiplier", 1.0f },
        { configKey, "{\"galaxies\":[]}" }
    };

        await FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults);

        try
        {
            // 3. Fetch using the dynamic interval
            await FirebaseRemoteConfig.DefaultInstance.FetchAsync(fetchInterval);
            await FirebaseRemoteConfig.DefaultInstance.ActivateAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Remote Config fetch failed: {e.Message}");
        }

        BaseMergeScore = (int)FirebaseRemoteConfig.DefaultInstance.GetValue("base_merge_score").LongValue;
        ScoreMultiplier = (float)FirebaseRemoteConfig.DefaultInstance.GetValue("score_multiplier").DoubleValue;

        // 4. Get the value using the dynamic key
        string json = FirebaseRemoteConfig.DefaultInstance.GetValue(configKey).StringValue;

        try
        {
            MergeScoreData = JsonConvert.DeserializeObject<MergeLevelData>(json);
            MergeLevelManager.Initialize(MergeScoreData);
            Debug.Log($"✅ Loaded {MergeScoreData?.galaxies?.Count ?? 0} galaxies from {configKey}.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Failed to parse {configKey} JSON: {e.Message}");
            MergeScoreData = new MergeLevelData();
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

    public static async void RefreshRemoteConfig(Action onComplete = null, Action<string> onError = null)
    {
        string configKey = "merge_levels";
        TimeSpan fetchInterval = TimeSpan.FromHours(6);

#if UNITY_EDITOR
        configKey = "merge_levels_testing";
        fetchInterval = TimeSpan.Zero;
#endif

        try
        {
            // Use the same dynamic interval logic
            await FirebaseRemoteConfig.DefaultInstance.FetchAsync(fetchInterval);
            await FirebaseRemoteConfig.DefaultInstance.ActivateAsync();

            string json = FirebaseRemoteConfig.DefaultInstance.GetValue(configKey).StringValue;
            MergeScoreData = JsonConvert.DeserializeObject<MergeLevelData>(json);
            MergeLevelManager.Initialize(MergeScoreData);

            Debug.Log($"🔁 Refreshed {configKey} successfully.");
            onComplete?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Remote Config refresh failed: {e.Message}");
            onError?.Invoke(e.Message);
        }
    }
}
