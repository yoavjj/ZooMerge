using UnityEngine;

public class LosePopupContent : WinLoseContentBase
{
    [SerializeField] private Animator animator;

    public override Animator Animator => animator;

    public override void OnShown()
    {
        animator.SetTrigger("Lose");
    }
}