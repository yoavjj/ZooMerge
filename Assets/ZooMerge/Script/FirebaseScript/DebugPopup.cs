using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System;

public class DebugPopup : MonoBehaviour
{
    [Header("Debug Ball Unlock")]
    [SerializeField]
    private BallType debugBallType = BallType.Cat;

    private GameHealthManager healthManager;

    [Header("Debug Fly Collectible")]
    [SerializeField]
    private CollectibleFlyTarget heartFlyTarget;

    [SerializeField]
    private string heartEntryId = "Hearts";

    [SerializeField]
    private int debugHeartAmount = 1;

    [SerializeField]
    private RectTransform overrideSpawnContainer;

    // ✅ GameOver collider toggle
    private Transform gameOverColliderTf;

    [SerializeField]
    private bool gameOverIsDown = false;

    private const float GAMEOVER_Y_DOWN = -3.35f;
    private const float GAMEOVER_Y_UP = 0.69f;

    [ContextMenu("Cooldown Timer -> last 3 seconds")]
    public void Debug_CooldownLast3Seconds()
    {
        CoinCooldown.Debug_SetRemainingSeconds(3);

        Debug.Log(
            "[DebugPopup] Cooldown timer forced " +
            "to 3 seconds remaining."
        );
    }

    [System.Obsolete]
    private void Start()
    {
        healthManager =
            FindObjectOfType<GameHealthManager>();

        if (healthManager == null)
        {
            Debug.LogError(
                "[DebugPopup] Could not find " +
                "GameHealthManager in the scene."
            );
        }

        // ✅ Find the collider object by name once
        GameObject go =
            GameObject.Find("GameOver_Collider");

        if (go != null)
        {
            gameOverColliderTf = go.transform;
        }
        else
        {
            Debug.LogWarning(
                "[DebugPopup] Could not find GameObject " +
                "named 'GameOver_Collider' in the scene."
            );
        }
    }

    private void Update()
    {
#if UNITY_EDITOR

        // Debug shortcut:
        // Z = perform Final Merge.
        if (Input.GetKeyDown(KeyCode.Z))
        {
            FinalMerge();
        }

#endif
    }

    // Call this from your debug button / context menu
    [ContextMenu("Toggle GameOver Collider Y")]
    public void ToggleGameOverColliderY()
    {
        if (gameOverColliderTf == null)
        {
            Debug.LogWarning(
                "[DebugPopup] ToggleGameOverColliderY failed: " +
                "'GameOver_Collider' not found."
            );

            return;
        }

        // Toggle state first
        // so first press goes DOWN.
        gameOverIsDown =
            !gameOverIsDown;

        Vector3 local =
            gameOverColliderTf.localPosition;

        local.y =
            gameOverIsDown
                ? GAMEOVER_Y_DOWN
                : GAMEOVER_Y_UP;

        gameOverColliderTf.localPosition =
            local;

        Debug.Log(
            $"[DebugPopup] GameOver_Collider " +
            $"localY set to {local.y} " +
            $"(down={gameOverIsDown})"
        );
    }

    [ContextMenu("☢️ NUKE ALL SAVE DATA ☢️")]
    public void NukeSaveData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log(
            "[DEBUG] Local PlayerPrefs wiped clean."
        );

        if (!string.IsNullOrEmpty(
                FirebaseInitializer.UserId))
        {
            FirebaseFirestore db =
                FirebaseFirestore.DefaultInstance;

            DocumentReference docRef =
                db.Collection("players")
                    .Document(
                        FirebaseInitializer.UserId
                    );

            docRef.DeleteAsync()
                .ContinueWithOnMainThread(
                    task =>
                    {
                        if (task.IsFaulted)
                        {
                            Debug.LogError(
                                $"[DEBUG] Failed to delete " +
                                $"cloud save: {task.Exception}"
                            );
                        }
                        else
                        {
                            Debug.Log(
                                "[DEBUG] Cloud Save " +
                                "completely deleted!"
                            );
                        }
                    }
                );
        }
        else
        {
            Debug.LogWarning(
                "[DEBUG] Could not delete cloud save: " +
                "User is not logged in yet. " +
                "Play the game first!"
            );
        }
    }

    [System.Obsolete]
    public void RestartLevel()
    {
        // --------------------------------
        // RESET GAME PROGRESS
        // --------------------------------

        PlayerProgress.ResetProgressToStart();

        // --------------------------------
        // RESET SPACESHIP SKINS
        // --------------------------------

        SpaceshipSkinProgress.ResetToDefaultOnly();

        // --------------------------------
        // RESET COIN COOLDOWN FEATURE
        // --------------------------------

        CooldownRewardProgress.ResetSteps();

        PlayerPrefs.DeleteKey("CooldownReward_FeatureUnlocked");
        PlayerPrefs.DeleteKey("CooldownReward_FtueShown");
        PlayerPrefs.DeleteKey("CoinCooldown_EndUtcTicks");

        PlayerPrefs.Save();

        Debug.Log("[DebugPopup] Coin Cooldown feature, progression and timer reset.");

        // --------------------------------
        // RESET HEART FEATURE FTUE
        // --------------------------------

        HeartFeatureProgress.ResetFeature();

        Debug.Log("[DebugPopup] Heart feature FTUE reset.");

        // --------------------------------
        // REFRESH SPACESHIP
        // --------------------------------

        if (SpaceshipSkinController.Instance != null)
        {
            SpaceshipSkinController.Instance.ApplySavedSkin();
        }

        // --------------------------------
        // RESET BALL UNLOCK FEATURE
        // --------------------------------

        BallUnlockFeatureProgress.ResetFeature();

        // --------------------------------
        // RESET CAT UNLOCK
        // --------------------------------

        if (BallUnlockManager.Instance != null)
        {
            BallUnlockManager.Instance.DebugResetUnlock(BallType.Cat);

            Debug.Log("[DebugPopup] Cat unlock reset.");
        }

        // --------------------------------
        // REFRESH MAIN MENU
        // --------------------------------

        MainMenuUI menu = FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);

        if (menu != null)
        {
            menu.RefreshCoinCooldownFeatureVisibility();
            menu.ForceRefreshProgressUIAndCache();
        }

        // --------------------------------
        // REBUILD HERO SELECTION
        // --------------------------------

        BallChoiceMenu ballChoiceMenu = FindFirstObjectByType<BallChoiceMenu>(FindObjectsInactive.Include);

        if (ballChoiceMenu != null)
        {
            ballChoiceMenu.Build();
        }

        // --------------------------------
        // SAVE RESET STATE
        // --------------------------------

        CloudSaveManager.SaveSnapshot(
            incrementMidLevelCompleted: false
        );

        Debug_CooldownLast3Seconds();

        Debug.Log(
            $"[DebugPopup] Progress restarted. " +
            $"Spaceship skins reset to " +
            $"'{SpaceshipSkinProgress.DefaultSkinId}' only. " +
            $"Coin Cooldown feature reset."
        );
    }

    public void RestartInventory()
    {
        // Reset local coins and merge balances.
        GameInventory.Instance.ResetAll();

        // Reset locally purchased animal unlocks.
        BallUnlockManager.Instance?.ResetUnlocks();

        // Reset retry hearts.
        PlayerProgress.NewLevelRetriesRemaining = 1;

        PlayerProgress.SaveNow();
        PlayerProgress.NotifyRetriesChanged();

        // Refresh the animal selection menu immediately.
        BallChoiceMenu menu =
            FindFirstObjectByType<BallChoiceMenu>(
                FindObjectsInactive.Include
            );

        if (menu != null)
        {
            menu.RefreshAll();
        }

        Debug.Log(
            "[DebugPopup] Local inventory reset. " +
            "Saving reset economy to cloud..."
        );

        // Save the reset coins, merges,
        // unlocks, and retries to Firestore.
        CloudSaveManager.SaveEconomyStateImmediate(
            success =>
            {
                if (success)
                {
                    Debug.Log(
                        "[DebugPopup] Inventory reset " +
                        "saved to cloud."
                    );
                }
                else
                {
                    Debug.LogError(
                        "[DebugPopup] Inventory was reset locally, " +
                        "but the cloud reset failed. " +
                        "Old cloud values may return after restarting."
                    );
                }
            }
        );
    }

    public void FinalMerge()
    {
        // Try finding the health manager again
        // in case it was recreated after Start().
        if (healthManager == null)
        {
            healthManager =
                FindFirstObjectByType<GameHealthManager>(
                    FindObjectsInactive.Include
                );
        }

        if (healthManager == null)
        {
            Debug.LogError(
                "[DebugPopup] Cannot perform Final Merge: " +
                "GameHealthManager reference is missing."
            );

            return;
        }

        healthManager.Debug_SetHpNow();

        Debug.Log(
            "[DebugPopup] Final Merge triggered."
        );
    }

    public static void Debug_SetRemainingSeconds(
        int seconds)
    {
        seconds =
            Mathf.Max(
                0,
                seconds
            );

        long end =
            DateTime.UtcNow
                .AddSeconds(seconds)
                .Ticks;

        PlayerPrefs.SetString(
            "CoinCooldown_EndUtcTicks",
            end.ToString()
        );

        PlayerPrefs.Save();
    }

    [ContextMenu("Debug: Fly Heart (+retries)")]
    public void Debug_FlyHeart_AddRetry()
    {
        if (CollectibleFlyService.Instance == null)
        {
            Debug.LogWarning(
                "[DebugPopup] " +
                "CollectibleFlyService.Instance is null."
            );

            return;
        }

        // Auto-find if not assigned.
        if (heartFlyTarget == null)
        {
            heartFlyTarget =
                FindFirstObjectByType<
                    CollectibleFlyTarget
                >(
                    FindObjectsInactive.Include
                );
        }

        if (heartFlyTarget == null)
        {
            Debug.LogWarning(
                "[DebugPopup] heartFlyTarget not assigned " +
                "and none found in scene."
            );

            return;
        }

        CollectibleFlyService.Instance.Fly(
            heartEntryId,
            debugHeartAmount,
            heartFlyTarget,
            overrideSpawnContainer
        );
    }

    [ContextMenu("Debug: Unlock Selected Ball")]
    public void DebugUnlockSelectedBall()
    {
        BallUnlockManager manager =
            BallUnlockManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "[DebugPopup] " +
                "BallUnlockManager.Instance is null."
            );

            return;
        }

        manager.DebugUnlock(
            debugBallType
        );

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
                "[DebugPopup] " +
                "BallUnlockManager.Instance is null."
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
        BallUnlockManager manager =
            BallUnlockManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "[DebugPopup] " +
                "BallUnlockManager.Instance is null."
            );

            return;
        }

        manager.DebugResetUnlock(
            debugBallType
        );

        Debug.Log(
            $"[DebugPopup] Reset {debugBallType} game ownership. " +
            "If this was purchased through Apple IAP, Apple may still " +
            "remember the non-consumable purchase."
        );
    }

    public void Debug_SetRetriesToZero()
    {
        PlayerProgress.NewLevelRetriesRemaining = 0;

        PlayerProgress.SaveNow();
        PlayerProgress.NotifyRetriesChanged();

        CloudSaveManager.SaveEconomyStateImmediate(
            success =>
            {
                if (success)
                {
                    Debug.Log(
                        "[DebugPopup] Retries reset to 0 " +
                        "and saved to cloud."
                    );
                }
                else
                {
                    Debug.LogError(
                        "[DebugPopup] Retries reset locally, " +
                        "but cloud save failed."
                    );
                }
            }
        );
    }
}