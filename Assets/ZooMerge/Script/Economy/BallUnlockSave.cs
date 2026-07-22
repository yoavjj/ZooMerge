using System;
using UnityEngine;

public static class BallUnlockSave
{
    private const string UnlockKeyPrefix =
        "BALL_UNLOCKED_";

    public static bool IsUnlocked(BallType type)
    {
        return PlayerPrefs.GetInt(
            GetUnlockKey(type),
            0
        ) == 1;
    }

    public static void SetUnlocked(
        BallType type,
        bool unlocked)
    {
        PlayerPrefs.SetInt(
            GetUnlockKey(type),
            unlocked ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    public static void ResetUnlock(BallType type)
    {
        PlayerPrefs.DeleteKey(
            GetUnlockKey(type)
        );

        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        foreach (
            BallType type
            in Enum.GetValues(typeof(BallType)))
        {
            PlayerPrefs.DeleteKey(
                GetUnlockKey(type)
            );
        }

        PlayerPrefs.Save();
    }

    public static int GetRawSavedValue(
        BallType type)
    {
        return PlayerPrefs.GetInt(
            GetUnlockKey(type),
            0
        );
    }

    private static string GetUnlockKey(
        BallType type)
    {
        return $"{UnlockKeyPrefix}{type}";
    }
}