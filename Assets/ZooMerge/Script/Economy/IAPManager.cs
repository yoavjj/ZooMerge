using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance { get; private set; }

    [Header("Catalog")]
    [SerializeField]
    private BallUnlockCatalogSO unlockCatalog;

    [Header("Editor Testing")]
    [SerializeField]
    private bool simulatePurchasesInEditor = true;

    [SerializeField, Min(0f)]
    private float simulatedPurchaseDelay = 0.75f;

    [SerializeField]
    private string simulatedLocalizedPrice = "$0.99";

    [SerializeField]
    private bool simulatedPurchaseShouldFail = false;

    [Header("Purchase Timeout")]
    [SerializeField, Min(5f)]
    private float purchaseTimeoutSeconds = 20f;

    private Coroutine purchaseTimeoutRoutine;
    private string waitingForProductId;

    public bool IsReady { get; private set; }

    public event Action Ready;
    public event Action<IAPPurchaseResult> PurchaseCompleted;

    private StoreController storeController;

    private readonly Dictionary<string, Product>
        productsById = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (purchaseTimeoutRoutine != null)
        {
            StopCoroutine(
                purchaseTimeoutRoutine
            );

            purchaseTimeoutRoutine =
                null;
        }

        UnsubscribeStoreEvents();

        if (Instance == this)
            Instance = null;
    }

    private async void Initialize()
    {
#if UNITY_EDITOR
        if (simulatePurchasesInEditor)
        {
            IsReady = true;

            Debug.Log(
                "[IAPManager] Editor purchase simulation is ready."
            );

            Ready?.Invoke();
            return;
        }
#endif

        try
        {
            Debug.Log(
                "[IAPManager] Starting Unity IAP initialization..."
            );

            storeController =
                UnityIAPServices.StoreController();

            SubscribeStoreEvents();

            await storeController.Connect();
            Debug.Log(
    $"[IAPManager] Bundle ID = {Application.identifier}"
);

            Debug.Log(
                "[IAPManager] Connected to store."
            );

            ProductCatalog catalog =
                ProductCatalog.LoadDefaultCatalog();

            Debug.Log(
$"[IAPManager] Loaded IAP catalog. " +
$"Products: {catalog.allProducts.Count}"
);

            foreach (ProductCatalogItem product in catalog.allProducts)
            {
                Debug.Log(
                    $"[IAPManager] Catalog product: " +
                    $"id='{product.id}', " +
                    $"type={product.type}"
                );
            }

            CatalogProvider catalogProvider =
                CodelessCatalogProvider.PopulateCatalogProvider(
                    catalog
                );

            catalogProvider.FetchProducts(
                products =>
                {
                    Debug.Log(
                        $"[IAPManager] Asking store to fetch " +
                        $"{products.Count} products."
                    );

                    foreach (ProductDefinition product in products)
                    {
                        Debug.Log(
                            $"[IAPManager] Fetching product: " +
                            $"id='{product.id}', " +
                            $"storeId='{product.storeSpecificId}', " +
                            $"type={product.type}"
                        );
                    }

                    storeController.FetchProducts(
                        products
                    );
                }
            );
        }
        catch (Exception exception)
        {
            IsReady = false;

            Debug.LogError(
                $"[IAPManager] Initialization failed: " +
                $"{exception}"
            );
        }
    }

    private void SubscribeStoreEvents()
    {
        if (storeController == null)
            return;

        storeController.OnStoreDisconnected +=
            HandleStoreDisconnected;

        storeController.OnProductsFetched +=
            HandleProductsFetched;

        storeController.OnProductsFetchFailed +=
            HandleProductsFetchFailed;

        storeController.OnPurchasesFetched +=
            HandlePurchasesFetched;

        storeController.OnPurchasesFetchFailed +=
            HandlePurchasesFetchFailed;

        storeController.OnPurchasePending +=
            HandlePurchasePending;

        storeController.OnPurchaseFailed +=
            HandlePurchaseFailed;

        storeController.OnPurchaseDeferred +=
            HandlePurchaseDeferred;
    }

    private void UnsubscribeStoreEvents()
    {
        if (storeController == null)
            return;

        storeController.OnStoreDisconnected -=
            HandleStoreDisconnected;

        storeController.OnProductsFetched -=
            HandleProductsFetched;

        storeController.OnProductsFetchFailed -=
            HandleProductsFetchFailed;

        storeController.OnPurchasesFetched -=
            HandlePurchasesFetched;

        storeController.OnPurchasesFetchFailed -=
            HandlePurchasesFetchFailed;

        storeController.OnPurchasePending -=
            HandlePurchasePending;

        storeController.OnPurchaseFailed -=
            HandlePurchaseFailed;

        storeController.OnPurchaseDeferred -=
            HandlePurchaseDeferred;
    }

    private void HandlePurchaseFailed(
        FailedOrder order)
    {
        string productId = string.Empty;

        if (order != null &&
            order.CartOrdered != null &&
            order.CartOrdered.Items().Count > 0)
        {
            Product product =
                order.CartOrdered.Items()[0].Product;

            if (product != null)
                productId = product.definition.id;
        }

        StopPurchaseTimeout(
            productId
        );

        Debug.LogError(
            $"[IAPManager] PURCHASE FAILED\n" +
            $"Product ID: '{productId}'\n" +
            $"Order: {order}"
        );

        PurchaseCompleted?.Invoke(
            IAPPurchaseResult.Failed(
                productId,
                IAPPurchaseFailureReason.PurchaseFailed,
                "Purchase was not completed."
            )
        );
    }

    private void HandlePurchaseDeferred(
        DeferredOrder order)
    {
        string productId = string.Empty;

        if (order != null &&
            order.CartOrdered != null &&
            order.CartOrdered.Items().Count > 0)
        {
            Product product =
                order.CartOrdered.Items()[0].Product;

            if (product != null)
                productId = product.definition.id;
        }

        Debug.LogWarning(
            $"[IAPManager] PURCHASE DEFERRED\n" +
            $"Product ID: '{productId}'\n" +
            $"Order: {order}"
        );

        PurchaseCompleted?.Invoke(
            IAPPurchaseResult.Failed(
                productId,
                IAPPurchaseFailureReason.PurchaseDeferred,
                "Purchase is waiting for approval."
            )
        );

        StopPurchaseTimeout(
        productId
        );
    }

    private void HandleStoreDisconnected(
        StoreConnectionFailureDescription failure)
    {
        IsReady = false;

        Debug.LogError(
            $"[IAPManager] Store disconnected: " +
            $"{failure.Message}"
        );
    }

    private void HandleProductsFetched(
        List<Product> products)
    {
        productsById.Clear();

        foreach (Product product in products)
        {
            if (product == null)
                continue;

            productsById[
                product.definition.id
            ] = product;

            Debug.Log(
                $"[IAPManager] Product fetched: " +
                $"{product.definition.id}, " +
                $"price={product.metadata.localizedPriceString}"
            );
        }

        Debug.Log(
            $"[IAPManager] Fetched {products.Count} products."
        );

        storeController.FetchPurchases();
    }

    private void HandleProductsFetchFailed(
        ProductFetchFailed failure)
    {
        IsReady = false;

        Debug.LogError(
            $"[IAPManager] PRODUCT FETCH FAILED\n" +
            $"Reason: {failure.FailureReason}\n" +
            $"Failed product count: " +
            $"{failure.FailedFetchProducts.Count}\n" +
            $"Bundle ID: {Application.identifier}"
        );

        foreach (
            ProductDefinition product
            in failure.FailedFetchProducts)
        {
            Debug.LogError(
                $"[IAPManager] Failed product:\n" +
                $"ID = '{product.id}'\n" +
                $"Store ID = '{product.storeSpecificId}'\n" +
                $"Type = {product.type}"
            );
        }
    }

    private void HandlePurchasesFetched(
        Orders orders)
    {
        IsReady = true;

        Debug.Log(
            "[IAPManager] Purchases fetched. Store ready."
        );

        Ready?.Invoke();
    }

    private void HandlePurchasesFetchFailed(
        PurchasesFetchFailureDescription failure)
    {
        // Product information is already available,
        // so we can still allow new purchases.
        IsReady = true;

        Debug.LogWarning(
            $"[IAPManager] Previous purchases could not " +
            $"be fetched: {failure}"
        );

        Ready?.Invoke();
    }

    public void PurchaseBall(
        BallType type)
    {
        if (!IsReady)
        {
            PurchaseCompleted?.Invoke(
                IAPPurchaseResult.Failed(
                    string.Empty,
                    IAPPurchaseFailureReason.NotInitialized,
                    "The store is not ready."
                )
            );

            return;
        }

        BallUnlockCatalogSO.UnlockDefinition definition =
            unlockCatalog != null
                ? unlockCatalog.GetDefinition(type)
                : null;

        if (definition == null ||
            !definition.purchasableWithIap ||
            string.IsNullOrWhiteSpace(
                definition.iapProductId
            ))
        {
            PurchaseCompleted?.Invoke(
                IAPPurchaseResult.Failed(
                    definition?.iapProductId,
                    IAPPurchaseFailureReason.ProductNotFound,
                    $"No IAP product is configured for {type}."
                )
            );

            return;
        }

        BeginStorePurchase(
            definition.iapProductId
        );
    }

    private void BeginStorePurchase(
        string productId)
    {
#if UNITY_EDITOR
        if (simulatePurchasesInEditor)
        {
            StartCoroutine(
                SimulatePurchaseRoutine(
                    productId
                )
            );

            return;
        }
#endif

        if (storeController == null ||
            !productsById.TryGetValue(
                productId,
                out Product product))
        {
            PurchaseCompleted?.Invoke(
                IAPPurchaseResult.Failed(
                    productId,
                    IAPPurchaseFailureReason.ProductNotFound,
                    "The product is not available from the store."
                )
            );

            return;
        }

        Debug.Log(
            $"[IAPManager] Starting purchase: {productId}"
        );

        Debug.Log(
            $"[IAPManager] Starting purchase:\n" +
            $"Product ID = '{productId}'\n" +
            $"Store ID = '{product.definition.storeSpecificId}'\n" +
            $"Price = '{product.metadata.localizedPriceString}'\n" +
            $"Available = {product.availableToPurchase}"
        );

        storeController.PurchaseProduct(
            product
        );

        StartPurchaseTimeout(
            productId
        );
    }

    private void StartPurchaseTimeout(
    string productId)
    {
        if (purchaseTimeoutRoutine != null)
        {
            StopCoroutine(
                purchaseTimeoutRoutine
            );
        }

        waitingForProductId =
            productId;

        purchaseTimeoutRoutine =
            StartCoroutine(
                PurchaseTimeoutRoutine(
                    productId
                )
            );
    }

    private IEnumerator PurchaseTimeoutRoutine(
        string productId)
    {
        yield return new WaitForSecondsRealtime(
            purchaseTimeoutSeconds
        );

        purchaseTimeoutRoutine = null;

        if (!string.Equals(
                waitingForProductId,
                productId,
                StringComparison.Ordinal))
        {
            yield break;
        }

        waitingForProductId =
            null;

        Debug.LogWarning(
            $"[IAPManager] Purchase timed out after " +
            $"{purchaseTimeoutSeconds} seconds: {productId}"
        );

        PurchaseCompleted?.Invoke(
            IAPPurchaseResult.Failed(
                productId,
                IAPPurchaseFailureReason.PurchaseFailed,
                "Purchase timed out. Please try again."
            )
        );
    }

    private void StopPurchaseTimeout(
    string productId)
    {
        if (!string.Equals(
                waitingForProductId,
                productId,
                StringComparison.Ordinal))
        {
            return;
        }

        waitingForProductId =
            null;

        if (purchaseTimeoutRoutine != null)
        {
            StopCoroutine(
                purchaseTimeoutRoutine
            );

            purchaseTimeoutRoutine =
                null;
        }
    }

    private void HandlePurchasePending(
        PendingOrder order)
    {
        if (order == null)
            return;

        Product purchasedProduct =
            order.CartOrdered.Items().Count > 0
                ? order.CartOrdered.Items()[0].Product
                : null;

        if (purchasedProduct == null)
        {
            Debug.LogError(
                "[IAPManager] Pending order has no product."
            );

            return;
        }

        string productId =
            purchasedProduct.definition.id;

        StopPurchaseTimeout(
            productId
        );

        Debug.Log(
            $"[IAPManager] Purchase pending: {productId}"
        );

        BallUnlockManager unlockManager =
            BallUnlockManager.Instance;

        if (unlockManager == null)
        {
            PurchaseCompleted?.Invoke(
                IAPPurchaseResult.Failed(
                    productId,
                    IAPPurchaseFailureReason.FulfillmentFailed,
                    "Unlock manager is unavailable."
                )
            );

            return;
        }

        bool granted =
            unlockManager.TryUnlockFromIap(
                productId,
                out BallType unlockedType,
                out string reason
            );

        if (!granted)
        {
            Debug.LogError(
                $"[IAPManager] Could not fulfill " +
                $"{productId}: {reason}"
            );

            PurchaseCompleted?.Invoke(
                IAPPurchaseResult.Failed(
                    productId,
                    IAPPurchaseFailureReason.FulfillmentFailed,
                    reason
                )
            );

            return;
        }

        // Real-money purchase successfully granted.
        CloudSaveManager.RegisterIapPurchase();

        AnalyticsEvents.IapPurchaseCompleted(productId,unlockedType);

        Debug.Log(
            $"[IAPManager] Granted {unlockedType}. " +
            "Confirming purchase."
        );

        storeController.ConfirmPurchase(
            order
        );

        PurchaseCompleted?.Invoke(
            IAPPurchaseResult.Succeeded(
                productId
            )
        );
    }

    public string GetLocalizedPrice(
        BallType type)
    {
        BallUnlockCatalogSO.UnlockDefinition definition =
            unlockCatalog != null
                ? unlockCatalog.GetDefinition(type)
                : null;

        if (definition == null ||
            string.IsNullOrWhiteSpace(
                definition.iapProductId
            ))
        {
            return string.Empty;
        }

        return GetStoreLocalizedPrice(
            definition.iapProductId
        );
    }

    private string GetStoreLocalizedPrice(
        string productId)
    {
#if UNITY_EDITOR
        if (simulatePurchasesInEditor)
            return simulatedLocalizedPrice;
#endif

        if (productsById.TryGetValue(
                productId,
                out Product product) &&
            product != null &&
            product.metadata != null)
        {
            return product.metadata.localizedPriceString;
        }

        return string.Empty;
    }

    private IEnumerator SimulatePurchaseRoutine(
        string productId)
    {
        if (simulatedPurchaseDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                simulatedPurchaseDelay
            );
        }

        if (simulatedPurchaseShouldFail)
        {
            PurchaseCompleted?.Invoke(
                IAPPurchaseResult.Failed(
                    productId,
                    IAPPurchaseFailureReason.PurchaseFailed,
                    "Simulated purchase failed."
                )
            );

            yield break;
        }

        BallUnlockManager unlockManager =
            BallUnlockManager.Instance;

        if (unlockManager == null)
        {
            PurchaseCompleted?.Invoke(
                IAPPurchaseResult.Failed(
                    productId,
                    IAPPurchaseFailureReason.FulfillmentFailed,
                    "Unlock manager is unavailable."
                )
            );

            yield break;
        }

        bool granted =
            unlockManager.TryUnlockFromIap(
                productId,
                out BallType unlockedType,
                out string reason
            );

        if (!granted)
        {
            PurchaseCompleted?.Invoke(
                IAPPurchaseResult.Failed(
                    productId,
                    IAPPurchaseFailureReason.FulfillmentFailed,
                    reason
                )
            );

            yield break;
        }

        Debug.Log(
            $"[IAPManager] Simulated purchase granted " +
            $"{unlockedType}."
        );

        PurchaseCompleted?.Invoke(
            IAPPurchaseResult.Succeeded(
                productId
            )
        );
    }
}