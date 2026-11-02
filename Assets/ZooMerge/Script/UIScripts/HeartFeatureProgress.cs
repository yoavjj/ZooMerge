using UnityEngine;

public static class HeartFeatureProgress
{
    private const string KEY_FEATURE_UNLOCKED = "HeartFeature_Unlocked";
    private const string KEY_FTUE_SHOWN = "HeartFeature_FtueShown";

    public static bool IsFeatureUnlocked => PlayerPrefs.GetInt(KEY_FEATURE_UNLOCKED, 0) == 1;

    public static bool ShouldShowFtue => IsFeatureUnlocked && PlayerPrefs.GetInt(KEY_FTUE_SHOWN, 0) == 0;

    public static void UnlockFeature()
    {
        if (IsFeatureUnlocked)
            return;

        PlayerPrefs.SetInt(KEY_FEATURE_UNLOCKED, 1);
        PlayerPrefs.SetInt(KEY_FTUE_SHOWN, 0);
        PlayerPrefs.Save();

        Debug.Log("[HeartFeatureProgress] Heart feature unlocked.");
    }

    public static void MarkFtueShown()
    {
        if (!IsFeatureUnlocked)
            return;

        PlayerPrefs.SetInt(KEY_FTUE_SHOWN, 1);
        PlayerPrefs.Save();

        Debug.Log("[HeartFeatureProgress] Heart FTUE marked as shown.");
    }

    public static void ResetFeature()
    {
        PlayerPrefs.DeleteKey(KEY_FEATURE_UNLOCKED);
        PlayerPrefs.DeleteKey(KEY_FTUE_SHOWN);
        PlayerPrefs.Save();

        Debug.Log("[HeartFeatureProgress] Heart feature reset.");
    }
}