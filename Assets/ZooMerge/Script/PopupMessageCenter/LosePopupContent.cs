using UnityEngine;

public class LosePopupContent : MonoBehaviour, IWinLoseContent
{
    [SerializeField] private Animator animator;
    public Animator Animator => animator;

    public void OnShown()
    {
        animator.SetTrigger("Lose");
    }
}
