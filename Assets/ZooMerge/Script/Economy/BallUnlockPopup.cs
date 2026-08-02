using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BallUnlockPopup : MonoBehaviour
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
    [SerializeField] private Button launchButton;

    [Header("Message Panel")]
    [SerializeField] private Animator messagePanelAnimator;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private string messageTrigger = "In";

    [SerializeField, Min(0f)]
    private float messageCooldown = 0.75f;

    [SerializeField, TextArea]
    private string insufficientResourcesMessage =
        "Not enough resources.";

    private bool messageLocked;
    private Coroutine messageCooldownRoutine;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string inTrigger = "In";
    [SerializeField] private string outTrigger = "Out";
    [SerializeField] private string outRevealTrigger = "OutReveal";

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
    

    private void Awake()
    {
        if (launchButton != null)
        {
            launchButton.onClick.AddListener(
                PurchaseWithCoins
            );
        }
    }

    private void OnEnable()
    {
        GameInventory.Instance.OnChanged +=
            HandleInventoryChanged;
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

        messageLocked = false;
        isOpening = false;
    }

    private void OnDestroy()
    {
        if (launchButton != null)
        {
            launchButton.onClick.RemoveListener(
                PurchaseWithCoins
            );
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
    }

    private void RefreshLaunchButton()
    {
        if (launchButton == null)
            return;

        BallUnlockManager manager =
            BallUnlockManager.Instance;

        if (manager == null)
        {
            launchButton.interactable = false;
            return;
        }

        // Keep the button clickable while locked.
        // Affordability is checked when the player presses it.
        launchButton.interactable =
            !manager.IsUnlocked(targetType) &&
            !isCompletingPurchase;
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

        if (launchButton != null)
            launchButton.interactable = false;

        if (!manager.TryUnlock(
                targetType,
                out string result))
        {
            isCompletingPurchase = false;

            if (launchButton != null)
                launchButton.interactable = true;

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

        if (spawnedAnimalCard != null)
        {
            spawnedAnimalCard.PlayUnlockReveal();
        }
        else
        {
            Close();
        }

        AnimalUnlocked?.Invoke(targetType);
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
}