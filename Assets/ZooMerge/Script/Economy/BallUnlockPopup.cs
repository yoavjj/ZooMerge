using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BallUnlockPopup : SfxBehaviourTirgger
{
    [Header("Data")]
    [SerializeField] private BallUnlockCatalogSO unlockCatalog;
    [SerializeField] private BallSet ballSet;

    [Header("Animal Card")]
    [SerializeField] private Transform animalCardContainer;
    [SerializeField] private BallChoiceItemUI animalCardPrefab;

    [Header("Requirement Items")]
    [SerializeField] private Transform requirementsContainer;

    [SerializeField]
    private BallUnlockRequirementItemUI requirementItemPrefab;

    private BallUnlockRequirementItemUI spawnedCoinRequirement;

    [Header("Currency Requirement")]
    [SerializeField] private Transform coinRequirementContainer;
    [SerializeField] private Sprite coinIcon;

    [Header("UI")]
    [SerializeField] private Button coinPurchaseButton;

    [Header("In-App Purchase")]
    [SerializeField] private Button iapPurchaseButton;
    [SerializeField] private TextMeshProUGUI iapPriceText;

    [Header("Message Panel")]
    [SerializeField] private Animator messagePanelAnimator;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private string messageTrigger = "In";

    [Header("Purchase Button Ready FX")]
    [SerializeField] private Animator launchButtonAnimator;
    [SerializeField] private string readyTrigger = "Ready";
    [SerializeField] private CardSelectionVisualController coinPurchaseVisualController;

    [SerializeField, TextArea] private string purchaseSuccessMessage = "Purchase successful!";

    [SerializeField, Min(0f)]
    private float purchaseSuccessDelay = 1.25f;

    [SerializeField] private CanvasGroup iapLoadingGroup;
    [SerializeField, Range(0f, 1f)] private float iapLoadingAlpha = 1f;
    [SerializeField, Min(0.01f)] private float iapLoadingFadeDuration = 0.2f;

    private Coroutine iapLoadingRoutine;

    private Coroutine iapSuccessRoutine;

    [SerializeField, Min(0f)]
    private float messageCooldown = 0.75f;

    [SerializeField, TextArea]
    private string insufficientResourcesMessage =
        "Not enough resources.";

    [SerializeField, TextArea]
    private string iapPurchaseFailedMessage =
        "Purchase failed. Please try again.";

    [SerializeField, TextArea]
    private string iapPurchaseDeferredMessage =
        "Purchase is waiting for approval.";

    private bool messageLocked;
    private Coroutine messageCooldownRoutine;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string inTrigger = "In";
    [SerializeField] private string outTrigger = "Out";
    [SerializeField] private string outRevealTrigger = "OutReveal";

    [SerializeField]
    private AnimationCurve iapLoadingFadeCurve =
    AnimationCurve.EaseInOut(
        0f,
        0f,
        1f,
        1f
    );

    public event Action Closed;
    public event Action<BallType> AnimalUnlocked;

    [Header("Opening")]
    [SerializeField, Range(1, 5)]
    private int layoutWarmupFrames = 2;

    private Coroutine openRoutine;
    private bool isOpening;
    private bool isCompletingPurchase;

    private readonly List<BallUnlockRequirementItemUI>
        spawnedRequirements = new();

    private BallChoiceItemUI spawnedAnimalCard;
    private BallType targetType;

    public BallType TargetType => targetType;
    private bool wasReadyToUnlock;


    private void Awake()
    {
        if (coinPurchaseButton != null)
        {
            coinPurchaseButton.onClick.AddListener(
                PurchaseWithCoins
            );
        }

        if (iapPurchaseButton != null)
        {
            iapPurchaseButton.onClick.AddListener(
                PurchaseWithIap
            );
        }

        if (iapLoadingGroup != null)
        {
            iapLoadingGroup.alpha = 0f;
            iapLoadingGroup.blocksRaycasts = false;
            iapLoadingGroup.interactable = false;
        }
    }

    private void OnEnable()
    {
        GameInventory.Instance.OnChanged +=
            HandleInventoryChanged;

        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.PurchaseCompleted +=
                HandleIapPurchaseCompleted;

            IAPManager.Instance.Ready +=
                HandleIapReady;
        }
    }

    private void OnDisable()
    {
        GameInventory.Instance.OnChanged -=
            HandleInventoryChanged;

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        if (messageCooldownRoutine != null)
        {
            StopCoroutine(messageCooldownRoutine);
            messageCooldownRoutine = null;
        }

        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.PurchaseCompleted -=
                HandleIapPurchaseCompleted;

            IAPManager.Instance.Ready -=
                HandleIapReady;
        }

        if (iapSuccessRoutine != null)
        {
            StopCoroutine(iapSuccessRoutine);
            iapSuccessRoutine = null;
        }

        if (iapLoadingRoutine != null)
        {
            StopCoroutine(
                iapLoadingRoutine
            );

            iapLoadingRoutine = null;
        }

        if (iapLoadingGroup != null)
        {
            iapLoadingGroup.alpha = 0f;
            iapLoadingGroup.blocksRaycasts = false;
            iapLoadingGroup.interactable = false;
        }

        messageLocked = false;
        isOpening = false;
    }

    private void OnDestroy()
    {
        if (coinPurchaseButton != null)
        {
            coinPurchaseButton.onClick.RemoveListener(
                PurchaseWithCoins
            );
        }

        if (iapPurchaseButton != null)
        {
            iapPurchaseButton.onClick.RemoveListener(
                PurchaseWithIap
            );
        }
    }

    private void HandleIapPurchaseCompleted(
        IAPPurchaseResult result)
    {
        HideIapLoading();

        BallUnlockCatalogSO.UnlockDefinition definition =
            unlockCatalog != null
                ? unlockCatalog.GetDefinition(targetType)
                : null;

        if (definition == null)
            return;

        bool resultHasProductId =
            !string.IsNullOrWhiteSpace(
                result.ProductId
            );

        if (resultHasProductId &&
            !string.Equals(
                result.ProductId,
                definition.iapProductId,
                StringComparison.Ordinal))
        {
            return;
        }

        if (!result.Success)
        {
            isCompletingPurchase = false;

            // Allow the user to try the real-money purchase again.
            if (iapPurchaseButton != null)
            {
                iapPurchaseButton.interactable =
                    IAPManager.Instance != null &&
                    IAPManager.Instance.IsReady;
            }

            // Coin purchasing should also return to its normal state.
            RefreshLaunchButton();

            string message =
                result.FailureReason ==
                IAPPurchaseFailureReason.PurchaseDeferred
                    ? iapPurchaseDeferredMessage
                    : iapPurchaseFailedMessage;

            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                message = result.Message;
            }

            ShowMessage(message);

            Debug.LogWarning(
                $"[BallUnlockPopup] IAP failed for {targetType}. " +
                $"Reason={result.FailureReason}, " +
                $"Message='{result.Message}'"
            );

            return;
        }

        CompletePurchasedUnlock(
            result.ProductId
        );
    }

    private void CompletePurchasedUnlock(
        string productId)
    {
        BallUnlockCatalogSO.UnlockDefinition definition =
            unlockCatalog != null
                ? unlockCatalog.GetDefinitionByProductId(
                    productId
                )
                : null;

        if (definition == null)
        {
            isCompletingPurchase = false;

            ShowMessage(
                "Purchase completed, but the product could not be displayed."
            );

            return;
        }

        if (definition.type != targetType)
        {
            isCompletingPurchase = false;

            Debug.LogError(
                $"[BallUnlockPopup] Product '{productId}' belongs to " +
                $"{definition.type}, but popup displays {targetType}."
            );

            return;
        }

        if (coinPurchaseButton != null)
            coinPurchaseButton.interactable = false;

        if (iapPurchaseButton != null)
            iapPurchaseButton.interactable = false;

        if (iapSuccessRoutine != null)
            StopCoroutine(iapSuccessRoutine);

        iapSuccessRoutine = StartCoroutine(
            IapPurchaseSuccessRoutine(
                definition.type
            )
        );
    }

    private IEnumerator IapPurchaseSuccessRoutine(
    BallType unlockedType)
    {
        ShowMessage(purchaseSuccessMessage);
        PlayUiSfx(SfxCue.CurrentWorldLevel_Woosh);
        if (purchaseSuccessDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                purchaseSuccessDelay
            );
        }

        AnimalUnlocked?.Invoke(
            unlockedType
        );

        if (spawnedAnimalCard != null)
        {
            StartOutReveal();
        }
        else
        {
            Close();
        }

        iapSuccessRoutine = null;
    }

    private void HandleIapReady()
    {
        RefreshIapButton();
    }

    private void RefreshIapButton()
    {
        if (iapPurchaseButton == null)
            return;

        BallUnlockCatalogSO.UnlockDefinition definition =
            unlockCatalog != null
                ? unlockCatalog.GetDefinition(targetType)
                : null;

        bool hasIapOffer =
            definition != null &&
            definition.purchasableWithIap &&
            !string.IsNullOrWhiteSpace(
                definition.iapProductId
            );

        IAPManager manager =
            IAPManager.Instance;

        bool available =
            hasIapOffer &&
            manager != null &&
            manager.IsReady &&
            BallUnlockManager.Instance != null &&
            !BallUnlockManager.Instance.IsUnlocked(
                targetType
            );

        iapPurchaseButton.gameObject.SetActive(
            hasIapOffer
        );

        iapPurchaseButton.interactable =
            available &&
            !isCompletingPurchase;

        if (iapPriceText != null)
        {
            iapPriceText.text =
                manager != null
                    ? manager.GetLocalizedPrice(targetType)
                    : string.Empty;
        }
    }

    public void Open(BallType type)
    {
        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        openRoutine = StartCoroutine(
            OpenRoutine(type)
        );
    }

    private IEnumerator OpenRoutine(BallType type)
    {
        isOpening = true;
        isCompletingPurchase = false;
        targetType = type;
        wasReadyToUnlock = false;

        BallUnlockCatalogSO.UnlockDefinition definition =
            unlockCatalog != null
                ? unlockCatalog.GetDefinition(type)
                : null;

        if (definition == null)
        {
            Debug.LogWarning(
                $"[BallUnlockPopup] No unlock definition exists for {type}."
            );

            isOpening = false;
            openRoutine = null;
            yield break;
        }

        gameObject.SetActive(true);

        if (animator != null &&
            !string.IsNullOrEmpty(inTrigger))
        {
            animator.ResetTrigger(inTrigger);
        }

        BuildAnimalCard();
        BuildRequirements(definition);
        RefreshLaunchButton();
        RefreshIapButton();

        int frames = Mathf.Clamp(layoutWarmupFrames, 1, 5);

        for (int i = 0; i < frames; i++)
            yield return null;

        Canvas.ForceUpdateCanvases();

        if (transform is RectTransform popupRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                popupRect
            );
        }

        Canvas.ForceUpdateCanvases();

        PlayInAnimation();

        isOpening = false;
        openRoutine = null;
    }

    private void BuildAnimalCard()
    {
        ClearAnimalCard();

        if (animalCardPrefab == null ||
            animalCardContainer == null)
        {
            Debug.LogError(
                "[BallUnlockPopup] Missing animal-card prefab or container."
            );

            return;
        }

        Sprite profileSprite = ballSet != null
            ? ballSet.GetProfileSprite(targetType)
            : null;

        spawnedAnimalCard = Instantiate(
            animalCardPrefab,
            animalCardContainer
        );

        spawnedAnimalCard.Initialize(
            targetType,
            profileSprite
        );

        spawnedAnimalCard.SetDisplayOnly(
            showFullColor: true,
            showLockedOverlay: true
        );

        spawnedAnimalCard.RevealFinished +=
        HandleAnimalCardRevealFinished;
    }

    private void HandleAnimalCardRevealFinished()
    {
        CloseAfterReveal();
    }

    private void BuildRequirements(
        BallUnlockCatalogSO.UnlockDefinition definition)
    {
        ClearRequirementItems();

        if (requirementItemPrefab == null)
        {
            Debug.LogError(
                "[BallUnlockPopup] Requirement item prefab is missing."
            );
            return;
        }

        // Coin requirement goes into its own container.
        if (definition.coinCost > 0)
        {
            if (coinRequirementContainer == null)
            {
                Debug.LogError(
                    "[BallUnlockPopup] Coin requirement container is missing."
                );
            }
            else
            {
                int currentCoins =
                    GameInventory.Instance.Get(
                        CurrencyType.Coins
                    );

                spawnedCoinRequirement =
                    CreateRequirementItem(
                        coinRequirementContainer,
                        "Coins",
                        coinIcon,
                        currentCoins,
                        definition.coinCost
                    );
            }
        }

        // Merge requirements go into the normal requirements container.
        if (definition.mergeRequirements == null)
            return;

        if (requirementsContainer == null)
        {
            Debug.LogError(
                "[BallUnlockPopup] Merge requirements container is missing."
            );
            return;
        }

        foreach (
            BallUnlockCatalogSO.MergeRequirement requirement
            in definition.mergeRequirements)
        {
            if (requirement == null)
                continue;

            int currentMerges =
                GameInventory.Instance.Get(
                    requirement.type
                );

            Sprite requirementIcon =
                ballSet != null
                    ? ballSet.GetMergeIcon(
                        requirement.type
                    )
                    : null;

            BallUnlockRequirementItemUI item =
                CreateRequirementItem(
                    requirementsContainer,
                    requirement.type.ToString(),
                    requirementIcon,
                    currentMerges,
                    requirement.requiredAmount
                );

            if (item != null)
                spawnedRequirements.Add(item);
        }
    }

    private BallUnlockRequirementItemUI CreateRequirementItem(
        Transform targetContainer,
        string requirementName,
        Sprite icon,
        int currentAmount,
        int requiredAmount)
    {
        if (targetContainer == null ||
            requirementItemPrefab == null)
        {
            return null;
        }

        BallUnlockRequirementItemUI item =
            Instantiate(
                requirementItemPrefab,
                targetContainer
            );

        item.Initialize(
            requirementName,
            icon,
            currentAmount,
            requiredAmount
        );

        return item;
    }

    private void HandleInventoryChanged()
    {
        if (!gameObject.activeInHierarchy)
            return;

        // The popup is already playing the successful reveal.
        // Do not rebuild the requirement items and reset their sliders.
        if (isCompletingPurchase)
            return;

        Refresh();
    }

    public void Refresh()
    {
        if (unlockCatalog == null)
            return;

        BallUnlockCatalogSO.UnlockDefinition definition =
            unlockCatalog.GetDefinition(targetType);

        if (definition == null)
            return;

        BuildRequirements(definition);
        RefreshLaunchButton();
        RefreshIapButton();
    }

    private void RefreshLaunchButton()
    {
        if (coinPurchaseButton == null)
            return;

        BallUnlockManager manager = BallUnlockManager.Instance;

        if (manager == null)
        {
            coinPurchaseButton.interactable = false;
            coinPurchaseVisualController?.SetSelectedImmediate(false);
            wasReadyToUnlock = false;
            return;
        }

        bool isUnlocked = manager.IsUnlocked(targetType);

        bool canUnlock = !isUnlocked && manager.CanUnlock(targetType, out _);

        coinPurchaseButton.interactable = !isUnlocked && !isCompletingPurchase;

        if (coinPurchaseVisualController != null)
            coinPurchaseVisualController.SetSelected(canUnlock);

        // Play only when changing from not-ready to ready.
        if (canUnlock &&
            !wasReadyToUnlock &&
            launchButtonAnimator != null &&
            !string.IsNullOrEmpty(readyTrigger))
        {
            launchButtonAnimator.ResetTrigger(readyTrigger);
            launchButtonAnimator.SetTrigger(readyTrigger);
        }

        wasReadyToUnlock = canUnlock;
    }

    private void PurchaseWithCoins()
    {
        BallUnlockManager manager =
            BallUnlockManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "[BallUnlockPopup] BallUnlockManager.Instance is null."
            );

            return;
        }

        if (isCompletingPurchase)
            return;

        // Keep the button clickable, but show feedback
        // when the requirements have not been met.
        if (!manager.CanUnlock(
                targetType,
                out string reason))
        {
            if (!manager.IsUnlocked(targetType))
            {
                ShowMessage(
                    insufficientResourcesMessage
                );
            }

            Debug.Log(
                $"[BallUnlockPopup] Cannot unlock " +
                $"{targetType}: {reason}"
            );

            return;
        }

        // Prevent inventory notifications from rebuilding
        // the requirement sliders during the reveal.
        isCompletingPurchase = true;

        if (coinPurchaseButton != null)
            coinPurchaseButton.interactable = false;

        if (!manager.TryUnlock(
                targetType,
                out string result))
        {
            isCompletingPurchase = false;

            if (coinPurchaseButton != null)
                coinPurchaseButton.interactable = true;

            ShowMessage(
                insufficientResourcesMessage
            );

            Debug.Log(
                $"[BallUnlockPopup] Could not unlock " +
                $"{targetType}: {result}"
            );

            Refresh();
            return;
        }

        Debug.Log(
            $"[BallUnlockPopup] {result}"
        );
        AnimalUnlocked?.Invoke(targetType);

        if (spawnedAnimalCard != null)
        {
            StartOutReveal();
        }
        else
        {
            Close();
        }
    }

    private void StartOutReveal()
    {
        if (animator == null ||
            string.IsNullOrEmpty(outRevealTrigger))
        {
            AE_StartAnimalReveal();
            return;
        }

        animator.ResetTrigger(inTrigger);
        animator.ResetTrigger(outTrigger);
        animator.ResetTrigger(outRevealTrigger);

        animator.SetTrigger(outRevealTrigger);
    }

    private void ShowMessage(string message)
    {
        if (messageLocked)
            return;

        messageLocked = true;

        if (messageText != null)
            messageText.text = message;

        if (messagePanelAnimator != null &&
            !string.IsNullOrEmpty(messageTrigger))
        {
            messagePanelAnimator.ResetTrigger(
                messageTrigger
            );

            messagePanelAnimator.SetTrigger(
                messageTrigger
            );
        }

        if (messageCooldownRoutine != null)
        {
            StopCoroutine(
                messageCooldownRoutine
            );
        }

        messageCooldownRoutine =
            StartCoroutine(
                MessageCooldownRoutine()
            );
    }

    private IEnumerator MessageCooldownRoutine()
    {
        yield return new WaitForSecondsRealtime(
            messageCooldown
        );

        messageLocked = false;
        messageCooldownRoutine = null;
    }

    private void PlayInAnimation()
    {
        if (animator == null ||
            string.IsNullOrEmpty(inTrigger))
        {
            return;
        }

        animator.ResetTrigger(inTrigger);
        animator.SetTrigger(inTrigger);
    }

    private void ClearAnimalCard()
    {
        if (spawnedAnimalCard != null)
        {
            spawnedAnimalCard.RevealFinished -=
                HandleAnimalCardRevealFinished;

            Destroy(spawnedAnimalCard.gameObject);
            spawnedAnimalCard = null;
        }

        if (animalCardContainer == null)
            return;

        foreach (Transform child in animalCardContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void ClearRequirementItems()
    {
        if (spawnedCoinRequirement != null)
        {
            Destroy(spawnedCoinRequirement.gameObject);
            spawnedCoinRequirement = null;
        }

        foreach (
            BallUnlockRequirementItemUI item
            in spawnedRequirements)
        {
            if (item != null)
                Destroy(item.gameObject);
        }

        spawnedRequirements.Clear();
    }

    private void PurchaseWithIap()
    {
        IAPManager manager =
            IAPManager.Instance;

        if (manager == null)
        {
            ShowMessage(
                "Store unavailable."
            );

            return;
        }

        if (isCompletingPurchase)
            return;

        isCompletingPurchase = true;

        if (iapPurchaseButton != null)
            iapPurchaseButton.interactable = false;

        ShowIapLoading();

        manager.PurchaseBall(
            targetType
        );
    }

    public void Close()
    {
        if (isOpening)
            return;

        if (animator == null ||
            string.IsNullOrEmpty(outTrigger))
        {
            FinishClose();
            return;
        }

        animator.ResetTrigger(inTrigger);
        animator.ResetTrigger(outRevealTrigger);
        animator.ResetTrigger(outTrigger);

        animator.SetTrigger(outTrigger);
        PlayUiSfx(SfxCue.ButtonClick);
    }

    private void CloseAfterReveal()
    {
        if (isOpening)
            return;

        if (animator == null ||
            string.IsNullOrEmpty(outRevealTrigger))
        {
            FinishClose();
            return;
        }

        animator.ResetTrigger(inTrigger);
        animator.ResetTrigger(outTrigger);
        animator.ResetTrigger(outRevealTrigger);

        animator.SetTrigger(outRevealTrigger);
    }

    public void AE_FinishClose(float delay = 0f)
    {
        FinishClose(delay);
    }

    public void AE_StartAnimalReveal()
    {
        if (spawnedAnimalCard == null)
        {
            Debug.LogWarning(
                "[BallUnlockPopup] Cannot start animal reveal because the card is missing."
            );

            return;
        }

        spawnedAnimalCard.PlayUnlockReveal();
    }

    private void FinishClose(float delay = 0f)
    {
        Closed?.Invoke();
        Destroy(gameObject, delay);
    }

    public void AE_PlayRequirementFillAnimations()
    {
        if (spawnedCoinRequirement != null)
        {
            spawnedCoinRequirement.PlayFillAnimation();
        }

        foreach (
            BallUnlockRequirementItemUI item
            in spawnedRequirements)
        {
            if (item != null)
                item.PlayFillAnimation();
        }
    }

    private void ShowIapLoading()
    {
        FadeIapLoadingTo(
            iapLoadingAlpha
        );
    }

    private void HideIapLoading()
    {
        FadeIapLoadingTo(
            0f
        );
    }

    private void FadeIapLoadingTo(
        float targetAlpha)
    {
        if (iapLoadingGroup == null)
            return;

        if (iapLoadingRoutine != null)
        {
            StopCoroutine(
                iapLoadingRoutine
            );
        }

        iapLoadingRoutine =
            StartCoroutine(
                FadeIapLoadingRoutine(
                    targetAlpha
                )
            );
    }

    private IEnumerator FadeIapLoadingRoutine(
        float targetAlpha)
    {
        float startAlpha =
            iapLoadingGroup.alpha;

        float elapsed = 0f;

        while (elapsed < iapLoadingFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalized =
                Mathf.Clamp01(
                    elapsed /
                    iapLoadingFadeDuration
                );

            float t =
                iapLoadingFadeCurve.Evaluate(
                    normalized
                );

            iapLoadingGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t
                );

            yield return null;
        }

        iapLoadingGroup.alpha =
            targetAlpha;

        iapLoadingGroup.blocksRaycasts =
            targetAlpha > 0f;

        iapLoadingGroup.interactable =
            targetAlpha > 0f;

        iapLoadingRoutine = null;
    }
}