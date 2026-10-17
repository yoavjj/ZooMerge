using System;
using UnityEngine;
using UnityEngine.UI;

public class GarageSkinCatalogItemUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image spaceshipImage;
    [SerializeField] private Button selectButton;

    [Header("Locked Visual")]
    [SerializeField] private Material lockedSpaceshipMaterial;

    private static readonly int BlendProperty =
        Shader.PropertyToID("_Blend");

    public string SkinId { get; private set; }

    public event Action<string> Selected;

    private void Awake()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(HandlePressed);
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(HandlePressed);
    }

    public void Initialize(string skinId, Sprite spaceshipSprite)
    {
        SkinId = skinId;

        if (spaceshipImage != null)
        {
            spaceshipImage.sprite = spaceshipSprite;
            spaceshipImage.enabled = spaceshipSprite != null;
        }

        RefreshVisualState();

        gameObject.name = $"Skin_{skinId}";
    }

    public void RefreshVisualState()
    {
        if (spaceshipImage == null)
            return;

        bool unlocked =
            SpaceshipSkinProgress.IsUnlocked(SkinId);

        if (unlocked)
        {
            spaceshipImage.material = null;
            return;
        }

        if (lockedSpaceshipMaterial == null)
        {
            Debug.LogWarning(
                $"[GarageSkinCatalogItemUI] Locked material missing for {SkinId}"
            );

            return;
        }

        // Force locked skins to full black & white.
        if (lockedSpaceshipMaterial.HasProperty(BlendProperty))
            lockedSpaceshipMaterial.SetFloat(BlendProperty, 0f);

        spaceshipImage.material =
            lockedSpaceshipMaterial;
    }

    private void HandlePressed()
    {
        if (string.IsNullOrWhiteSpace(SkinId))
            return;

        Selected?.Invoke(SkinId);
    }
}