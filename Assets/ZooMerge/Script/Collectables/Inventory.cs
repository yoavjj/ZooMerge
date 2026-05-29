using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameInventory
{
    public static Inventory Instance { get; } = new Inventory();
}

public sealed class Inventory
{
    private readonly Dictionary<BallType, int> ballValues = new();
    private readonly Dictionary<CurrencyType, int> currencyValues = new();

    public event Action OnChanged;

    // ------------ BALLS ------------
    public int Get(BallType type)
        => ballValues.TryGetValue(type, out var v) ? v : 0;

    public void Add(BallType type, int amount)
    {
        if (amount <= 0) return;

        ballValues[type] = Get(type) + amount;

        // Persist immediately (simple + safe)
        PlayerPrefs.SetInt(GetBallKey(type), ballValues[type]);
        PlayerPrefs.Save();

        OnChanged?.Invoke();
    }

    public IReadOnlyDictionary<BallType, int> Snapshot()
        => ballValues;

    // ------------ CURRENCY ------------
    public int Get(CurrencyType type)
        => currencyValues.TryGetValue(type, out var v) ? v : 0;

    public void Add(CurrencyType type, int amount)
    {
        if (amount <= 0) return;

        currencyValues[type] = Get(type) + amount;

        PlayerPrefs.SetInt(GetCurrencyKey(type), currencyValues[type]);
        PlayerPrefs.Save();

        OnChanged?.Invoke();
    }

    public IReadOnlyDictionary<CurrencyType, int> CurrencySnapshot()
        => currencyValues;

    // ------------ LOAD ON START ------------
    public void LoadFromPrefs()
    {
        foreach (BallType t in Enum.GetValues(typeof(BallType)))
            ballValues[t] = PlayerPrefs.GetInt(GetBallKey(t), 0);

        foreach (CurrencyType t in Enum.GetValues(typeof(CurrencyType)))
            currencyValues[t] = PlayerPrefs.GetInt(GetCurrencyKey(t), 0);

        OnChanged?.Invoke();
    }

    private static string GetBallKey(BallType t) => $"INV_BALL_{t}";
    private static string GetCurrencyKey(CurrencyType t) => $"INV_CUR_{t}";

    public void ResetAll()
    {
        ballValues.Clear();
        currencyValues.Clear();

        // Clear persisted keys for balls
        foreach (BallType t in Enum.GetValues(typeof(BallType)))
            PlayerPrefs.DeleteKey($"INV_BALL_{t}");

        // Clear persisted keys for currency
        foreach (CurrencyType t in Enum.GetValues(typeof(CurrencyType)))
            PlayerPrefs.DeleteKey($"INV_CUR_{t}");

        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    public bool Spend(CurrencyType type, int amount)
    {
        if (amount <= 0) return true;

        int current = Get(type);
        if (current < amount)
            return false;

        currencyValues[type] = current - amount;

        PlayerPrefs.SetInt(GetCurrencyKey(type), currencyValues[type]);
        PlayerPrefs.Save();

        OnChanged?.Invoke();
        return true;
    }
}

public enum CurrencyType
{
    Coins
}
