using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialDialogueView : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("Refs")]
    [SerializeField]
    private RectTransform dialogueRoot;

    [SerializeField]
    private RectTransform tipUpRoot;

    [SerializeField]
    private RectTransform tipDownRoot;

    [SerializeField]
    private RectTransform crownRoot;

    [SerializeField]
    private TextMeshProUGUI dialogueText;

    [SerializeField]
    private Animator dialogueAnimator;

    [Header("Layout")]
    [SerializeField]
    private RectTransform layoutRefreshRoot;

    [Header("Animated Text")]
    [SerializeField]
    private TutorialAnimatedText animatedText;

    // ============================================================
    // RUNTIME
    // ============================================================

    private Coroutine showRoutine;
    public bool IsVisible =>
    dialogueRoot != null &&
    dialogueRoot.gameObject.activeSelf;

    // ============================================================
    // ANIMATOR
    // ============================================================

    private static readonly int TR_In =
        Animator.StringToHash(
            "In"
        );

    private static readonly int TR_Out =
        Animator.StringToHash(
            "Out"
        );

    // ============================================================
    // INITIALIZATION
    // ============================================================

    private void Awake()
    {
        if (dialogueRoot != null)
        {
            dialogueRoot.gameObject.SetActive(
                false
            );
        }
    }

    // ============================================================
    // PLAY
    // ============================================================

    public void Play(
        TutorialDialogueData database,
        string dialogueId,
        float delay = 0f)
    {
        StopCurrentShow();

        showRoutine =
            StartCoroutine(
                ShowRoutine(
                    database,
                    dialogueId,
                    delay
                )
            );
    }

    private IEnumerator ShowRoutine(
        TutorialDialogueData database,
        string dialogueId,
        float delay)
    {
        // ========================================================
        // OPTIONAL DELAY
        // ========================================================

        if (delay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    delay
                );
        }

        // ========================================================
        // VALIDATE
        // ========================================================

        if (database == null ||
            dialogueRoot == null ||
            dialogueText == null)
        {
            showRoutine = null;
            yield break;
        }

        TutorialDialogueData.Entry data =
            database.Get(
                dialogueId
            );

        if (data == null)
        {
            Debug.LogWarning(
                $"[TutorialDialogueView] Dialogue ID " +
                $"'{dialogueId}' was not found."
            );

            showRoutine = null;
            yield break;
        }

        // Important when different dialogue IDs
        // use the same TMP text object.
        animatedText?.ClearAll();

        // ========================================================
        // DIALOGUE POSITION
        // ========================================================

        if (data.useDialoguePosition)
        {
            dialogueRoot.anchoredPosition =
                data.dialogueAnchoredPosition;
        }

        // ========================================================
        // CROWN POSITION
        // ========================================================

        if (crownRoot != null &&
            data.useCrownPosition)
        {
            crownRoot.anchoredPosition =
                data.crownAnchoredPosition;
        }

        // ========================================================
        // TIP
        // ========================================================

        bool showUpTip =
            data.useTip &&
            data.tipUp;

        bool showDownTip =
            data.useTip &&
            !data.tipUp;

        // --------------------------------
        // UP TIP
        // --------------------------------

        if (tipUpRoot != null)
        {
            tipUpRoot.gameObject.SetActive(
                showUpTip
            );

            if (showUpTip)
            {
                tipUpRoot.anchoredPosition =
                    data.tipAnchoredPosition;

                tipUpRoot.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        data.tipRotation
                    );

                Vector3 scale =
                    tipUpRoot.localScale;

                scale.x =
                    Mathf.Abs(
                        scale.x
                    ) *
                    data.tipScaleX;

                tipUpRoot.localScale =
                    scale;
            }
        }

        // --------------------------------
        // DOWN TIP
        // --------------------------------

        if (tipDownRoot != null)
        {
            tipDownRoot.gameObject.SetActive(
                showDownTip
            );

            if (showDownTip)
            {
                tipDownRoot.anchoredPosition =
                    data.tipAnchoredPosition;

                tipDownRoot.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        data.tipRotation
                    );

                Vector3 scale =
                    tipDownRoot.localScale;

                scale.x =
                    Mathf.Abs(
                        scale.x
                    ) *
                    data.tipScaleX;

                tipDownRoot.localScale =
                    scale;
            }
        }

        // ========================================================
        // PREPARE TEXT
        // ========================================================

        if (!data.useAnimatedText)
        {
            dialogueText.text =
                data.GetFormattedMessage();

            dialogueText.color =
                data.textColor;

            dialogueText.richText =
                data.UsesRichText();

            RefreshLayout();
        }
        else
        {
            dialogueText.text =
                string.Empty;
        }

        // ========================================================
        // SHOW ROOT
        // ========================================================

        dialogueRoot.gameObject.SetActive(
            true
        );

        RefreshLayout();

        // ========================================================
        // IN ANIMATION
        // ========================================================

        if (dialogueAnimator != null)
        {
            dialogueAnimator.ResetTrigger(
                TR_Out
            );

            dialogueAnimator.ResetTrigger(
                TR_In
            );

            dialogueAnimator.SetTrigger(
                TR_In
            );
        }

        // Let UI/TMP initialize first.
        yield return null;

        // ========================================================
        // ANIMATED TEXT
        // ========================================================

        // ========================================================
        // ANIMATED TEXT
        // ========================================================

        if (data.useAnimatedText)
        {
            if (animatedText != null)
            {
                Coroutine textRoutine =
                    StartCoroutine(
                        animatedText.PlayAndWait(
                            dialogueId
                        )
                    );

                // The complete TMP string has now been
                // inserted by TutorialAnimatedText.
                yield return null;

                RefreshLayout();

                // Continue waiting for all word animation,
                // punctuation and pauses to finish.
                yield return textRoutine;
            }
            else
            {
                Debug.LogWarning(
                    "[TutorialDialogueView] " +
                    "TutorialAnimatedText is missing."
                );

                dialogueText.text =
                    data.GetFormattedMessage();

                dialogueText.color =
                    data.textColor;

                dialogueText.richText =
                    data.UsesRichText();

                RefreshLayout();
            }
        }

        // ========================================================
        // OPTIONAL AUTO HIDE
        // ========================================================

        if (data.autoHide)
        {
            if (data.autoHideDelay > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        data.autoHideDelay
                    );
            }

            Hide();
        }

        showRoutine = null;
    }

    // ============================================================
    // STOP CURRENT
    // ============================================================

    private void StopCurrentShow()
    {
        if (showRoutine != null)
        {
            StopCoroutine(
                showRoutine
            );

            showRoutine = null;
        }

        animatedText?.ClearAll();
    }

    // ============================================================
    // HIDE
    // ============================================================

    public void Hide()
    {
        if (dialogueRoot == null)
            return;

        if (dialogueAnimator == null)
        {
            HideImmediate();
            return;
        }

        dialogueAnimator.ResetTrigger(
            TR_In
        );

        dialogueAnimator.ResetTrigger(
            TR_Out
        );

        dialogueAnimator.SetTrigger(
            TR_Out
        );
    }

    public void HideImmediate()
    {
        StopCurrentShow();

        if (dialogueRoot == null)
            return;

        dialogueRoot.gameObject.SetActive(
            false
        );
    }

    // ============================================================
    // ANIMATION EVENTS
    // ============================================================

    public void AE_OutFinished()
    {
        if (dialogueRoot == null)
            return;

        animatedText?.ClearAll();

        dialogueRoot.gameObject.SetActive(
            false
        );
    }

    // ============================================================
    // LAYOUT
    // ============================================================

    private void RefreshLayout()
    {
        Canvas.ForceUpdateCanvases();

        RectTransform root =
            layoutRefreshRoot != null
                ? layoutRefreshRoot
                : dialogueRoot;

        if (root != null)
        {
            LayoutRebuilder
                .ForceRebuildLayoutImmediate(
                    root
                );
        }

        Canvas.ForceUpdateCanvases();
    }
}