using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameInventory
{
    public static Inventory Instance { get; } =
        new Inventory();
}

public sealed class Inventory
{
    private readonly Dictionary<BallType, int>
        ballValues = new();

    private readonly Dictionary<CurrencyType, int>
        currencyValues = new();

    public event Action OnChanged;
    public event Action OnCoinsReduced;

    public void NotifyChanged()
    {
        OnChanged?.Invoke();
    }

    // ------------ BALLS ------------
    public int Get(BallType type)
    {
        return ballValues.TryGetValue(
            type,
            out int value)
                ? value
                : 0;
    }

    public void Add(BallType type, int amount)
    {
        if (amount <= 0)
            return;

        ballValues[type] =
            Get(type) + amount;

        PlayerPrefs.SetInt(
            GetBallKey(type),
            ballValues[type]
        );

        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    public bool Spend(
        BallType type,
        int amount,
        bool notify = true)
    {
        if (amount <= 0)
            return true;

        int current = Get(type);

        if (current < amount)
            return false;

        ballValues[type] =
            current - amount;

        PlayerPrefs.SetInt(
            GetBallKey(type),
            ballValues[type]
        );

        PlayerPrefs.Save();

        if (notify)
            OnChanged?.Invoke();

        return true;
    }

    public IReadOnlyDictionary<BallType, int>
        Snapshot()
    {
        return ballValues;
    }

    // ------------ CURRENCY ------------
    public int Get(CurrencyType type)
    {
        return currencyValues.TryGetValue(
            type,
            out int value)
                ? value
                : 0;
    }

    public void Add(
        CurrencyType type,
        int amount)
    {
        if (amount <= 0)
            return;

        currencyValues[type] =
            Get(type) + amount;

        PlayerPrefs.SetInt(
            GetCurrencyKey(type),
            currencyValues[type]
        );

        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    public bool Spend(
        CurrencyType type,
        int amount,
        bool notify = true)
    {
        if (amount <= 0)
            return true;

        int current = Get(type);

        if (current < amount)
            return false;

        currencyValues[type] =
            current - amount;

        PlayerPrefs.SetInt(
            GetCurrencyKey(type),
            currencyValues[type]
        );

        PlayerPrefs.Save();

        if (notify)
        {
            OnChanged?.Invoke();

            if (type == CurrencyType.Coins)
                OnCoinsReduced?.Invoke();
        }

        return true;
    }

    public IReadOnlyDictionary<CurrencyType, int>
        CurrencySnapshot()
    {
        return currencyValues;
    }

    // ------------ LOAD ON START ------------
    public void LoadFromPrefs()
    {
        foreach (
            BallType type
            in Enum.GetValues(typeof(BallType)))
        {
            ballValues[type] =
                PlayerPrefs.GetInt(
                    GetBallKey(type),
                    0
                );
        }

        foreach (
            CurrencyType type
            in Enum.GetValues(typeof(CurrencyType)))
        {
            currencyValues[type] =
                PlayerPrefs.GetInt(
                    GetCurrencyKey(type),
                    0
                );
        }

        OnChanged?.Invoke();
    }

    public void ResetAll()
    {
        ballValues.Clear();
        currencyValues.Clear();

        foreach (
            BallType type
            in Enum.GetValues(typeof(BallType)))
        {
            PlayerPrefs.DeleteKey(
                GetBallKey(type)
            );
        }

        foreach (
            CurrencyType type
            in Enum.GetValues(typeof(CurrencyType)))
        {
            PlayerPrefs.DeleteKey(
                GetCurrencyKey(type)
            );
        }

        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    private static string GetBallKey(
        BallType type)
    {
        return $"INV_BALL_{type}";
    }

    private static string GetCurrencyKey(
        CurrencyType type)
    {
        return $"INV_CUR_{type}";
    }
}

public enum CurrencyType
{
    Coins
}