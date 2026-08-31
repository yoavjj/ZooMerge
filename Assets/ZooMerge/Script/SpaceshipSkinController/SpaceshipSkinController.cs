using UnityEngine;

public class SpaceshipSkinController : MonoBehaviour
{
    public static SpaceshipSkinController Instance { get; private set; }

    [Header("Catalog")]
    [SerializeField] private SpaceshipSkinCatalogSO skinCatalog;

    [Header("Target")]
    [SerializeField] private SpriteRenderer spaceshipRenderer;

    [Header("Reveal")]
    [SerializeField] private SpaceshipSkinRevealAnimator skinRevealAnimator;

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

    public void UnlockAndRevealSkin(string skinId, System.Action<bool> onComplete = null)
    {
        if (skinCatalog == null)
        {
            Debug.LogError("[SpaceshipSkinController] Skin catalog is missing.");
            onComplete?.Invoke(false);
            return;
        }

        if (spaceshipRenderer == null)
        {
            Debug.LogError("[SpaceshipSkinController] SpriteRenderer reference is missing.");
            onComplete?.Invoke(false);
            return;
        }

        SpaceshipSkinCatalogSO.SkinDefinition definition = skinCatalog.GetDefinition(skinId);

        if (definition == null)
        {
            Debug.LogWarning($"[SpaceshipSkinController] Skin not found: {skinId}");
            onComplete?.Invoke(false);
            return;
        }

        if (definition.spaceshipSprite == null)
        {
            Debug.LogWarning($"[SpaceshipSkinController] Skin '{skinId}' has no sprite.");
            onComplete?.Invoke(false);
            return;
        }

        Sprite oldSprite = spaceshipRenderer.sprite;
        Sprite newSprite = definition.spaceshipSprite;

        bool wasAlreadyUnlocked = SpaceshipSkinProgress.IsUnlocked(skinId);

        // Save ownership immediately. Do not wait for the visual animation.
        SpaceshipSkinProgress.UnlockAndSelect(skinId);

        if (!wasAlreadyUnlocked)
            AnalyticsEvents.SpaceshipSkinUnlocked(skinId);

        CloudSaveManager.SaveSpaceshipSkinsOnly(
            success =>
            {
                if (success)
                    Debug.Log($"[SpaceshipSkinController] Skin '{skinId}' saved to cloud.");
                else
                    Debug.LogWarning($"[SpaceshipSkinController] Skin '{skinId}' saved locally, but cloud save failed.");
            }
        );

        currentSkinId = skinId;

        if (skinRevealAnimator == null || oldSprite == null || oldSprite == newSprite)
        {
            spaceshipRenderer.sprite = newSprite;
            onComplete?.Invoke(true);
            return;
        }

        skinRevealAnimator.PlayReveal(
            oldSprite,
            newSprite,
            () =>
            {
                Debug.Log($"[SpaceshipSkinController] Reveal finished: {skinId}");
                onComplete?.Invoke(true);
            }
        );
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