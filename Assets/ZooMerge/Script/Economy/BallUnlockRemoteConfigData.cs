using System;
using System.Collections.Generic;

[Serializable]
public class BallUnlockRemoteConfigData
{
    public List<BallUnlockRemoteDefinition> definitions = new();
}

[Serializable]
public class BallUnlockRemoteDefinition
{
    public string type;
    public int coinCost;

    public List<BallUnlockRemoteMergeRequirement>
        mergeRequirements = new();
}

[Serializable]
public class BallUnlockRemoteMergeRequirement
{
    public string type;
    public int requiredAmount;
}