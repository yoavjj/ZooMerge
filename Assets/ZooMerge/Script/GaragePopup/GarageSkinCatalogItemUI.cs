using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GarageSkinCatalogItemUI : SfxBehaviourTirgger
{
    [Header("Refs")]
    [SerializeField] private Image spaceshipImage;
    [SerializeField] private Button selectButton;
    [SerializeField] private TextMeshProUGUI unlockLevelText;
    [SerializeField] private GameObject lockObject;

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

    public void Initialize(
        string skinId,
        Sprite spaceshipSprite)
    {
        SkinId = skinId;

        if (spaceshipImage != null)
        {
            spaceshipImage.sprite = spaceshipSprite;
            spaceshipImage.enabled = spaceshipSprite != null;
        }

        RefreshVisualState();
        RefreshUnlockLevelText();

        gameObject.name = $"Skin_{skinId}";
    }

    public void RefreshVisualState()
    {
        bool unlocked =
            SpaceshipSkinProgress.IsUnlocked(SkinId);

        // Lock icon
        if (lockObject != null)
            lockObject.SetActive(!unlocked);

        // Spaceship visual
        if (spaceshipImage != null)
        {
            if (unlocked)
            {
                spaceshipImage.material = null;
            }
            else
            {
                if (lockedSpaceshipMaterial == null)
                {
                    Debug.LogWarning(
                        $"[GarageSkinCatalogItemUI] Locked material missing for {SkinId}"
                    );
                }
                else
                {
                    if (lockedSpaceshipMaterial.HasProperty(BlendProperty))
                        lockedSpaceshipMaterial.SetFloat(BlendProperty, 0f);

                    spaceshipImage.material =
                        lockedSpaceshipMaterial;
                }
            }
        }

        RefreshUnlockLevelText();
    }

    private void RefreshUnlockLevelText()
    {
        if (unlockLevelText == null)
            return;

        // Default spaceship is owned from the beginning.
        if (SkinId == SpaceshipSkinProgress.DefaultSkinId)
        {
            unlockLevelText.text = "DEFAULT";
            return;
        }

        // Already owned.
        if (SpaceshipSkinProgress.IsUnlocked(SkinId))
        {
            unlockLevelText.text = "UNLOCKED";
            return;
        }

        if (TryGetUnlockLevel(
                SkinId,
                out int galaxyId,
                out int levelIndex))
        {
            unlockLevelText.text =
                $"GALAXY {galaxyId} - LEVEL {levelIndex}";

            return;
        }

        // No reward entry found in Remote Config.
        unlockLevelText.text =
            string.Empty;

        Debug.LogWarning(
            $"[GarageSkinCatalogItemUI] " +
            $"No unlock level found for skin '{SkinId}'."
        );
    }

    private bool TryGetUnlockLevel(
        string skinId,
        out int galaxyId,
        out int levelIndex)
    {
        galaxyId = 0;
        levelIndex = 0;

        MergeLevelData data =
            FirebaseInitializer.MergeScoreData;

        if (data == null ||
            data.galaxies == null)
        {
            return false;
        }

        foreach (GalaxyData galaxy in data.galaxies)
        {
            if (galaxy == null ||
                galaxy.levels == null)
            {
                continue;
            }

            foreach (MergeLevel level in galaxy.levels)
            {
                if (level == null ||
                    level.completionReward == null)
                {
                    continue;
                }

                LevelCompletionReward reward =
                    level.completionReward;

                if ((reward.rewardType &
                     LevelRewardType.SpaceshipSkin) == 0)
                {
                    continue;
                }

                if (!string.Equals(
                        reward.spaceshipSkinId,
                        skinId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                galaxyId =
                    galaxy.galaxyId;

                levelIndex =
                    level.index;

                return true;
            }
        }

        return false;
    }

    private void HandlePressed()
    {
        if (string.IsNullOrWhiteSpace(SkinId))
            return;

        bool unlocked =
            SpaceshipSkinProgress.IsUnlocked(SkinId);

        if (!unlocked)
        {
            PlayUiSfx(
                SfxCue.ButtonClickNegative
            );

            return;
        }

        PlayUiSfx(
            SfxCue.ButtonClick
        );

        Selected?.Invoke(SkinId);
    }
}