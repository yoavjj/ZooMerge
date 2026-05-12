using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

public class DebugPopup : MonoBehaviour
{
    GameHealthManager healthManager;

    [System.Obsolete]
    void Start()
    {
        healthManager = FindObjectOfType<GameHealthManager>();
        if (healthManager == null)
        {
            Debug.LogError("[DebugPopup] Could not find GameHealthManager in the scene.");
        }
    }

    [ContextMenu("☢️ NUKE ALL SAVE DATA ☢️")]
    public void NukeSaveData()
    {
        // 1. Wipe the local phone/editor memory
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[DEBUG] Local PlayerPrefs wiped clean.");

        // 2. Wipe the Cloud Database
        if (!string.IsNullOrEmpty(FirebaseInitializer.UserId))
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            DocumentReference docRef = db.Collection("players").Document(FirebaseInitializer.UserId);

            docRef.DeleteAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"[DEBUG] Failed to delete cloud save: {task.Exception}");
                }
                else
                {
                    Debug.Log("[DEBUG] Cloud Save completely deleted!");
                }
            });
        }
        else
        {
            Debug.LogWarning("[DEBUG] Could not delete cloud save: User is not logged in yet. Play the game first!");
        }
    }

    [System.Obsolete]
    public void RestartLevel()
    {
        PlayerProgress.ResetProgressToStart();

        // ✅ Make Main Menu update instantly
        var menu = FindObjectOfType<MainMenuUI>();
        if (menu != null)
            menu.ForceRefreshProgressUIAndCache();
    }

    public void RestartInventory()
    {
        GameInventory.Instance.ResetAll();
    } 

    public void FinalMerge()
    {
        if (healthManager == null)
        {
            Debug.LogError("[DebugPopup] Cannot perform Final Merge: GameHealthManager reference is missing.");
            return;
        }
       
       healthManager.Debug_SetHpNow();
    }
}