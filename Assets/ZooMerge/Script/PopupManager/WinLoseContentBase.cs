using UnityEngine;

public abstract class WinLoseContentBase : MonoBehaviour
{
    public abstract Animator Animator { get; }
    public abstract void OnShown();
}
