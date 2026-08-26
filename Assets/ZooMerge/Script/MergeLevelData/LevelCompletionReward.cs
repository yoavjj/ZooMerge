using System;
using UnityEngine;

[Flags]
public enum LevelRewardType
{
    None = 0,
    Heart = 1 << 0,
    BallUnlock = 1 << 1,
    SpaceshipSkin = 1 << 2
}

[Serializable]
public class LevelCompletionReward
{
    public LevelRewardType rewardType = LevelRewardType.None;

    [Min(1)]
    public int amount = 1;

    public BallType ballType;

    public string spaceshipSkinId;
}