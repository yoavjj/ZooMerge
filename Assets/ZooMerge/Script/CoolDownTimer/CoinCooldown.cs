using System;
using UnityEngine;

public static class CoinCooldown
{
    private const string KEY_END_UTC_TICKS = "CoinCooldown_EndUtcTicks";

    public static int CooldownSeconds { get; set; } = 300;
    public static int RewardCoins { get; set; } = 10;

    public static void EnsureInitialized()
    {
        if (!PlayerPrefs.HasKey(KEY_END_UTC_TICKS))
        {
            RestartCooldown();
        }
    }

    public static long GetEndTicks()
    {
        EnsureInitialized();
        var s = PlayerPrefs.GetString(KEY_END_UTC_TICKS, "0");
        return long.TryParse(s, out var t) ? t : 0;
    }

    public static TimeSpan GetRemaining()
    {
        long endTicks = GetEndTicks();
        long now = DateTime.UtcNow.Ticks;
        long remaining = endTicks - now;
        return remaining <= 0 ? TimeSpan.Zero : new TimeSpan(remaining);
    }

    public static bool IsReadyToCollect()
        => GetRemaining() <= TimeSpan.Zero;

    public static string GetRemainingText()
    {
        var r = GetRemaining();
        if (r <= TimeSpan.Zero) return "00:00";

        int totalMinutes = (int)r.TotalMinutes;
        return $": {totalMinutes:00}:{r.Seconds:00}";
    }

    public static void RestartCooldown()
    {
        long end = DateTime.UtcNow.AddSeconds(CooldownSeconds).Ticks;
        PlayerPrefs.SetString(KEY_END_UTC_TICKS, end.ToString());
        PlayerPrefs.Save();
    }

    public static void Debug_SetRemainingSeconds(int seconds)
    {
        seconds = Mathf.Max(0, seconds);
        long end = DateTime.UtcNow.AddSeconds(seconds).Ticks;
        PlayerPrefs.SetString("CoinCooldown_EndUtcTicks", end.ToString());
        PlayerPrefs.Save();
    }
}