using System.Collections.Generic;
using UnityEngine;

public static class SpineSortingOrderManager
{
    private static SortedSet<int> usedOrders = new SortedSet<int>();
    private static Stack<int> freedOrders = new Stack<int>();
    private static int minOrder = 5;
    private static int currentMax = minOrder - 1;

    public static int GetNextOrder()
    {
        int next;
        if (freedOrders.Count > 0)
        {
            next = freedOrders.Pop();
        }
        else
        {
            currentMax++;
            next = currentMax;
        }

        usedOrders.Add(next);
        return next;
    }

    public static void ReleaseOrder(int order)
    {
        if (usedOrders.Contains(order))
        {
            usedOrders.Remove(order);
            freedOrders.Push(order);
        }
    }

    public static void ClaimOrder(int order)
    {
        usedOrders.Add(order);

        // Keep currentMax updated so we don't accidentally reuse this number later
        currentMax = Mathf.Max(currentMax, order);
    }
    
    public static void ResetAll()
    {
        usedOrders.Clear();
        freedOrders.Clear();
        currentMax = minOrder - 1;
    }
}
