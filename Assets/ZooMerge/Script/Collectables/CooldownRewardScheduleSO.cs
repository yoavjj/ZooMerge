using UnityEngine;

[CreateAssetMenu(menuName = "Game/Economy/Cooldown Reward Schedule", fileName = "CooldownRewardSchedule")]
public class CooldownRewardScheduleSO : ScriptableObject
{
    [System.Serializable]
    public struct Step
    {
        [Min(1)] public int cooldownSeconds; // how long to wait
        [Min(0)] public int rewardCoins;     // coins granted on collect
    }

    [Tooltip("Steps in order. After each collect you move to the next step.")]
    public Step[] steps =
    {
        new Step { cooldownSeconds = 600, rewardCoins = 2 },  // 10 min
        new Step { cooldownSeconds = 900, rewardCoins = 3 },  // 15 min
        new Step { cooldownSeconds = 1200, rewardCoins = 4 }, // 20 min
    };

    [Header("Overflow (if player exceeds steps length)")]
    [Min(1)] public int overflowCooldownSeconds = 1800; // 30 min
    [Min(0)] public int overflowRewardCoins = 5;

    public Step GetStep(int index)
    {
        if (steps != null && steps.Length > 0)
        {
            if (index < 0) index = 0;
            if (index < steps.Length) return steps[index];
        }

        return new Step { cooldownSeconds = overflowCooldownSeconds, rewardCoins = overflowRewardCoins };
    }

    public int MaxStepCount => steps != null ? steps.Length : 0;
}