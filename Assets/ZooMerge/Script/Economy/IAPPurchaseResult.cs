public enum IAPRewardType
{
    None,
    Retries,
    Coins
}

public enum IAPPurchaseFailureReason
{
    NotInitialized,
    ProductNotFound,
    AlreadyOwned,
    StoreUnavailable,
    UserCancelled,
    PurchaseFailed,
    PurchaseDeferred,
    FulfillmentFailed
}

public readonly struct IAPPurchaseResult
{
    public bool Success { get; }
    public string ProductId { get; }
    public IAPPurchaseFailureReason FailureReason { get; }
    public string Message { get; }

    private IAPPurchaseResult(
        bool success,
        string productId,
        IAPPurchaseFailureReason failureReason,
        string message)
    {
        Success = success;
        ProductId = productId;
        FailureReason = failureReason;
        Message = message;
    }

    public static IAPPurchaseResult Succeeded(
        string productId)
    {
        return new IAPPurchaseResult(
            true,
            productId,
            default,
            string.Empty
        );
    }

    public static IAPPurchaseResult Failed(
        string productId,
        IAPPurchaseFailureReason reason,
        string message)
    {
        return new IAPPurchaseResult(
            false,
            productId,
            reason,
            message
        );
    }
}