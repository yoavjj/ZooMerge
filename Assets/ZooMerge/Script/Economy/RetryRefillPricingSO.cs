using UnityEngine;

[CreateAssetMenu(
    menuName = "Game/Economy/Retry Refill Pricing",
    fileName = "RetryRefillPricing"
)]
public class RetryRefillPricingSO : ScriptableObject
{
    [Tooltip(
        "Costs by purchase index: 0=first purchase, 1=second, ..."
    )]
    public int[] costs =
    {
        5,
        7,
        9,
        12,
        15
    };

    [Tooltip(
        "If purchases exceed costs length, keep increasing by this step."
    )]
    public int overflowStep = 3;

    public int GetCost(int purchaseIndex)
    {
        if (purchaseIndex < 0)
            purchaseIndex = 0;

        int[] activeCosts =
            costs;

        int activeOverflowStep =
            overflowStep;

        RetryRefillPricingRemoteConfigData remote =
            FirebaseInitializer.RetryRefillPricingConfig;

        if (remote != null &&
            remote.costs != null &&
            remote.costs.Length > 0)
        {
            activeCosts =
                remote.costs;

            activeOverflowStep =
                remote.overflowStep;
        }

        if (activeCosts != null &&
            activeCosts.Length > 0)
        {
            if (purchaseIndex <
                activeCosts.Length)
            {
                return Mathf.Max(
                    0,
                    activeCosts[purchaseIndex]
                );
            }

            int last =
                Mathf.Max(
                    0,
                    activeCosts[
                        activeCosts.Length - 1
                    ]
                );

            int extra =
                purchaseIndex -
                (activeCosts.Length - 1);

            return Mathf.Max(
                0,
                last +
                extra *
                Mathf.Max(
                    0,
                    activeOverflowStep
                )
            );
        }

        return 5;
    }
}