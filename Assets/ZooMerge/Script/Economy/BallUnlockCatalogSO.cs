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

        [Header("Unlock Price")]
        [Min(0)]
        public int coinCost;

        [Header("Required Lifetime Merges")]
        public List<MergeRequirement> mergeRequirements = new();
    }

    [SerializeField]
    private List<UnlockDefinition> definitions = new();

    public UnlockDefinition GetDefinition(BallType type)
    {
        return definitions.Find(definition =>
            definition != null &&
            definition.type == type
        );
    }
}