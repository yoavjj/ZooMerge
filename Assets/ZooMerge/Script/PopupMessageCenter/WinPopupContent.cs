using UnityEngine;

public class WinPopupContent : WinLoseContentBase
{
    [SerializeField] private Animator animator;

    public override Animator Animator => animator;

    public override void OnShown()
    {
        animator.SetTrigger("Win");
    }
}