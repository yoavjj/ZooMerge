using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

[CreateAssetMenu(
    fileName = "IAPProductCatalog",
    menuName = "Game/IAP Product Catalog"
)]
public class IAPProductCatalogSO : ScriptableObject
{
    [Serializable]
    public class ProductDefinition
    {
        [Header("Store Product")]
        [Tooltip("Must exactly match the product ID configured in Apple / Google.")]
        public string productId;

        public ProductType productType = ProductType.Consumable;

        [Header("Reward")]
        public IAPRewardType rewardType = IAPRewardType.None;

        [Min(0)]
        public int rewardAmount;
    }

    [SerializeField] private List<ProductDefinition> products = new();

    public IReadOnlyList<ProductDefinition> Products => products;

    public ProductDefinition GetDefinition(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            return null;

        return products.Find(product =>
            product != null &&
            string.Equals(
                product.productId,
                productId,
                StringComparison.Ordinal
            )
        );
    }

    public ProductDefinition GetDefinition(IAPRewardType rewardType)
    {
        return products.Find(product =>
            product != null &&
            product.rewardType == rewardType
        );
    }
}