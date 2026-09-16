using UnityEngine;

public class WinLosePopupContent : WinLoseContentBase
{
    [SerializeField] private Animator animator;
    [SerializeField] private LevelArtController levelArtController;

    public override Animator Animator => animator;

    public override void OnShown()
    {
        if (animator == null)
            return;

        animator.ResetTrigger("Win");
        animator.SetTrigger("Win");
    }

    private void Start()
    {
        if (levelArtController == null)
            return;
            
        levelArtController?.Refresh();
    }

    public void PlayOut()
    {
        if (animator == null)
            return;

        animator.ResetTrigger("Out");
        animator.SetTrigger("Out");
    }
}