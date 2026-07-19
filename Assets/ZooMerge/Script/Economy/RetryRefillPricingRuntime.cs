using UnityEngine;

public static class RetryRefillPricingRuntime
{
    private const string KEY_PURCHASE_COUNT = "RetryRefillPurchaseCount";

    public static int PurchaseCount
    {
        get => PlayerPrefs.GetInt(KEY_PURCHASE_COUNT, 0);
        set => PlayerPrefs.SetInt(KEY_PURCHASE_COUNT, Mathf.Max(0, value));
    }

    public static void IncrementPurchaseCount()
    {
        PurchaseCount = PurchaseCount + 1;
        PlayerPrefs.Save();
    }

    public static void ResetPurchaseCount()
    {
        PurchaseCount = 0;
        PlayerPrefs.Save();
    }
}