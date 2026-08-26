using UnityEngine;

public class SpaceshipSkinController : MonoBehaviour
{
    public static SpaceshipSkinController Instance { get; private set; }

    [Header("Catalog")]
    [SerializeField] private SpaceshipSkinCatalogSO skinCatalog;

    [Header("Target")]
    [SerializeField] private SpriteRenderer spaceshipRenderer;

    private string currentSkinId;

    private void Awake()
    {
        Instance = this;

        ApplySavedSkin();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ApplySavedSkin()
    {
        string skinId = SpaceshipSkinProgress.CurrentSkinId;

        if (!ApplySkin(skinId))
            ApplySkin(SpaceshipSkinProgress.DefaultSkinId);
    }

    public bool UnlockAndApplySkin(string skinId)
    {
        if (!ApplySkin(skinId))
            return false;

        bool wasAlreadyUnlocked = SpaceshipSkinProgress.IsUnlocked(skinId);

        SpaceshipSkinProgress.UnlockAndSelect(skinId);

        if (!wasAlreadyUnlocked)
            AnalyticsEvents.SpaceshipSkinUnlocked(skinId);

        CloudSaveManager.SaveSpaceshipSkinsOnly(
            success =>
            {
                if (success)
                {
                    Debug.Log($"[SpaceshipSkinController] Skin '{skinId}' saved to cloud.");
                }
                else
                {
                    Debug.LogWarning($"[SpaceshipSkinController] Skin '{skinId}' saved locally, but cloud save failed.");
                }
            }
        );

        Debug.Log($"[SpaceshipSkinController] Unlocked and selected skin: {skinId}");

        return true;
    }

    public bool ApplySkin(string skinId)
    {
        if (skinCatalog == null)
        {
            Debug.LogError("[SpaceshipSkinController] Skin catalog is missing.");
            return false;
        }

        if (spaceshipRenderer == null)
        {
            Debug.LogError("[SpaceshipSkinController] SpriteRenderer reference is missing.");
            return false;
        }

        SpaceshipSkinCatalogSO.SkinDefinition definition = skinCatalog.GetDefinition(skinId);

        if (definition == null)
        {
            Debug.LogWarning($"[SpaceshipSkinController] Skin not found: {skinId}");
            return false;
        }

        if (definition.spaceshipSprite == null)
        {
            Debug.LogWarning($"[SpaceshipSkinController] Skin '{skinId}' has no sprite.");
            return false;
        }

        spaceshipRenderer.sprite = definition.spaceshipSprite;
        currentSkinId = definition.skinId;

        Debug.Log($"[SpaceshipSkinController] Applied skin: {currentSkinId}");

        return true;
    }

    public string GetCurrentSkinId()
    {
        return currentSkinId;
    }
}