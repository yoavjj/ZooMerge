using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI Buttons")]
    [SerializeField] private Button playButton;

    private void Awake()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayPressed);
    }

    private void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayPressed);
    }

    private void OnPlayPressed()
    {
        // Inform progression system
        MergeLevelManager.SetLevel(MergeLevelManager.CurrentLevelNumber);

        // Determine if we are starting from the beginning of the level
        bool isNewLevel = MergeLevelManager.CurrentEnemyIndex == 0;

        // Centralized session begin
        PopupManager.Instance?.BeginSession(isNewLevel);

        // Initialize the progress bar on the PopupManager's slider
        PopupManager.Instance?.InitializeProgressBarNow();

        Destroy(gameObject);
    }
}
