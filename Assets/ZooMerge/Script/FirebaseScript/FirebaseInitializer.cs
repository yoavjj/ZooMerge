using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class MergeScoreList
{
    public int enemy_health = 100;
    public List<MergeScoreEntry> scores = new();
}

[System.Serializable]
public class MergeScoreEntry
{
    public int level;
    public int score;
}

public static class FirebaseInitializer
{
    public static bool IsReady { get; private set; } = false;

    private static bool initializing = false;
    private static List<Action> onReadyQueue = new();
    private static List<Action<string>> onErrorQueue = new();

    public static int BaseMergeScore { get; private set; } = 2;
    public static float ScoreMultiplier { get; private set; } = 1.0f;

    public static MergeScoreList MergeScoreData { get; private set; } = new();

    /// <summary>
    /// Ensures Firebase and Remote Config are initialized. Queues callbacks until ready.
    /// </summary>
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

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(async task =>
        {
            if (task.Result != DependencyStatus.Available)
            {
                HandleError($"Firebase dependencies not resolved: {task.Result}");
                return;
            }

            Debug.Log("✅ Firebase dependencies resolved. Initializing Remote Config...");

            await InitializeRemoteConfig();
            IsReady = true;

            foreach (var a in onReadyQueue) a?.Invoke();
            onReadyQueue.Clear();
            onErrorQueue.Clear();

            Debug.Log("🔥 Firebase + Remote Config Ready");
        });
    }

    private static async Task InitializeRemoteConfig()
    {
        var defaults = new Dictionary<string, object>
{
    { "base_merge_score", 2 },
    { "score_multiplier", 1.0f },
    { "merge_scores", "{\"enemy_health\":51,\"scores\":[]}" }  // Default JSON fallback
};

        await FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults);

        // Fetch and activate
        try
        {
            await FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero);
            await FirebaseRemoteConfig.DefaultInstance.ActivateAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Remote Config fetch failed: {e.Message}");
        }

        // Read simple values
        BaseMergeScore = (int)FirebaseRemoteConfig.DefaultInstance.GetValue("base_merge_score").LongValue;
        ScoreMultiplier = (float)FirebaseRemoteConfig.DefaultInstance.GetValue("score_multiplier").DoubleValue;

        // Parse merge score list from JSON
        string json = FirebaseRemoteConfig.DefaultInstance.GetValue("merge_scores").StringValue;
        try
        {
            MergeScoreData = JsonConvert.DeserializeObject<MergeScoreList>(json);
            Debug.Log($"✅ Loaded {MergeScoreData?.scores?.Count ?? 0} merge scores.");
            Debug.Log($"❤️ Enemy health loaded from JSON: {MergeScoreData.enemy_health}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Failed to parse merge_scores JSON: {e.Message}");
            MergeScoreData = new MergeScoreList(); // fallback
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
        try
        {
            await FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero);
            await FirebaseRemoteConfig.DefaultInstance.ActivateAsync();

            // Re-parse JSON
            string json = FirebaseRemoteConfig.DefaultInstance.GetValue("merge_scores").StringValue;
            MergeScoreData = JsonConvert.DeserializeObject<MergeScoreList>(json);
            Debug.Log($"🔁 Refreshed: {MergeScoreData?.scores?.Count ?? 0} merge scores.");

            onComplete?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Remote Config refresh failed: {e.Message}");
            onError?.Invoke(e.Message);
        }
    }
}
