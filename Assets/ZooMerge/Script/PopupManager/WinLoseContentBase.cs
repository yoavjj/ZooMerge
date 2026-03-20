using UnityEngine;

public abstract class WinLoseContentBase : MonoBehaviour, IWinLoseContent
{
    public abstract Animator Animator { get; }
    public abstract void OnShown();
}
