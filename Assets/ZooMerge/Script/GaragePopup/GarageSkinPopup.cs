using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GarageSkinPopup : MonoBehaviour
{
    [Header("Catalog")]
    [SerializeField] private SpaceshipSkinCatalogSO skinCatalog;

    [Header("Scroll Rect")]
    [SerializeField] private ScrollRect skinScrollRect;

    [Header("Catalog Item")]
    [SerializeField] private GarageSkinCatalogItemUI skinItemPrefab;

    [Header("Main Spaceship Preview")]
    [SerializeField] private Image mainSpaceshipImage;
    private string currentPreviewSkinId;

    [SerializeField]
    private SpaceshipSkinRevealAnimator
    mainSpaceshipRevealAnimator;

    private bool isSkinRevealPlaying;
    private string revealingSkinId;


    private readonly List<GarageSkinCatalogItemUI> spawnedItems = new();

    private void Start()
    {
        BuildSkinCatalog();

        currentPreviewSkinId =
            SpaceshipSkinProgress.CurrentSkinId;

        RefreshMainSpaceship();
    }

    private void OnEnable()
    {
        SpaceshipSkinController.SkinSelected += HandleSkinSelected;
    }

    private void OnDisable()
    {
        SpaceshipSkinController.SkinSelected -= HandleSkinSelected;
    }

    private void BuildSkinCatalog()
    {
        if (skinCatalog == null)
        {
            Debug.LogError(
                "[GarageSkinPopup] SpaceshipSkinCatalog is missing."
            );

            return;
        }

        if (skinScrollRect == null ||
            skinScrollRect.content == null)
        {
            Debug.LogError(
                "[GarageSkinPopup] ScrollRect or Content is missing."
            );

            return;
        }

        if (skinItemPrefab == null)
        {
            Debug.LogError(
                "[GarageSkinPopup] Skin item prefab is missing."
            );

            return;
        }

        ClearCatalog();

        for (int i = 0; i < skinCatalog.Skins.Count; i++)
        {
            SpaceshipSkinCatalogSO.SkinDefinition definition =
                skinCatalog.Skins[i];

            if (definition == null)
                continue;

            GarageSkinCatalogItemUI item =
                Instantiate(
                    skinItemPrefab,
                    skinScrollRect.content
                );

            item.Initialize(
                definition.skinId,
                definition.spaceshipSprite
            );

            item.Selected += HandleCatalogItemSelected;

            spawnedItems.Add(item);
        }

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            skinScrollRect.content
        );

        skinScrollRect.horizontalNormalizedPosition = 0f;
    }

    private void HandleCatalogItemSelected(string skinId)
    {
        if (skinCatalog == null || mainSpaceshipImage == null)
            return;

        // Locked skins cannot be previewed or selected.
        if (!SpaceshipSkinProgress.IsUnlocked(skinId))
        {
            Debug.Log($"[GarageSkinPopup] Skin is locked: {skinId}");
            return;
        }

        SpaceshipSkinCatalogSO.SkinDefinition definition =
            skinCatalog.GetDefinition(skinId);

        if (definition == null ||
            definition.spaceshipSprite == null)
        {
            return;
        }

        // Already previewing this skin.
        if (currentPreviewSkinId == skinId)
            return;

        SpaceshipSkinCatalogSO.SkinDefinition oldDefinition =
            skinCatalog.GetDefinition(currentPreviewSkinId);

        Sprite oldSprite =
            oldDefinition != null
                ? oldDefinition.spaceshipSprite
                : mainSpaceshipImage.sprite;

        Sprite newSprite =
            definition.spaceshipSprite;

        // From this point onward this IS the Garage preview.
        currentPreviewSkinId = skinId;

        // Equip locally.
        if (SpaceshipSkinController.Instance != null)
            SpaceshipSkinController.Instance.SelectSkin(skinId);

        // Play reveal.
        isSkinRevealPlaying = true;
        revealingSkinId = skinId;

        if (mainSpaceshipRevealAnimator != null)
        {
            mainSpaceshipRevealAnimator.PlayReveal(
                oldSprite,
                newSprite,
                () => FinishGarageSkinReveal(skinId, newSprite)
            );

            return;
        }

        FinishGarageSkinReveal(skinId, newSprite);
    }

    private void FinishGarageSkinReveal(
    string skinId,
    Sprite finalSprite)
    {
        if (mainSpaceshipImage != null)
            mainSpaceshipImage.sprite =
                finalSprite;

        isSkinRevealPlaying = false;
        revealingSkinId = null;

        Debug.Log(
            $"[GarageSkinPopup] " +
            $"Skin preview reveal finished: {skinId}"
        );
    }

    private void HandleSkinSelected(
        string skinId)
    {
        // Our own Garage selection is currently
        // being animated. Don't snap the sprite.
        if (isSkinRevealPlaying &&
            skinId == revealingSkinId)
        {
            return;
        }

        // Skin was changed externally while
        // the Garage is open.
        SetMainSpaceship(skinId);
    }

    private void RefreshMainSpaceship()
    {
        SetMainSpaceship(
            SpaceshipSkinProgress.CurrentSkinId
        );
    }

    private void SetMainSpaceship(string skinId)
    {
        if (mainSpaceshipImage == null ||
            skinCatalog == null)
        {
            return;
        }

        SpaceshipSkinCatalogSO.SkinDefinition definition =
            skinCatalog.GetDefinition(skinId);

        if (definition == null ||
            definition.spaceshipSprite == null)
        {
            return;
        }

        mainSpaceshipImage.sprite =
            definition.spaceshipSprite;

        mainSpaceshipImage.enabled = true;
    }

    private void ClearCatalog()
    {
        foreach (
            GarageSkinCatalogItemUI item
            in spawnedItems)
        {
            if (item == null)
                continue;

            item.Selected -=
                HandleCatalogItemSelected;

            Destroy(
                item.gameObject
            );
        }

        spawnedItems.Clear();
    }
}