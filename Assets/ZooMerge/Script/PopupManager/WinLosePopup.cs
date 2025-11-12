using TMPro;
using UnityEngine;
using static BallEventManager;

public class WinLosePopup : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI levelMessageText;
    [SerializeField] private TextMeshProUGUI playButtonText;
    [SerializeField] private Animator animator;

    private bool isContinue = false;

    private GameOverReason currentReason;

    public void SetMessage(string msg)
    {
        if (messageText != null)
            messageText.text = msg;
    }

    public void SetLevelMessage(int currentLevel, GameOverReason reason)
    {
        if (levelMessageText == null || playButtonText == null) return;

        currentReason = reason;

        switch (reason)
        {
            case GameOverReason.Won:
                levelMessageText.text = $"Level {currentLevel} Complete!\nNext: Level {currentLevel + 1}";
                playButtonText.text = $"Next Level {currentLevel + 1}";
                break;

            case GameOverReason.Lost:
                levelMessageText.text = $"Try Again: Level {currentLevel}";
                playButtonText.text = $"Restart Level {currentLevel}";
                break;

            default:
                levelMessageText.text = $"Level {currentLevel}";
                playButtonText.text = $"Play Level {currentLevel}";
                break;
        }
    }

    public void ShowContinueOption()
    {
        SetMessage("Try Again?");
        SetContinueMessageAfterFailure();

        isContinue = true;
    }

    public void OnMainMenuButtonPressed()
    {
        PopupManager.Instance?.ShowMainMenu();
        animator.SetTrigger("Out");
        Destroy(gameObject, 1.5f);
    }

    public void OnPlayPressed()
    {
        bool isNewLevel = currentReason == GameOverReason.Won;

        if (isContinue)
        {
            var dropped = CircleDragInput.Instance?.droppedContainer;
            if (dropped != null)
                BallStateSaver.Instance.RestoreState(dropped);
            BallEventManager.RaiseSessionStarted();
            isContinue = false;
        }
        else
        {
            PopupManager.Instance?.OnPlayButtonPressed(isNewLevel);
        }

        animator.SetTrigger("Out");
        Destroy(gameObject, 1.5f);
    }

    public void SetTemporaryMessage()
    {
        if (playButtonText != null)
            playButtonText.text = "Continue";

        if (levelMessageText != null)
        {
            int currentLevel = MergeLevelManager.CurrentLevelNumber;
            int currentEnemy = MergeLevelManager.CurrentEnemyIndex + 1; // next enemy index
            int totalEnemies = MergeLevelManager.TotalEnemiesInLevel;

            levelMessageText.text = $"Next: Enemy {currentEnemy}/{totalEnemies}\nLevel {currentLevel}";
        }
    }

    private void SetContinueMessageAfterFailure()
    {
        if (playButtonText != null)
            playButtonText.text = "Continue";

        if (levelMessageText != null)
        {
            int currentLevel = MergeLevelManager.CurrentLevelNumber;
            levelMessageText.text = $"You can retry from here\nLevel {currentLevel}";
        }
    }

    private void AutoClose()
    {
        animator.SetTrigger("Out");
        Destroy(gameObject, 1.5f);
    }
}
