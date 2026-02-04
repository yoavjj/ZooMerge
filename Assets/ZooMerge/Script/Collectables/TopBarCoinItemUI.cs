using System.Collections;
using TMPro;
using UnityEngine;

public class TopBarCoinItemUI : TopBarCurrencyItemUI
{
    [SerializeField] private TextMeshProUGUI addAmountText;

    private int startCount;
    private int targetCount;

    private int pendingAddAmount;

    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        pendingAddAmount = amount;

        startCount = count;
        targetCount = count + pendingAddAmount;

        // Show +X text
        if (addAmountText != null)
        {
            addAmountText.gameObject.SetActive(true);
            addAmountText.text = $"+{pendingAddAmount}";
        }

        // Trigger animation ONLY
        if (animator != null && !string.IsNullOrEmpty(addAnimationName))
        {
            animator.Play(addAnimationName, 0, 0f);
        }
        else
        {
            ApplyAddCoins(); // fallback
        }
    }

    // 🔔 CALLED BY ANIMATION EVENT
    public void ApplyAddCoins()
    {
        count = targetCount;
        pendingAddAmount = 0;

        UpdateCountText();

        // Hide +X text
        if (addAmountText != null)
            addAmountText.gameObject.SetActive(false);
    }

    public void AnimateCountUp()
    {
        StopAllCoroutines();
        StartCoroutine(CountUpRoutine());
    }

    private IEnumerator CountUpRoutine()
    {
        const float duration = 0.25f; // tweak to taste
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);

            int value = Mathf.RoundToInt(
                Mathf.Lerp(startCount, targetCount, p)
            );

            countText.text = value.ToString();
            yield return null;
        }

        countText.text = targetCount.ToString();
    }
}