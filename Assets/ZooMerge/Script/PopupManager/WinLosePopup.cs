using TMPro;
using UnityEngine;

public class WinLosePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Animator animator;

    public void SetMessage(string msg)
    {
        if (messageText != null)
            messageText.text = msg;
    }

    public void OnMainMenuButtonPressed()
    {
        PopupManager.Instance?.ShowMainMenu();
        animator.SetTrigger("Out");
        Destroy(gameObject, 1.5f);
    }
}
