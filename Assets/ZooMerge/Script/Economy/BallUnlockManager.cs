using System;
using UnityEngine;

public class BallUnlockManager : MonoBehaviour
{
    public static BallUnlockManager Instance { get; private set; }

    [SerializeField]
    private BallUnlockCatalogSO unlockCatalog;

    public event Action<BallType> OnBallUnlocked;

    private const string UnlockKeyPrefix = "BALL_UNLOCKED_";

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

        // Missing definitions are treated as locked.
        // This prevents newly added animals from accidentally becoming usable.
        if (definition == null)
            return false;

        if (definition.unlockedByDefault)
            return true;

        return PlayerPrefs.GetInt(GetUnlockKey(type), 0) == 1;
    }

    public bool CanUnlock(BallType type, out string reason)
    {
        reason = string.Empty;

        BallUnlockCatalogSO.UnlockDefinition definition =
            GetDefinition(type);

        if (definition == null)
        {
            reason = $"No unlock definition exists for {type}.";
            return false;
        }

        if (IsUnlocked(type))
        {
            reason = $"{type} is already unlocked.";
            return true;
        }

        int currentCoins =
            GameInventory.Instance.Get(CurrencyType.Coins);

        if (currentCoins < definition.coinCost)
        {
            reason =
                $"Not enough coins for {type}. " +
                $"Need {definition.coinCost}, have {currentCoins}.";

            return false;
        }

        foreach (
            BallUnlockCatalogSO.MergeRequirement requirement
            in definition.mergeRequirements)
        {
            if (requirement == null)
                continue;

            int currentAmount =
                GameInventory.Instance.Get(requirement.type);

            if (currentAmount < requirement.requiredAmount)
            {
                reason =
                    $"Not enough {requirement.type} merges for {type}. " +
                    $"Need {requirement.requiredAmount}, " +
                    $"have {currentAmount}.";

                return false;
            }
        }

        return true;
    }

    public bool TryUnlock(BallType type, out string reason)
    {
        reason = string.Empty;

        if (IsUnlocked(type))
        {
            reason = $"{type} is already unlocked.";
            return true;
        }

        if (!CanUnlock(type, out reason))
            return false;

        BallUnlockCatalogSO.UnlockDefinition definition =
            GetDefinition(type);

        if (definition == null)
        {
            reason = $"No unlock definition exists for {type}.";
            return false;
        }

        // Merge values are requirements only; they are not consumed.
        // Coins are paid when the animal is unlocked.
        if (!GameInventory.Instance.Spend(
                CurrencyType.Coins,
                definition.coinCost))
        {
            reason = $"Could not spend coins to unlock {type}.";
            return false;
        }

        PlayerPrefs.SetInt(GetUnlockKey(type), 1);
        PlayerPrefs.Save();

        OnBallUnlocked?.Invoke(type);

        reason = $"{type} unlocked.";
        return true;
    }

    public void ResetUnlocks()
    {
        foreach (
            BallType type
            in Enum.GetValues(typeof(BallType)))
        {
            PlayerPrefs.DeleteKey(GetUnlockKey(type));
        }

        PlayerPrefs.Save();
    }

    public string GetRequirementSummary(BallType type)
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

        foreach (
            BallUnlockCatalogSO.MergeRequirement requirement
            in definition.mergeRequirements)
        {
            if (requirement == null)
                continue;

            int current =
                GameInventory.Instance.Get(requirement.type);

            result +=
                $", {requirement.type}: " +
                $"{current}/{requirement.requiredAmount}";
        }

        return result;
    }

    private BallUnlockCatalogSO.UnlockDefinition GetDefinition(
        BallType type)
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

    private static string GetUnlockKey(BallType type)
    {
        return $"{UnlockKeyPrefix}{type}";
    }

#if UNITY_EDITOR
    [ContextMenu("Reset All Ball Unlocks")]
    private void ResetAllUnlocks()
    {
        foreach (
            BallType type
            in Enum.GetValues(typeof(BallType)))
        {
            PlayerPrefs.DeleteKey(GetUnlockKey(type));
        }

        PlayerPrefs.Save();

        Debug.Log(
            "[BallUnlockManager] All saved ball unlocks were reset."
        );
    }
#endif
}