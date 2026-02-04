using System;
using System.Collections.Generic;

public static class GameInventory
{
    public static Inventory Instance { get; } = new Inventory();
}

public sealed class Inventory
{
    private readonly Dictionary<BallType, int> ballValues = new();
    private readonly Dictionary<CurrencyType, int> currencyValues = new();

    // 🔵 BALL TYPE HANDLING
    public int Get(BallType type)
        => ballValues.TryGetValue(type, out var v) ? v : 0;

    public void Add(BallType type, int amount)
    {
        if (amount <= 0) return;

        if (!ballValues.ContainsKey(type))
            ballValues[type] = 0;

        ballValues[type] += amount;
    }

    public IReadOnlyDictionary<BallType, int> Snapshot()
        => ballValues;

    // 🟡 CURRENCY HANDLING (Coins, etc.)
    public int Get(CurrencyType type)
        => currencyValues.TryGetValue(type, out var v) ? v : 0;

    public void Add(CurrencyType type, int amount)
    {
        if (amount <= 0) return;

        if (!currencyValues.ContainsKey(type))
            currencyValues[type] = 0;

        currencyValues[type] += amount;
    }

    public IReadOnlyDictionary<CurrencyType, int> CurrencySnapshot()
        => currencyValues;
}

public enum CurrencyType
{
    Coins
}
