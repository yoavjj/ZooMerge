using UnityEngine;
using UnityEngine.UI;

public class CollectFTUEPopup : MonoBehaviour
{
    public static CollectFTUEPopup LastSpawned { get; private set; }

    [Header("UI")]
    [SerializeField] private Button goToMainMenuButton;
    [SerializeField] private Button closeButton;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string outTrigger = "Out";

    private WinLosePopup winLosePopup;
    private bool isClosing;

    private CloseAction closeAction;

    private enum CloseAction
    {
        None,
        GoToMainMenu,
        CloseOnly
    }

    private void Awake()
    {
        LastSpawned = this;

        if (goToMainMenuButton != null)
            goToMainMenuButton.onClick.AddListener(OnGoToMainMenuPressed);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClosePressed);
    }

    private void OnDestroy()
    {
        if (goToMainMenuButton != null)
            goToMainMenuButton.onClick.RemoveListener(OnGoToMainMenuPressed);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClosePressed);

        if (LastSpawned == this)
            LastSpawned = null;
    }

    public void Initialize(WinLosePopup owner)
    {
        winLosePopup = owner;
    }

    private void OnGoToMainMenuPressed()
    {
        BeginClose(CloseAction.GoToMainMenu);
    }

    public void OnClosePressed()
    {
        BeginClose(CloseAction.CloseOnly);
    }

    private void BeginClose(CloseAction action)
    {
        if (isClosing)
            return;

        isClosing = true;
        closeAction = action;

        if (goToMainMenuButton != null)
            goToMainMenuButton.interactable = false;

        if (closeButton != null)
            closeButton.interactable = false;

        if (animator != null && !string.IsNullOrEmpty(outTrigger))
        {
            animator.ResetTrigger(outTrigger);
            animator.SetTrigger(outTrigger);
            return;
        }

        AE_OutFinished();
    }

    public void AE_OutFinished()
    {
        WinLosePopup owner = winLosePopup;
        winLosePopup = null;

        CloseAction action = closeAction;
        closeAction = CloseAction.None;

        if (action == CloseAction.GoToMainMenu)
        {
            if (owner != null)
                owner.OnMainMenuButtonPressed();
        }
        else if (action == CloseAction.CloseOnly)
        {
            if (owner != null)
                owner.OnCollectFtueClosed();
        }

        Destroy(gameObject, 1.2f);
    }
}