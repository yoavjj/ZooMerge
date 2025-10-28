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

    private int GetScoreForLevel(int level)
    {
        var entry = FirebaseInitializer.MergeScoreData?.scores?.Find(s => s.level == level);
        return entry != null ? entry.score : FirebaseInitializer.BaseMergeScore;
    }

    private void PrintMergeScores()
    {
        if (FirebaseInitializer.MergeScoreData?.scores == null)
        {
            Debug.LogWarning("No merge scores data available.");
            return;
        }

        foreach (var entry in FirebaseInitializer.MergeScoreData.scores)
        {
            Debug.Log($"Level {entry.level} => Score {entry.score}");
        }
    }
}
