using System.Collections.Generic;
using UnityEngine;

public static class SpaceshipSkinProgress
{
    private const string KEY_CURRENT_SKIN = "SHIP_CURRENT_SKIN";
    private const string KEY_UNLOCK_PREFIX = "SHIP_UNLOCKED_";
    private const string KEY_UNLOCKED_IDS = "SHIP_UNLOCKED_IDS";

    public const string DefaultSkinId = "spaceship_01";

    public static string CurrentSkinId
    {
        get => PlayerPrefs.GetString(KEY_CURRENT_SKIN, DefaultSkinId);

        set
        {
            string skinId =
                string.IsNullOrWhiteSpace(value)
                    ? DefaultSkinId
                    : value;

            PlayerPrefs.SetString(KEY_CURRENT_SKIN, skinId);
        }
    }

    public static bool IsUnlocked(string skinId)
    {
        if (string.IsNullOrWhiteSpace(skinId))
            return false;

        if (skinId == DefaultSkinId)
            return true;

        return PlayerPrefs.GetInt(
            KEY_UNLOCK_PREFIX + skinId,
            0
        ) == 1;
    }

    public static void Unlock(string skinId)
    {
        if (string.IsNullOrWhiteSpace(skinId))
            return;

        PlayerPrefs.SetInt(
            KEY_UNLOCK_PREFIX + skinId,
            1
        );

        string savedIds =
            PlayerPrefs.GetString(
                KEY_UNLOCKED_IDS,
                string.Empty
            );

        List<string> ids =
            new List<string>();

        if (!string.IsNullOrWhiteSpace(savedIds))
            ids.AddRange(savedIds.Split('|'));

        if (!ids.Contains(skinId))
        {
            ids.Add(skinId);

            PlayerPrefs.SetString(
                KEY_UNLOCKED_IDS,
                string.Join("|", ids)
            );
        }

        PlayerPrefs.Save();
    }

    public static void UnlockAndSelect(string skinId)
    {
        if (string.IsNullOrWhiteSpace(skinId))
            return;

        Unlock(skinId);

        CurrentSkinId = skinId;

        PlayerPrefs.Save();
    }

    public static Dictionary<string, bool> GetUnlockedSkins()
    {
        Dictionary<string, bool> result =
            new Dictionary<string, bool>
            {
                { DefaultSkinId, true }
            };

        string savedIds =
            PlayerPrefs.GetString(
                KEY_UNLOCKED_IDS,
                string.Empty
            );

        if (string.IsNullOrWhiteSpace(savedIds))
            return result;

        string[] ids =
            savedIds.Split('|');

        foreach (string skinId in ids)
        {
            if (string.IsNullOrWhiteSpace(skinId))
                continue;

            result[skinId] =
                IsUnlocked(skinId);
        }

        return result;
    }

    public static void RestoreFromCloud(
        string currentSkinId,
        Dictionary<string, bool> unlockedSkins)
    {
        List<string> ids =
            new List<string>();

        if (unlockedSkins != null)
        {
            foreach (
                KeyValuePair<string, bool> pair
                in unlockedSkins)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    continue;

                PlayerPrefs.SetInt(
                    KEY_UNLOCK_PREFIX + pair.Key,
                    pair.Value ? 1 : 0
                );

                if (pair.Value)
                    ids.Add(pair.Key);
            }
        }

        if (!ids.Contains(DefaultSkinId))
            ids.Add(DefaultSkinId);

        PlayerPrefs.SetString(
            KEY_UNLOCKED_IDS,
            string.Join("|", ids)
        );

        if (!string.IsNullOrWhiteSpace(currentSkinId))
            CurrentSkinId = currentSkinId;

        PlayerPrefs.Save();
    }
}