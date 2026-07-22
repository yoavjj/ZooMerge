using System;
using UnityEngine;

public class BallUnlockManager : MonoBehaviour
{
    public static BallUnlockManager Instance { get; private set; }

    [SerializeField]
    private BallUnlockCatalogSO unlockCatalog;

    public event Action<BallType> OnBallUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool IsUnlocked(BallType type)
    {
        BallUnlockCatalogSO.UnlockDefinition definition =
            GetDefinition(type);

        if (definition == null)
            return false;

        if (definition.unlockedByDefault)
            return true;

        return BallUnlockSave.IsUnlocked(type);
    }

    public bool CanUnlock(
        BallType type,
        out string reason)
    {
        reason = string.Empty;

        BallUnlockCatalogSO.UnlockDefinition definition =
            GetDefinition(type);

        if (definition == null)
        {
            reason =
                $"No unlock definition exists for {type}.";

            return false;
        }

        if (IsUnlocked(type))
        {
            reason =
                $"{type} is already unlocked.";

            return false;
        }

        int currentCoins =
            GameInventory.Instance.Get(
                CurrencyType.Coins
            );

        if (currentCoins < definition.coinCost)
        {
            reason =
                $"Not enough coins for {type}. " +
                $"Need {definition.coinCost}, " +
                $"have {currentCoins}.";

            return false;
        }

        if (definition.mergeRequirements != null)
        {
            foreach (
                BallUnlockCatalogSO.MergeRequirement requirement
                in definition.mergeRequirements)
            {
                if (requirement == null)
                    continue;

                int currentAmount =
                    GameInventory.Instance.Get(
                        requirement.type
                    );

                if (currentAmount <
                    requirement.requiredAmount)
                {
                    reason =
                        $"Not enough {requirement.type} merges " +
                        $"for {type}. " +
                        $"Need {requirement.requiredAmount}, " +
                        $"have {currentAmount}.";

                    return false;
                }
            }
        }

        return true;
    }

    public bool TryUnlock(
        BallType type,
        out string reason)
    {
        reason = string.Empty;

        if (IsUnlocked(type))
        {
            reason = $"{type} is already unlocked.";
            return false;
        }

        BallUnlockCatalogSO.UnlockDefinition definition =
            GetDefinition(type);

        if (definition == null)
        {
            reason =
                $"No unlock definition exists for {type}.";

            return false;
        }

        // Validate every cost before deducting anything.
        if (!CanUnlock(type, out reason))
            return false;

        // Spend coins without notifying yet.
        if (definition.coinCost > 0)
        {
            bool coinsSpent =
                GameInventory.Instance.Spend(
                    CurrencyType.Coins,
                    definition.coinCost,
                    notify: false
                );

            if (!coinsSpent)
            {
                reason =
                    $"Could not spend coins to unlock {type}.";

                return false;
            }
        }

        // Merge requirements are now consumed as part of the purchase.
        if (definition.mergeRequirements != null)
        {
            foreach (
                BallUnlockCatalogSO.MergeRequirement requirement
                in definition.mergeRequirements)
            {
                if (requirement == null ||
                    requirement.requiredAmount <= 0)
                {
                    continue;
                }

                bool mergesSpent =
                    GameInventory.Instance.Spend(
                        requirement.type,
                        requirement.requiredAmount,
                        notify: false
                    );

                if (!mergesSpent)
                {
                    reason =
                        $"Could not spend " +
                        $"{requirement.requiredAmount} " +
                        $"{requirement.type} merges.";

                    Debug.LogError(
                        $"[BallUnlockManager] Purchase validation " +
                        $"passed, but spending merges failed for " +
                        $"{requirement.type}."
                    );

                    return false;
                }
            }
        }

        // Save the permanent local unlock.
        BallUnlockSave.SetUnlocked(
            type,
            true
        );

        // Notify all inventory UI once, after every deduction is complete.
        GameInventory.Instance.NotifyChanged();

        OnBallUnlocked?.Invoke(type);

        reason = $"{type} unlocked.";

        // Save coins, merges, and unlock state to Firestore.
        CloudSaveManager.SyncEconomyNow();

        return true;
    }

    public void RestoreUnlockFromCloud(
        BallType type,
        bool unlocked)
    {
        BallUnlockCatalogSO.UnlockDefinition definition =
            GetDefinition(type);

        if (definition == null)
            return;

        if (definition.unlockedByDefault)
            return;

        BallUnlockSave.SetUnlocked(
            type,
            unlocked
        );
    }

    public void ResetUnlocks()
    {
        BallUnlockSave.ResetAll();

        Debug.Log(
            "[BallUnlockManager] All saved unlocks were reset."
        );
    }

    public string GetRequirementSummary(
        BallType type)
    {
        BallUnlockCatalogSO.UnlockDefinition definition =
            GetDefinition(type);

        if (definition == null)
            return $"No unlock definition for {type}.";

        if (definition.unlockedByDefault)
            return $"{type} is unlocked by default.";

        string result =
            $"{type} is locked. Coins: " +
            $"{GameInventory.Instance.Get(CurrencyType.Coins)}" +
            $"/{definition.coinCost}";

        if (definition.mergeRequirements != null)
        {
            foreach (
                BallUnlockCatalogSO.MergeRequirement requirement
                in definition.mergeRequirements)
            {
                if (requirement == null)
                    continue;

                int current =
                    GameInventory.Instance.Get(
                        requirement.type
                    );

                result +=
                    $", {requirement.type}: " +
                    $"{current}/{requirement.requiredAmount}";
            }
        }

        return result;
    }

    public void DebugUnlock(BallType type)
    {
        BallUnlockSave.SetUnlocked(
            type,
            true
        );

        OnBallUnlocked?.Invoke(type);

        Debug.Log(
            $"[BallUnlockManager] Debug unlocked {type}. " +
            $"Stored value: " +
            $"{BallUnlockSave.GetRawSavedValue(type)}"
        );
    }

    private BallUnlockCatalogSO.UnlockDefinition
        GetDefinition(BallType type)
    {
        if (unlockCatalog == null)
        {
            Debug.LogError(
                "[BallUnlockManager] Unlock catalog is not assigned."
            );

            return null;
        }

        return unlockCatalog.GetDefinition(type);
    }

#if UNITY_EDITOR
    [ContextMenu("Reset All Ball Unlocks")]
    private void ResetAllUnlocks()
    {
        ResetUnlocks();
    }
#endif
}