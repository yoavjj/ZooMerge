using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SpaceshipSkinCatalog",
    menuName = "Game/Spaceship Skin Catalog"
)]
public class SpaceshipSkinCatalogSO : ScriptableObject
{
    [Serializable]
    public class SkinDefinition
    {
        [Header("Server ID")]
        [Tooltip("Unique ID used by Remote Config / server rewards.")]
        public string skinId;

        [Header("Spaceship Art")]
        public Sprite spaceshipSprite;
    }

    [SerializeField] private List<SkinDefinition> skins = new();

    public IReadOnlyList<SkinDefinition> Skins => skins;

    public SkinDefinition GetDefinition(string skinId)
    {
        if (string.IsNullOrWhiteSpace(skinId))
            return null;

        return skins.Find(skin =>
            skin != null &&
            string.Equals(
                skin.skinId,
                skinId,
                StringComparison.Ordinal
            )
        );
    }
}