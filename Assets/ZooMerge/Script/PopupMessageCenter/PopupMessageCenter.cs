using UnityEngine;
using static BallEventManager;

public static class PopupMessageCenter
{
    public static void ShowEndPopupMessage(WinLosePopup popup, GameOverReason reason)
    {
        if (popup == null) return;

        string msg = reason switch
        {
            GameOverReason.Won => "You Won!",
            GameOverReason.Lost => "Game Over",
            _ => "Game Over"
        };

        popup.SetMessage(msg);
        popup.SetLevelMessage(MergeLevelManager.CurrentLevelNumber, reason);

        if (reason == GameOverReason.Lost && MergeLevelManager.CurrentEnemyIndex > 0)
        {
            popup.ShowContinueOption();
        }

        if (reason == GameOverReason.Won)
        {
            MergeLevelManager.AdvanceLevel();
        }
    }

    public static void ShowEnemyDefeated(WinLosePopup popup)
    {
        if (popup == null) return;

        popup.SetMessage("Enemy Defeated!");
        popup.SetLevelMessage(MergeLevelManager.CurrentLevelNumber, GameOverReason.Won);
        popup.SetTemporaryMessage();
    }
}
