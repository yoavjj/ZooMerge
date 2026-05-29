using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

public class DebugPopup : MonoBehaviour
{
    GameHealthManager healthManager;

    // ✅ GameOver collider toggle
    private Transform gameOverColliderTf;
    [SerializeField]
    private bool gameOverIsDown = false;
    private const float GAMEOVER_Y_DOWN = -3.35f;
    private const float GAMEOVER_Y_UP = 0.69f;

    [System.Obsolete]
    void Start()
    {
        healthManager = FindObjectOfType<GameHealthManager>();
        if (healthManager == null)
        {
            Debug.LogError("[DebugPopup] Could not find GameHealthManager in the scene.");
        }

        // ✅ Find the collider object by name once
        var go = GameObject.Find("GameOver_Collider");
        if (go != null)
        {
            gameOverColliderTf = go.transform;
        }
        else
        {
            Debug.LogWarning("[DebugPopup] Could not find GameObject named 'GameOver_Collider' in the scene.");
        }
    }

    // Call this from your debug button / context menu
    [ContextMenu("Toggle GameOver Collider Y")]
    public void ToggleGameOverColliderY()
    {
        if (gameOverColliderTf == null)
        {
            Debug.LogWarning("[DebugPopup] ToggleGameOverColliderY failed: 'GameOver_Collider' not found.");
            return;
        }

        // Toggle state first (so first press goes DOWN)
        gameOverIsDown = !gameOverIsDown;

        var local = gameOverColliderTf.localPosition;
        local.y = gameOverIsDown ? GAMEOVER_Y_DOWN : GAMEOVER_Y_UP;
        gameOverColliderTf.localPosition = local;

        Debug.Log($"[DebugPopup] GameOver_Collider localY set to {local.y} (down={gameOverIsDown})");
    }

    [ContextMenu("☢️ NUKE ALL SAVE DATA ☢️")]
    public void NukeSaveData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[DEBUG] Local PlayerPrefs wiped clean.");

        if (!string.IsNullOrEmpty(FirebaseInitializer.UserId))
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            DocumentReference docRef = db.Collection("players").Document(FirebaseInitializer.UserId);

            docRef.DeleteAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError($"[DEBUG] Failed to delete cloud save: {task.Exception}");
                else
                    Debug.Log("[DEBUG] Cloud Save completely deleted!");
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