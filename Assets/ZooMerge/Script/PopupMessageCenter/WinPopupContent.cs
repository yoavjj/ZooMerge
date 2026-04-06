using UnityEngine;

public class WinPopupContent : WinLoseContentBase
{
    [SerializeField] private Animator animator;
    [SerializeField] private LevelArtController levelArtController;

    public override Animator Animator => animator;

    public override void OnShown()
    {
        animator.SetTrigger("Win");
    }

    void Start()
    {
        levelArtController.Refresh();
    }
}