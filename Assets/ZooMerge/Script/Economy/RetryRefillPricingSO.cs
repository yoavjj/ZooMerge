using UnityEngine;

[CreateAssetMenu(menuName = "Game/Economy/Retry Refill Pricing", fileName = "RetryRefillPricing")]
public class RetryRefillPricingSO : ScriptableObject
{
    [Tooltip("Costs by purchase index: 0=first purchase, 1=second, ...")]
    public int[] costs = { 5, 7, 9, 12, 15 };

    [Tooltip("If purchases exceed costs length, keep increasing by this step.")]
    public int overflowStep = 3;

    public int GetCost(int purchaseIndex)
    {
        if (purchaseIndex < 0) purchaseIndex = 0;

        if (costs != null && costs.Length > 0)
        {
            if (purchaseIndex < costs.Length)
                return Mathf.Max(0, costs[purchaseIndex]);

            // overflow: continue increasing from last cost
            int last = Mathf.Max(0, costs[costs.Length - 1]);
            int extra = purchaseIndex - (costs.Length - 1);
            return Mathf.Max(0, last + extra * Mathf.Max(0, overflowStep));
        }

        // fallback
        return 5;
    }
}