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
}