using UnityEngine;

public class SessionManager : MonoBehaviour
{
    private bool hasGameOvered = false;

    private void Start()
    {
        FirebaseInitializer.WaitForFirebase(
            onReady: () =>
            {
                Debug.Log("🔥 Firebase is ready!");

                // ✅ Set starting level
                MergeLevelManager.SetLevel(1); // Starts player at level 1
            },
            onError: err =>
            {
                Debug.LogError($"Firebase init failed: {err}");
            });
    }

    public void ResetSession()
    {
        hasGameOvered = false;
    }

    /// <summary>
    /// Returns the score value for a given merge level in the current stage.
    /// </summary>
    private int GetScoreForLevel(int level)
    {
        var current = MergeLevelManager.GetCurrentLevel();

        if (current?.scores == null)
        {
            Debug.LogWarning("⚠️ Current level has no score data.");
            return FirebaseInitializer.BaseMergeScore;
        }

        var entry = current.scores.Find(s => s.level == level);
        return entry != null ? entry.score : FirebaseInitializer.BaseMergeScore;
    }

    /// <summary>
    /// Prints all score data for the currently active level.
    /// </summary>
    private void PrintCurrentLevelInfo()
    {
        var level = MergeLevelManager.GetCurrentLevel();
        if (level == null)
        {
            Debug.LogWarning("⚠️ No current level data available.");
            return;
        }

        Debug.Log($"📘 Current Level: {level.level}");

        if (level.enemy_data == null || level.enemy_data.Count == 0)
        {
            Debug.LogWarning("No enemies found in current level.");
        }
        else
        {
            foreach (var enemy in level.enemy_data)
            {
                Debug.Log($"   👾 Enemy ID: {enemy.id}, HP: {enemy.health}");
            }
        }

        if (level.scores == null || level.scores.Count == 0)
        {
            Debug.LogWarning("No score entries found for this level.");
            return;
        }

        foreach (var entry in level.scores)
        {
            Debug.Log($"   ➤ Ball Level {entry.level} => Score {entry.score}");
        }
    }

    /// <summary>
    /// Moves to the next level, if available.
    /// </summary>
    public void AdvanceToNextLevel()
    {
        MergeLevelManager.AdvanceLevel();
        var newLevel = MergeLevelManager.GetCurrentLevel();

        Debug.Log($"🚀 Advanced to Level {newLevel.level}");

        if (newLevel.enemy_data != null && newLevel.enemy_data.Count > 0)
        {
            Debug.Log("👾 Enemies in new level:");
            foreach (var enemy in newLevel.enemy_data)
            {
                Debug.Log($"   ➤ Enemy ID: {enemy.id}, HP: {enemy.health}");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No enemies defined for new level.");
        }
    }

    /// <summary>
    /// Resets progression back to Level 1.
    /// </summary>
    public void RestartProgression()
    {
        MergeLevelManager.ResetLevel();
        Debug.Log("🔁 Restarted level progression to Level 1.");
    }
}
