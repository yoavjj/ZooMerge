using System.Collections.Generic;

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

    public static void ResetAll()
    {
        usedOrders.Clear();
        freedOrders.Clear();
        currentMax = minOrder - 1;
    }
}
