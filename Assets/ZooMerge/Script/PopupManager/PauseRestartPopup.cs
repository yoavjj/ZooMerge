using UnityEngine;
using static BallEventManager;

public class PauseRestartPopup : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private void OnEnable()
    {
        BallEventManager.OnGameOver += HandleGameOver;
        BallEventManager.OnEnemySessionEnded += HandleEnemySessionEnded;
    }

    private void OnDisable()
    {
        BallEventManager.OnGameOver -= HandleGameOver;
        BallEventManager.OnEnemySessionEnded -= HandleEnemySessionEnded;
    }

    private void HandleGameOver(BallInfo _, GameOverReason __)
    {
        // Simulate resume press
        OnResumeButtonPressed();
    }

    private void HandleEnemySessionEnded()
    {
        OnResumeButtonPressed();
    }

    public void OnResumeButtonPressed()
    {
        animator.SetTrigger("Out");
        Destroy(gameObject, 1f);
        PopupManager.Instance.ClearPausePopupReference();
    }

    public void OnMainMenuButtonPressed()
    {
        PopupManager.Instance?.ConfirmReturnToMainMenu();
        animator.SetTrigger("Out");
        Destroy(gameObject, 1f);
        PopupManager.Instance.ClearPausePopupReference();
    }

    public void OnRestartSessionPressed()
    {
        var dropped = CircleDragInput.Instance?.droppedContainer;
        if (dropped != null)
        {
            BallStateSaver.Instance.RestoreState(dropped);
        }

        PopupManager.Instance?.BeginSession(isNewLevel: false, restartmidlevel: true);

        animator.SetTrigger("Out");
        Destroy(gameObject, 1.5f);
        PopupManager.Instance?.ClearPausePopupReference();
    }
}
