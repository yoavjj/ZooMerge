using UnityEngine;

public class WinPopupContent : MonoBehaviour, IWinLoseContent
{
    [SerializeField] private Animator animator;
    public Animator Animator => animator;

    public void OnShown()
    {
        animator.SetTrigger("Win");
    }
}
