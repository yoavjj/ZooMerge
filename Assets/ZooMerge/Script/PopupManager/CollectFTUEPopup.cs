using UnityEngine;
using UnityEngine.UI;

public class CollectFTUEPopup : MonoBehaviour
{
    public static CollectFTUEPopup LastSpawned { get; private set; }

    [Header("UI")]
    [SerializeField] private Button goToMainMenuButton;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string outTrigger = "Out";

    private WinLosePopup winLosePopup;
    private bool isClosing;

    private void Awake()
    {
        LastSpawned = this;

        if (goToMainMenuButton != null)
            goToMainMenuButton.onClick.AddListener(OnButtonPressed);
    }

    private void OnDestroy()
    {
        if (goToMainMenuButton != null)
            goToMainMenuButton.onClick.RemoveListener(OnButtonPressed);

        if (LastSpawned == this)
            LastSpawned = null;
    }

    public void Initialize(WinLosePopup owner)
    {
        winLosePopup = owner;
    }

    private void OnButtonPressed()
    {
        if (isClosing)
            return;

        isClosing = true;

        if (goToMainMenuButton != null)
            goToMainMenuButton.interactable = false;

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

        if (owner != null)
            owner.OnMainMenuButtonPressed();

        Destroy(gameObject);
    } 

    public void ClosePopup()
    {
        animator.ResetTrigger(outTrigger);
        animator.SetTrigger(outTrigger);

        Destroy(gameObject, 1f);
    }
}