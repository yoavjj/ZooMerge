using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OutOfTriesPopup : SfxBehaviourTirgger
{
    public static OutOfTriesPopup LastSpawned { get; private set; }

    [Header("Buy Retries")]
    [SerializeField] private int retryRefillCostCoins = 5;
    [SerializeField] private RetryRefillPricingSO pricing;

    public event Action Closed;
    public static event Action RetriesPurchased;

    [Header("Animation")]
    [SerializeField] private Animator animator;                 
    [SerializeField] private string outTrigger = "Out";
    [SerializeField] private string outOfCoinTrigger = "OutOFCoin"; // new trigger
    [SerializeField] private TextMeshProUGUI MessageText;         // optional: assign if you added a message text
    [SerializeField] private string outOfCoinMessage = "Not enough coins!";
    [SerializeField] private string retryTrigger = "Retry";
    [SerializeField] private string successMessage = "Let's Go!";
    [SerializeField, Min(0f)] private float destroyDelay = 0.35f; // fallback if no clip found

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI retryCostText;
    [SerializeField] private RectTransform layoutRootToRebuild;
    [SerializeField] private GameObject quitButtonRoot;

    private bool isClosing;

    private bool hasLoggedPopupShown;

    private void Awake()
    {
        LastSpawned = this;
    }

    private void OnEnable()
    {
        RefreshCostUI();

        if (!hasLoggedPopupShown)
        {
            hasLoggedPopupShown = true;
            LogPopupShown();
        }
    }

    private void RefreshCostUI()
    {
        if (retryCostText != null)
            retryCostText.text = CurrentCost().ToString();

        // Force layout refresh (HorizontalLayoutGroup + ContentSizeFitter)
        if (layoutRootToRebuild != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRootToRebuild);
        }
    }

    private int CurrentCost()
    {
        if (pricing == null) return retryRefillCostCoins; // fallback to your old field
        return pricing.GetCost(RetryRefillPricingRuntime.PurchaseCount);
    }

    public void QuitToMainMenu()
    {
        PlayUiSfx(SfxCue.ButtonClick);

        // Optional: just save snapshot so server has latest economy/time/etc
        CloudSaveManager.SaveSnapshot(incrementMidLevelCompleted: false);

        // Close this popup first
        Close();

        // Prevent AE_OnRevealFinished from starting a session
        WinLosePopup.SetSuppressSessionStartFromReveal(true);

        // Close WinLose popup and return to main menu
        if (WinLosePopup.Instance != null)
            WinLosePopup.Instance.OnMainMenuButtonPressed();
        else
            PopupManager.Instance?.ConfirmReturnToMainMenu();
    }

    public void WatchAdForRetry()
    {
        // Only offer if truly out of tries
        if (PlayerProgress.CurrentLevelRetriesRemaining() > 0)
        {
            Debug.Log("[OutOfTriesPopup] Retries already available, no need for ad.");
            Close();
            PlayUiSfx(SfxCue.ButtonClickNegative);
            return;
        }

        if (AdManager.Instance == null)
        {
            Debug.LogWarning("[OutOfTriesPopup] AdManager missing.");
            PlayUiSfx(SfxCue.ButtonClickNegative);
            return;
        }

        PlayUiSfx(SfxCue.ButtonClick);

        // Optional: disable button UI here while ad is showing

        AdManager.Instance.ShowRewarded(
        onReward: () =>
        {
            // Permanent per-player Firestore counter.
            CloudSaveManager
                .RegisterRewardedAdCompleted();

            // Aggregated Analytics event.
            AnalyticsEvents.RewardedAdCompleted(
                "out_of_tries_retry"
            );

            // Tell UI/game systems.
            RetriesPurchased?.Invoke();

            // Existing cloud snapshot.
            CloudSaveManager.SyncEconomyNow();

            if (MessageText != null)
            {
                MessageText.text =
                    successMessage;
            }

            Debug.Log(
                "[OutOfTriesPopup] " +
                "Rewarded success: +1 retry"
            );

            Close(retryTrigger);
        },
            onFail: reason =>
            {
                AnalyticsEvents.RewardedAdFailed(
                    "out_of_tries_retry",
                    reason
                );

                Debug.LogWarning(
                    "[OutOfTriesPopup] " +
                    $"Rewarded failed: {reason}"
                );
            }
    )   ;
    }

    public void BuyRetriesWithCoins()
    {
        // Only makes sense if truly out of tries.
        if (PlayerProgress.CurrentLevelRetriesRemaining() > 0)
        {
            Debug.Log(
                "[OutOfTriesPopup] " +
                "Retries already available, no need to buy."
            );

            Close();
            PlayUiSfx(SfxCue.ButtonClick);
            return;
        }

        // Capture this purchase's exact price once.
        int cost = CurrentCost();

        if (cost <= 0)
        {
            Debug.LogWarning(
                "[OutOfTriesPopup] " +
                "Retry refill price is invalid."
            );

            PlayOutOfCoinFeedback("Invalid price");
            PlayUiSfx(SfxCue.ButtonClickNegative);
            return;
        }

        int coins =
            GameInventory.Instance.Get(
                CurrencyType.Coins
            );

        if (coins < cost)
        {
            Debug.Log(
                "[OutOfTriesPopup] " +
                $"Not enough coins. Have {coins}, " +
                $"need {cost}."
            );

            PlayOutOfCoinFeedback(
                "Not enough coins"
            );

            PlayUiSfx(
                SfxCue.ButtonClickNegative
            );

            return;
        }

        bool paid =
            GameInventory.Instance.Spend(
                CurrencyType.Coins,
                cost
            );

        if (!paid)
        {
            Debug.Log(
                "[OutOfTriesPopup] " +
                "Spend failed unexpectedly."
            );

            PlayOutOfCoinFeedback(
                "Not enough coins"
            );

            PlayUiSfx(
                SfxCue.ButtonClickNegative
            );

            return;
        }

        // ✅ Successful purchase.

        // Track permanent Firestore totals locally.
        CloudSaveManager
            .RegisterRetryPurchaseWithCoins(cost);

        // Read the new balance after spending.
        int coinsAfterPurchase =
            GameInventory.Instance.Get(
                CurrencyType.Coins
            );

        // Advance dynamic pricing.
        RetryRefillPricingRuntime
            .IncrementPurchaseCount();

        // Track the purchase in Firebase Analytics.
        AnalyticsEvents.RetryPurchasedWithCoins(
            coinsSpent: cost,
            balanceBefore: coins,
            balanceAfter: coinsAfterPurchase,
            purchaseNumber:
                RetryRefillPricingRuntime.PurchaseCount
        );

        // Tell UI/game systems.
        RetriesPurchased?.Invoke();

        if (MessageText != null)
        {
            MessageText.text =
                successMessage;
        }

        Close(retryTrigger);

        Debug.Log(
            "[OutOfTriesPopup] " +
            $"Bought retries for {cost} coins. " +
            $"Balance: {coins} -> {coinsAfterPurchase}. " +
            $"Purchase number: " +
            $"{RetryRefillPricingRuntime.PurchaseCount}"
        );

        // Existing snapshot also uploads
        // permanent Firestore counters.
        CloudSaveManager.SyncEconomyNow();

        PlayUiSfx(SfxCue.ButtonClick);
    }

    private void PlayOutOfCoinFeedback(string reason)
    {
        if (MessageText != null)
            MessageText.text = outOfCoinMessage;

        if (animator != null && !string.IsNullOrEmpty(outOfCoinTrigger))
        {
            animator.ResetTrigger(outOfCoinTrigger);
            animator.SetTrigger(outOfCoinTrigger);
        }
        else
        {
            Debug.LogWarning("[OutOfTriesPopup] OutOFCoin trigger requested but animator/trigger not set.");
        }
    }

    // Hook this to your Close / X button
    public void Close(string triggerOverride = null)
    {
        if (isClosing) return;
        isClosing = true;

        Closed?.Invoke();

        if (animator != null)
        {
            string trig = string.IsNullOrEmpty(triggerOverride) ? outTrigger : triggerOverride;

            if (!string.IsNullOrEmpty(trig))
            {
                animator.ResetTrigger(trig);
                animator.SetTrigger(trig);

                StartCoroutine(DestroyAfterAnim());
                return;
            }
        }

        Destroy(gameObject,0.35f); // slight buffer to ensure anim finishes
    }

    private IEnumerator DestroyAfterAnim()
    {
        // Wait one frame so the trigger is applied
        yield return null;

        float wait = destroyDelay;

        // Try to use the actual current state's length (more accurate than hardcoding)
        if (animator != null)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.length > 0f)
                wait = state.length;
        }

        yield return new WaitForSeconds(wait);
        Destroy(gameObject,0.35f); // slight buffer to ensure anim finishes
    }

    public void SetQuitButtonVisible(bool visible)
    {
        if (quitButtonRoot != null)
            quitButtonRoot.SetActive(visible);
    }

    public void PlayButtonSfx()
    {
        PlayUiSfx(SfxCue.ButtonClick);
    }

    private void LogPopupShown()
    {
        int coinBalance =
            GameInventory.Instance.Get(
                CurrencyType.Coins
            );

        int retryCost = CurrentCost();

        AnalyticsEvents.OutOfTriesPopupShown(
            coinBalance,
            retryCost
        );
    }
}