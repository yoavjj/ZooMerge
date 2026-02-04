using UnityEngine;

public class TopBarMergeItemUI : TopBarCurrencyItemUI
{
    public BallType Type { get; private set; }

    public void Initialize(BallType type, Sprite icon, int startCount)
    {
        Type = type;
        base.Initialize(icon, startCount);
    }

    public void AddOne()
    {
        SetCount(count + 1);
    }
}
