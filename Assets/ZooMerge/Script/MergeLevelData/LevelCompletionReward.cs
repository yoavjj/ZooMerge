using System;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[JsonConverter(typeof(StringEnumConverter))]
[Flags]
public enum LevelRewardType
{
    None = 0,
    Heart = 1 << 0,
    BallUnlockType = 1 << 1,
    SpaceshipSkin = 1 << 2,
    CoinCooldownFeature = 1 << 3,
    BallUnlockFeature = 1 << 4
}

[Serializable]
public class LevelCompletionReward
{
    public LevelRewardType rewardType =
        LevelRewardType.None;

    [Min(1)]
    public int amount = 1;

    public BallType ballType;

    public string spaceshipSkinId;

    public bool allowReject = true;
}