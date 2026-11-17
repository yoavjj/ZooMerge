using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialDialogueView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform dialogueRoot;
    [SerializeField] private RectTransform tipRoot;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Animator dialogueAnimator;

    private static readonly int TR_In =
    Animator.StringToHash("In");

    private static readonly int TR_Out =
        Animator.StringToHash("Out");

    private void Awake()
    {
        if (dialogueRoot != null)
            dialogueRoot.gameObject.SetActive(false);
    }

    public IEnumerator Show(
        TutorialDialogueData database,
        string dialogueId)
    {
        if (database == null ||
            dialogueRoot == null ||
            dialogueText == null)
        {
            yield break;
        }

        TutorialDialogueData.Entry data =
            database.Get(dialogueId);

        if (data == null)
        {
            Debug.LogWarning(
                $"[TutorialDialogueView] Dialogue ID " +
                $"'{dialogueId}' was not found."
            );

            yield break;
        }

        // --------------------------------
        // APPLY TEXT
        // --------------------------------

        dialogueText.text =
            data.message;

        dialogueText.color =
            data.textColor;

        dialogueText.richText =
            data.allowRichText;

        // --------------------------------
        // APPLY POSITION
        // --------------------------------

        if (data.useDialoguePosition)
        {
            dialogueRoot.anchoredPosition =
                data.dialogueAnchoredPosition;
        }

        // --------------------------------
        // APPLY TIP
        // --------------------------------

        if (tipRoot != null)
        {
            tipRoot.gameObject.SetActive(
                data.useTip
            );

            if (data.useTip)
            {
                tipRoot.anchoredPosition =
                    data.tipAnchoredPosition;

                tipRoot.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        data.tipRotation
                    );
            }
        }

        // --------------------------------
        // SHOW
        // --------------------------------

        dialogueRoot.gameObject.SetActive(true);

        if (dialogueAnimator != null)
        {
            dialogueAnimator.ResetTrigger(TR_Out);
            dialogueAnimator.ResetTrigger(TR_In);

            dialogueAnimator.SetTrigger(TR_In);
        }

        yield return null;
    }

    public void Hide()
    {
        if (dialogueRoot == null)
            return;

        if (dialogueAnimator == null)
        {
            HideImmediate();
            return;
        }

        dialogueAnimator.ResetTrigger(TR_In);
        dialogueAnimator.ResetTrigger(TR_Out);

        dialogueAnimator.SetTrigger(TR_Out);
    }

    public void HideImmediate()
    {
        if (dialogueRoot == null)
            return;

        dialogueRoot.gameObject.SetActive(false);
    }

    public void AE_OutFinished()
    {
        if (dialogueRoot != null)
            dialogueRoot.gameObject.SetActive(false);
    }
}