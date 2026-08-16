using UnityEngine;

public class BallUnlockOfferPrompt : MonoBehaviour
{
    [Header("Data")]
    [SerializeField]
    private BallUnlockCatalogSO unlockCatalog;

    [Header("Behaviour")]
    [Tooltip(
        "When enabled, each animal is automatically offered only once."
    )]
    [SerializeField]
    private bool offerOnlyOnce = true;

    public bool TryGetAvailableUnlock(
        out BallType availableType)
    {
        availableType = default;

        BallUnlockManager manager =
            BallUnlockManager.Instance;

        if (manager == null || unlockCatalog == null)
            return false;

        foreach (
            BallUnlockCatalogSO.UnlockDefinition definition
            in unlockCatalog.Definitions)
        {
            if (definition == null)
                continue;

            if (definition.unlockedByDefault)
                continue;

            if (manager.IsUnlocked(definition.type))
                continue;

            if (offerOnlyOnce &&
                WasAlreadyOffered(definition.type))
            {
                continue;
            }

            // Checks both coins and merge requirements.
            if (!manager.CanUnlock(
                    definition.type,
                    out _))
            {
                continue;
            }

            availableType = definition.type;
            return true;
        }

        return false;
    }

    public void MarkAsOffered(BallType type)
    {
        PlayerPrefs.SetInt(
            GetOfferKey(type),
            1
        );

        PlayerPrefs.Save();
    }

    private static bool WasAlreadyOffered(
        BallType type)
    {
        return PlayerPrefs.GetInt(
            GetOfferKey(type),
            0
        ) == 1;
    }

    private static string GetOfferKey(
        BallType type)
    {
        return $"BALL_UNLOCK_OFFERED_{type}";
    }

#if UNITY_EDITOR
    [ContextMenu("Reset Unlock Offer History")]
    private void ResetOfferHistory()
    {
        if (unlockCatalog == null)
            return;

        foreach (
            BallUnlockCatalogSO.UnlockDefinition definition
            in unlockCatalog.Definitions)
        {
            if (definition == null)
                continue;

            PlayerPrefs.DeleteKey(
                GetOfferKey(definition.type)
            );
        }

        PlayerPrefs.Save();

        Debug.Log(
            "[BallUnlockOfferPrompt] Offer history reset."
        );
    }
#endif
}