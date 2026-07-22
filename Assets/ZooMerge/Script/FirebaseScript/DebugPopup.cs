using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System;

public class DebugPopup : MonoBehaviour
{
    [Header("Debug Ball Unlock")]
    [SerializeField]
    private BallType debugBallType = BallType.Cat;

    GameHealthManager healthManager;

    [Header("Debug Fly Collectible")]
    [SerializeField] private CollectibleFlyTarget heartFlyTarget;   // put this on the TOP BAR HEART ICON
    [SerializeField] private string heartEntryId = "Hearts";         // must match CollectibleFlyService entry id
    [SerializeField] private int debugHeartAmount = 1;              // later you can change 2..6 etc.
    [SerializeField] private RectTransform overrideSpawnContainer;

    // ✅ GameOver collider toggle
    private Transform gameOverColliderTf;
    [SerializeField]
    private bool gameOverIsDown = false;
    private const float GAMEOVER_Y_DOWN = -3.35f;
    private const float GAMEOVER_Y_UP = 0.69f;

    [ContextMenu("Cooldown Timer -> last 3 seconds")]
    public void Debug_CooldownLast3Seconds()
    {
        CoinCooldown.Debug_SetRemainingSeconds(1);
        Debug.Log("[DebugPopup] Cooldown timer forced to 3 seconds remaining.");
    }

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
        BallUnlockManager.Instance?.ResetUnlocks();

        // Reset retries back to 1 (starting amount)
        PlayerProgress.NewLevelRetriesRemaining = 1;
        PlayerProgress.SaveNow();          // if you have this
        PlayerProgress.NotifyRetriesChanged(); // if you have this
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

    public static void Debug_SetRemainingSeconds(int seconds)
    {
        seconds = Mathf.Max(0, seconds);
        long end = DateTime.UtcNow.AddSeconds(seconds).Ticks;
        PlayerPrefs.SetString("CoinCooldown_EndUtcTicks", end.ToString());
        PlayerPrefs.Save();
    }

    [ContextMenu("Debug: Fly Heart (+retries)")]
    public void Debug_FlyHeart_AddRetry()
    {
        if (CollectibleFlyService.Instance == null)
        {
            Debug.LogWarning("[DebugPopup] CollectibleFlyService.Instance is null.");
            return;
        }

        // Auto-find if not assigned
        if (heartFlyTarget == null)
            heartFlyTarget = FindFirstObjectByType<CollectibleFlyTarget>(FindObjectsInactive.Include);

        if (heartFlyTarget == null)
        {
            Debug.LogWarning("[DebugPopup] heartFlyTarget not assigned and none found in scene.");
            return;
        }

        CollectibleFlyService.Instance.Fly(heartEntryId, 1, heartFlyTarget, overrideSpawnContainer);
    }

    [ContextMenu("Debug: Unlock Selected Ball")]
    public void DebugUnlockSelectedBall()
    {
        BallUnlockManager manager =
            BallUnlockManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "[DebugPopup] BallUnlockManager.Instance is null."
            );

            return;
        }

        manager.DebugUnlock(debugBallType);

        Debug.Log(
            $"[DebugPopup] Unlocked {debugBallType}. " +
            $"Saved value: " +
            $"{BallUnlockSave.GetRawSavedValue(debugBallType)}"
        );
    }

    [ContextMenu("Debug: Print Selected Ball Unlock")]
    public void DebugPrintSelectedBallUnlock()
    {
        BallUnlockManager manager =
            BallUnlockManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "[DebugPopup] BallUnlockManager.Instance is null."
            );

            return;
        }

        Debug.Log(
            $"[DebugPopup] Type: {debugBallType}, " +
            $"IsUnlocked: {manager.IsUnlocked(debugBallType)}, " +
            $"Saved value: " +
            $"{BallUnlockSave.GetRawSavedValue(debugBallType)}"
        );
    }

    [ContextMenu("Debug: Reset Selected Ball Purchase")]
    public void DebugResetSelectedBallPurchase()
    {
        BallUnlockSave.ResetUnlock(
            debugBallType
        );

        BallChoiceMenu menu =
            FindFirstObjectByType<BallChoiceMenu>(
                FindObjectsInactive.Include
            );

        if (menu != null)
            menu.RefreshAll();

        Debug.Log(
            $"[DebugPopup] Locally reset purchase for " +
            $"{debugBallType}. Saving reset to cloud..."
        );

        CloudSaveManager.SaveEconomyStateImmediate(
            success =>
            {
                if (success)
                {
                    Debug.Log(
                        $"[DebugPopup] Cloud reset saved for " +
                        $"{debugBallType}. It will remain locked " +
                        "after restarting the app."
                    );
                }
                else
                {
                    Debug.LogError(
                        $"[DebugPopup] Failed to save the cloud reset " +
                        $"for {debugBallType}. The cloud may unlock it " +
                        "again on the next startup."
                    );
                }
            }
        );
    }
}