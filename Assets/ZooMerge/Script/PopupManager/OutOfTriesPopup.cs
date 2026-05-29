using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OutOfTriesPopup : MonoBehaviour
{

    [Header("Buy Retries")]
    [SerializeField] private int retryRefillCostCoins = 5;

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

    private bool isClosing;

    private void OnEnable()
    {
        RefreshCostUI();
    }

    private void RefreshCostUI()
    {
        if (retryCostText != null)
            retryCostText.text = retryRefillCostCoins.ToString();

        // Force layout refresh (HorizontalLayoutGroup + ContentSizeFitter)
        if (layoutRootToRebuild != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRootToRebuild);
        }
    }

    public void QuitToCheckpoint()
    {
        // 1) Move the player back to checkpoint locally (and apply into MergeLevelManager)
        PlayerProgress.FallbackToCheckpoint(); // sets LastGalaxy/LastLevel/EnemyIndex=0 + MergeLevelManager.SetProgress

        // 2) Notify server (progress + full snapshot so it sticks)
        CloudSaveManager.ForceCloudProgressMap(PlayerProgress.LastGalaxyId, PlayerProgress.LastLevelInGalaxy, 0);
        CloudSaveManager.SaveSnapshot(incrementMidLevelCompleted: false);

        // 3) Close THIS popup (plays Out animation, then destroys)
        Close();

        WinLosePopup.SetSuppressSessionStartFromReveal(true);

        // 4) Close WinLosePopup + go back to main menu
        if (WinLosePopup.Instance != null)
            WinLosePopup.Instance.OnMainMenuButtonPressed();
        else
            PopupManager.Instance?.ConfirmReturnToMainMenu(); // fallback safety
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
        if (retryRefillCostCoins <= 0)
        {
            Debug.LogWarning("[OutOfTriesPopup] retryRefillCostCoins is invalid.");
            PlayOutOfCoinFeedback("Invalid price");
            return;
        }

        // Check balance first (more explicit than calling Spend directly)
        int coins = GameInventory.Instance.Get(CurrencyType.Coins);
        if (coins < retryRefillCostCoins)
        {
            Debug.Log($"[OutOfTriesPopup] Not enough coins. Have {coins}, need {retryRefillCostCoins}.");
            PlayOutOfCoinFeedback("Not enough coins");
            return;
        }

        // Pay
        bool paid = GameInventory.Instance.Spend(CurrencyType.Coins, retryRefillCostCoins);
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
        
        PlayRetrySuccessFeedback();

        Debug.Log($"[OutOfTriesPopup] Bought retries. Retries now: {PlayerProgress.CurrentLevelRetriesRemaining()}");

        CloudSaveManager.SyncEconomyNow();
    }

    private void PlayRetrySuccessFeedback()
    {
        if (CoinText != null)
            CoinText.text = successMessage;

        if (animator != null && !string.IsNullOrEmpty(retryTrigger))
        {
            animator.ResetTrigger(retryTrigger);
            animator.SetTrigger(retryTrigger);
        }
        else
        {
            Debug.LogWarning("[OutOfTriesPopup] Retry trigger requested but animator/trigger not set.");
        }
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
    public void Close()
    {
        if (isClosing) return;
        isClosing = true;

        Closed?.Invoke();

        if (animator != null && !string.IsNullOrEmpty(outTrigger))
        {
            animator.ResetTrigger(outTrigger); // optional safety
            animator.SetTrigger(outTrigger);

            StartCoroutine(DestroyAfterOut());
        }
        else
        {
            // No animator assigned -> destroy immediately
            Destroy(gameObject);
        }
    }

    private IEnumerator DestroyAfterOut()
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
        Destroy(gameObject);
    }
}