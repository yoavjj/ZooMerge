using UnityEngine;

public static class BallUnlockFeatureProgress
{
    private const string KEY_FEATURE_UNLOCKED = "BallUnlockFeature_Unlocked";
    private const string KEY_FTUE_SHOWN = "BallUnlockFeature_FtueShown";

    public static bool IsUnlocked => PlayerPrefs.GetInt(KEY_FEATURE_UNLOCKED, 0) == 1;

    public static bool ShouldShowFtue =>
        IsUnlocked &&
        PlayerPrefs.GetInt(KEY_FTUE_SHOWN, 0) == 0;

    public static void UnlockFeature()
    {
        if (IsUnlocked)
            return;

        PlayerPrefs.SetInt(KEY_FEATURE_UNLOCKED, 1);
        PlayerPrefs.SetInt(KEY_FTUE_SHOWN, 0);
        PlayerPrefs.Save();

        Debug.Log("[BallUnlockFeatureProgress] Ball Unlock feature unlocked.");
    }

    public static void MarkFtueShown()
    {
        if (!IsUnlocked)
            return;

        PlayerPrefs.SetInt(KEY_FTUE_SHOWN, 1);
        PlayerPrefs.Save();

        Debug.Log("[BallUnlockFeatureProgress] FTUE marked as shown.");
    }

    public static void ResetFeature()
    {
        PlayerPrefs.DeleteKey(KEY_FEATURE_UNLOCKED);
        PlayerPrefs.DeleteKey(KEY_FTUE_SHOWN);
        PlayerPrefs.Save();

        Debug.Log("[BallUnlockFeatureProgress] Ball Unlock feature reset.");
    }
}