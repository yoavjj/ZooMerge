using System;
using System.Collections.Generic;
using UnityEngine;

public class BallChoiceMenu : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BallSet ballSet;

    [Header("UI")]
    [SerializeField] private Transform container;
    [SerializeField] private BallChoiceItemUI itemPrefab;

    private readonly Dictionary<BallType, BallChoiceItemUI> itemsByType = new();

    public void Build()
    {
        Clear();

        foreach (BallType type in Enum.GetValues(typeof(BallType)))
        {
            CreateItem(type);
        }
    }

    private void CreateItem(BallType type)
    {
        if (itemPrefab == null || container == null)
            return;

        BallChoiceItemUI item = Instantiate(
            itemPrefab,
            container
        );

        Sprite profileSprite = ballSet != null
            ? ballSet.GetProfileSprite(type)
            : null;

        item.Initialize(type, profileSprite);

        itemsByType[type] = item;
    }

    public void RefreshAll()
    {
        foreach (BallChoiceItemUI item in itemsByType.Values)
        {
            if (item != null)
                item.Refresh();
        }
    }

    private void Clear()
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        itemsByType.Clear();
    }
}