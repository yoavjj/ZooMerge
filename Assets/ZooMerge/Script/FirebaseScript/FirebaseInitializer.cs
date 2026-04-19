using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using Firebase.RemoteConfig;
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

    private static bool initializing = false;
    private static List<Action> onReadyQueue = new();
    private static List<Action<string>> onErrorQueue = new();

    public static int BaseMergeScore { get; private set; } = 2;
    public static float ScoreMultiplier { get; private set; } = 1.0f;

    public static MergeLevelData MergeScoreData { get; private set; } = new();

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
        { "merge_levels", "{\"galaxies\":[]}" } // ✅ new default
    };

        await FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults);

        try
        {
            await FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero);
            await FirebaseRemoteConfig.DefaultInstance.ActivateAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Remote Config fetch failed: {e.Message}");
        }

        BaseMergeScore = (int)FirebaseRemoteConfig.DefaultInstance.GetValue("base_merge_score").LongValue;
        ScoreMultiplier = (float)FirebaseRemoteConfig.DefaultInstance.GetValue("score_multiplier").DoubleValue;

        string json = FirebaseRemoteConfig.DefaultInstance.GetValue("merge_levels").StringValue;
        try
        {
            MergeScoreData = JsonConvert.DeserializeObject<MergeLevelData>(json);
            MergeLevelManager.Initialize(MergeScoreData);

            Debug.Log($"✅ Loaded {MergeScoreData?.galaxies?.Count ?? 0} galaxies.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Failed to parse merge_scores JSON: {e.Message}");
            MergeScoreData = new MergeLevelData(); // fallback
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

            string json = FirebaseRemoteConfig.DefaultInstance.GetValue("merge_scores").StringValue;
            MergeScoreData = JsonConvert.DeserializeObject<MergeLevelData>(json);
            MergeLevelManager.Initialize(MergeScoreData);

            Debug.Log($"🔁 Refreshed: {MergeScoreData?.galaxies?.Count ?? 0} galaxies.");

            onComplete?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Remote Config refresh failed: {e.Message}");
            onError?.Invoke(e.Message);
        }
    }
}
