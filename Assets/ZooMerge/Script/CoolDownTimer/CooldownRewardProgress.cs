using UnityEngine;

public static class CooldownRewardProgress
{
    private const string KEY_STEP_INDEX = "CooldownReward_StepIndex";

    public static int StepIndex
    {
        get => PlayerPrefs.GetInt(KEY_STEP_INDEX, 0);
        set => PlayerPrefs.SetInt(KEY_STEP_INDEX, Mathf.Max(0, value));
    }

    public static void AdvanceStepLoop(int stepCount)
    {
        if (stepCount <= 0)
        {
            StepIndex = 0;
            PlayerPrefs.Save();
            return;
        }

        StepIndex = (StepIndex + 1) % stepCount;
        PlayerPrefs.Save();
    }

    public static void ResetSteps()
    {
        StepIndex = 0;
        PlayerPrefs.Save();
    }
}