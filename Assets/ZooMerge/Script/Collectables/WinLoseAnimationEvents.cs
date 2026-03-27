using UnityEngine;

public class WinLoseAnimationEvents : MonoBehaviour
{
    [SerializeField] MergeSummaryPanel mergeSummaryPanel;
    public void TriggerSpawnWinCoins()
    {
        if (WinLosePopup.Instance != null)
            WinLosePopup.Instance.SpawnWinCoins();
    } 

    public void TriggerMergeSummaryAnimateIn()
    {
        if (mergeSummaryPanel != null)
        {
            mergeSummaryPanel.OnPopupOpenAnimationFinished();
        }
    }

    public void AE_OnRevealFinished()
    {
        PopupManager.Instance?.BeginSession(isNewLevel: false);
        PopupManager.Instance?.InitializeProgressBarNow();
    }
}
