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
        BallEventManager.RaiseSessionStarted();
        PopupManager.Instance?.OnPlayButtonPressed();
        BallEventManager.RaiseResetCounters();
        Destroy(gameObject);
    }
}
