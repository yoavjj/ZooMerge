using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BallUnlockCatalog",
    menuName = "Game/Ball Unlock Catalog"
)]
public class BallUnlockCatalogSO : ScriptableObject
{
    [Serializable]
    public class MergeRequirement
    {
        public BallType type;

        [Min(0)]
        public int requiredAmount;
    }

    [Serializable]
    public class UnlockDefinition
    {
        public BallType type;

        [Tooltip("These animals are available immediately.")]
        public bool unlockedByDefault;

        [Header("Soft Currency Unlock")]
        [Min(0)]
        public int coinCost;

        public List<MergeRequirement> mergeRequirements = new();

        [Header("Real Money Purchase")]
        public bool purchasableWithIap;

        [Tooltip(
            "Must exactly match the product ID configured in the store."
        )]
        public string iapProductId;
    }

    [SerializeField]
    private List<UnlockDefinition> definitions = new();

    public IReadOnlyList<UnlockDefinition> Definitions =>
        definitions;

    public UnlockDefinition GetDefinition(BallType type)
    {
        return definitions.Find(definition =>
            definition != null &&
            definition.type == type
        );
    }

    public UnlockDefinition GetDefinitionByProductId(
        string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            return null;

        return definitions.Find(definition =>
            definition != null &&
            definition.purchasableWithIap &&
            string.Equals(
                definition.iapProductId,
                productId,
                StringComparison.Ordinal
            )
        );
    }

    public void ApplyRemoteConfig(
        BallUnlockRemoteConfigData remoteData)
    {
        if (remoteData == null ||
            remoteData.definitions == null)
        {
            return;
        }

        foreach (
            BallUnlockRemoteDefinition remote
            in remoteData.definitions)
        {
            if (remote == null ||
                string.IsNullOrWhiteSpace(remote.type))
            {
                continue;
            }

            if (!Enum.TryParse(
                    remote.type,
                    true,
                    out BallType ballType))
            {
                Debug.LogWarning(
                    $"[BallUnlockCatalogSO] Unknown BallType " +
                    $"from Remote Config: '{remote.type}'"
                );

                continue;
            }

            UnlockDefinition local =
                GetDefinition(ballType);

            if (local == null)
            {
                Debug.LogWarning(
                    $"[BallUnlockCatalogSO] " +
                    $"No local definition for '{ballType}'."
                );

                continue;
            }

            // Remote Config controls only
            // soft-currency requirements.
            local.coinCost =
                Mathf.Max(
                    0,
                    remote.coinCost
                );

            if (local.mergeRequirements == null)
            {
                local.mergeRequirements =
                    new List<MergeRequirement>();
            }

            local.mergeRequirements.Clear();

            if (remote.mergeRequirements == null)
                continue;

            foreach (
                BallUnlockRemoteMergeRequirement requirement
                in remote.mergeRequirements)
            {
                if (requirement == null ||
                    string.IsNullOrWhiteSpace(
                        requirement.type))
                {
                    continue;
                }

                if (!Enum.TryParse(
                        requirement.type,
                        true,
                        out BallType requirementType))
                {
                    Debug.LogWarning(
                        $"[BallUnlockCatalogSO] " +
                        $"Unknown merge requirement type: " +
                        $"'{requirement.type}'"
                    );

                    continue;
                }

                local.mergeRequirements.Add(
                    new MergeRequirement
                    {
                        type =
                            requirementType,

                        requiredAmount =
                            Mathf.Max(
                                0,
                                requirement.requiredAmount
                            )
                    }
                );
            }
        }

        Debug.Log(
            "[BallUnlockCatalogSO] " +
            "Remote unlock requirements applied."
        );
    }
}