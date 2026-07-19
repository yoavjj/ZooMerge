using System.Collections;
using TMPro;
using UnityEngine;

public class TopBarCoinItemUI : TopBarCurrencyItemUI
{
    [SerializeField] private TextMeshProUGUI addAmountText;

    private int startCount;
    private int targetCount;

    private int pendingAddAmount;

    private bool saveCoinsToCloudAfterCountUp;

    public void AddCoins(int amount, bool saveToCloudAfterCountUp = false)
    {
        if (amount <= 0)
            return;

        saveCoinsToCloudAfterCountUp = saveToCloudAfterCountUp;

        pendingAddAmount = amount;

        startCount = count;
        targetCount = count + pendingAddAmount;

        if (addAmountText != null)
        {
            addAmountText.gameObject.SetActive(true);
            addAmountText.text = $"+{pendingAddAmount}";
        }

        if (animator != null && !string.IsNullOrEmpty(addAnimationName))
        {
            animator.Play(addAnimationName, 0, 0f);
        }
        else
        {
            ApplyAddCoins();

            if (saveCoinsToCloudAfterCountUp)
            {
                saveCoinsToCloudAfterCountUp = false;
                CloudSaveManager.SaveCoinsOnly();
            }
        }
    }

    public void ApplyAddCoins()
    {
        // UI only
        count = targetCount;
        pendingAddAmount = 0;

        UpdateCountText();

        if (addAmountText != null)
            addAmountText.gameObject.SetActive(false);
    }

    //getting called from animation event, so we can sync the actual count update with the visual "Add" effect
    public void AnimateCountUp()
    {
        StopAllCoroutines();
        StartCoroutine(CountUpRoutine());
    }

    private IEnumerator CountUpRoutine()
    {
        const float duration = 0.25f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);

            int value = Mathf.RoundToInt(Mathf.Lerp(startCount, targetCount, p));
            countText.text = value.ToString();
            yield return null;
        }

        // ✅ finalize BOTH the text and the cached count
        countText.text = targetCount.ToString();
        count = targetCount;

        if (saveCoinsToCloudAfterCountUp)
        {
            saveCoinsToCloudAfterCountUp = false;
            CloudSaveManager.SaveCoinsOnly();
        }
    }
}