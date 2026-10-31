using UnityEngine;

public static class CooldownRewardProgress
{
    private const string KEY_STEP_INDEX =
        "CooldownReward_StepIndex";

    private const string KEY_FEATURE_UNLOCKED =
        "CooldownReward_FeatureUnlocked";

    private const string KEY_FTUE_SHOWN =
        "CooldownReward_FtueShown";

    public static int StepIndex
    {
        get =>
            PlayerPrefs.GetInt(
                KEY_STEP_INDEX,
                0
            );

        set =>
            PlayerPrefs.SetInt(
                KEY_STEP_INDEX,
                Mathf.Max(0, value)
            );
    }

    public static bool IsFeatureUnlocked =>
        PlayerPrefs.GetInt(
            KEY_FEATURE_UNLOCKED,
            0
        ) == 1;

    public static bool ShouldShowFtue =>
        IsFeatureUnlocked &&
        PlayerPrefs.GetInt(
            KEY_FTUE_SHOWN,
            0
        ) == 0;

    public static void UnlockFeature()
    {
        if (IsFeatureUnlocked)
            return;

        PlayerPrefs.SetInt(
            KEY_FEATURE_UNLOCKED,
            1
        );

        PlayerPrefs.SetInt(
            KEY_FTUE_SHOWN,
            0
        );

        PlayerPrefs.Save();

        Debug.Log(
            "[CooldownRewardProgress] " +
            "Coin cooldown feature unlocked."
        );
    }

    public static void MarkFtueShown()
    {
        if (!IsFeatureUnlocked)
            return;

        PlayerPrefs.SetInt(
            KEY_FTUE_SHOWN,
            1
        );

        PlayerPrefs.Save();
    }

    public static void AdvanceStepLoop(
        int stepCount)
    {
        if (stepCount <= 0)
        {
            StepIndex = 0;

            PlayerPrefs.Save();
            return;
        }

        StepIndex =
            (StepIndex + 1) %
            stepCount;

        PlayerPrefs.Save();
    }

    public static void ResetSteps()
    {
        StepIndex = 0;

        PlayerPrefs.Save();
    }

#if UNITY_EDITOR

    public static void DebugResetFeature()
    {
        PlayerPrefs.DeleteKey(
            KEY_FEATURE_UNLOCKED
        );

        PlayerPrefs.DeleteKey(
            KEY_FTUE_SHOWN
        );

        PlayerPrefs.DeleteKey(
            KEY_STEP_INDEX
        );

        PlayerPrefs.Save();

        Debug.Log(
            "[CooldownRewardProgress] " +
            "Feature progress reset."
        );
    }

#endif
}