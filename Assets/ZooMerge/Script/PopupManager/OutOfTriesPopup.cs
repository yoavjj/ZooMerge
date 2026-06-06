using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OutOfTriesPopup : MonoBehaviour
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
    [SerializeField] private TextMeshProUGUI CoinText;         // optional: assign if you added a message text
    [SerializeField] private string outOfCoinMessage = "Not enough coins!";
    [SerializeField] private string retryTrigger = "Retry";
    [SerializeField] private string successMessage = "Let's Go!";
    [SerializeField, Min(0f)] private float destroyDelay = 0.35f; // fallback if no clip found

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI retryCostText;
    [SerializeField] private RectTransform layoutRootToRebuild;
    [SerializeField] private GameObject quitButtonRoot;

    private bool isClosing;

    private void Awake()
    {
        LastSpawned = this;
    }

    private void OnEnable()
    {
        RefreshCostUI();
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
        // ✅ Do NOT change progress at all
        // (no fallback, no level-back)

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

    public void BuyRetriesWithCoins()
    {
        // Only makes sense if truly out of tries
        if (PlayerProgress.CurrentLevelRetriesRemaining() > 0)
        {
            Debug.Log("[OutOfTriesPopup] Retries already available, no need to buy.");
            Close();
            return;
        }

        // Validate price
        if (CurrentCost() <= 0)
        {
            Debug.LogWarning("[OutOfTriesPopup] retryRefillCostCoins is invalid.");
            PlayOutOfCoinFeedback("Invalid price");
            return;
        }

        // Check balance first (more explicit than calling Spend directly)
        int coins = GameInventory.Instance.Get(CurrencyType.Coins);
        if (coins < CurrentCost())
        {
            Debug.Log($"[OutOfTriesPopup] Not enough coins. Have {coins}, need {CurrentCost()}.");
            PlayOutOfCoinFeedback("Not enough coins");
            return;
        }

        // Pay
        bool paid = GameInventory.Instance.Spend(CurrencyType.Coins, CurrentCost());
        if (!paid) // safety (shouldn't happen if we checked coins)
        {
            Debug.Log("[OutOfTriesPopup] Spend failed unexpectedly.");
            PlayOutOfCoinFeedback("Not enough coins");
            return;
        }

        // Refill retries for the current level
        PlayerProgress.RefillRetriesForCurrentNewLevel();

        // ✅ tell WinLosePopup “we have retries again”
        RetriesPurchased?.Invoke();

        if (CoinText != null)
            CoinText.text = successMessage;

        // plays your Retry animation, then destroys using the same coroutine
        Close(retryTrigger);

        Debug.Log($"[OutOfTriesPopup] Bought retries. Retries now: {PlayerProgress.CurrentLevelRetriesRemaining()}");

        RetryRefillPricingRuntime.IncrementPurchaseCount();
        CloudSaveManager.SyncEconomyNow();
    }

    private void PlayOutOfCoinFeedback(string reason)
    {
        if (CoinText != null)
            CoinText.text = outOfCoinMessage;

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
}