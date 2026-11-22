using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TMPWordScaleAnimator : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField]
    private List<TMPTextAnimationConfig>
        animations = new();

    [Header("Default")]
    [SerializeField, Min(0)]
    private int defaultAnimationIndex;

    private readonly Dictionary<
        TMPTextAnimationConfig,
        Coroutine
    > runningAnimations = new();

    // ============================================================
    // PUBLIC API
    // ============================================================

    public void PlayIndex(int index)
    {
        if (!TryGetAnimation(
                index,
                out TMPTextAnimationConfig config))
        {
            return;
        }

        StartAnimation(
            config
        );
    }

    public void PlayById(string id)
    {
        TMPTextAnimationConfig config =
            GetAnimationById(id);

        if (config == null)
        {
            Debug.LogWarning(
                $"[TMPWordScaleAnimator] " +
                $"Animation ID '{id}' was not found."
            );

            return;
        }

        StartAnimation(
            config
        );
    }

    public void PlayAll()
    {
        foreach (
            TMPTextAnimationConfig config
            in animations)
        {
            if (config?.text == null)
                continue;

            StartAnimation(
                config
            );
        }
    }

    public void Play(
        string animationId,
        TutorialDialogueData.Entry entry)
    {
        TMPTextAnimationConfig config =
            GetAnimationById(
                animationId
            );

        if (config == null)
        {
            Debug.LogWarning(
                $"[TMPWordScaleAnimator] " +
                $"Animation ID '{animationId}' was not found."
            );

            return;
        }

        if (entry == null)
        {
            Debug.LogWarning(
                $"[TMPWordScaleAnimator] " +
                $"Dialogue entry '{animationId}' is null."
            );

            return;
        }

        StopAnimation(
            config
        );

        Coroutine routine =
            StartCoroutine(
                PlayDialogueRoutine(
                    config,
                    entry
                )
            );

        runningAnimations[
            config
        ] = routine;
    }

    private IEnumerator PlayDialogueRoutine(
        TMPTextAnimationConfig config,
        TutorialDialogueData.Entry entry)
    {
        TextMeshProUGUI text =
            config.text;

        if (text == null)
            yield break;

        // --------------------------------
        // CLEAR OLD TEXT
        // --------------------------------

        text.text =
            string.Empty;

        text.ForceMeshUpdate(
            true,
            true
        );

        // --------------------------------
        // LOAD NEW DATABASE TEXT
        // --------------------------------

        ApplyDialogueEntry(
            config,
            entry
        );

        // --------------------------------
        // BUILD FINAL LAYOUT
        // --------------------------------

        RefreshLayout(
            config
        );

        text.ForceMeshUpdate(
            true,
            true
        );

        TMP_TextInfo textInfo =
            text.textInfo;

        Vector3[][] originalVertices =
            TMPTextMeshUtility.CopyVertices(
                textInfo
            );

        Color32[][] originalColors =
            TMPTextMeshUtility.CopyColors(
                textInfo
            );

        int elementCount =
            TMPTextMeshUtility.GetElementCount(
                config.revealMode,
                textInfo
            );

        if (elementCount <= 0)
        {
            FinishAnimation(
                config
            );

            yield break;
        }

        // ============================================================
        // IMPORTANT:
        // HIDE EVERY WORD BEFORE THE FIRST FRAME CAN RENDER
        // ============================================================

        HideElements(
            config,
            textInfo,
            originalVertices,
            originalColors,
            elementCount
        );

        // Nothing has yielded yet.
        //
        // At this exact point:
        // Word 0 = Start Scale / Start Opacity
        // Word 1 = Start Scale / Start Opacity
        // Word 2 = Start Scale / Start Opacity
        // ...
        //
        // So the full sentence must NOT be visible.

        // --------------------------------
        // START DELAY
        // --------------------------------

        if (config.startDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    config.startDelay
                );
        }

        // --------------------------------
        // REVEAL ONE WORD AT A TIME
        // --------------------------------

        for (int i = 0;
             i < elementCount;
             i++)
        {
            yield return
                AnimateElement(
                    config,
                    textInfo,
                    originalVertices,
                    originalColors,
                    i
                );

            if (config.delayBetweenElements > 0f &&
                i < elementCount - 1)
            {
                yield return
                    new WaitForSecondsRealtime(
                        config.delayBetweenElements
                    );
            }
        }

        FinishAnimation(
            config
        );
    }

    public void StopAll()
    {
        foreach (
            Coroutine routine
            in runningAnimations.Values)
        {
            if (routine != null)
                StopCoroutine(routine);
        }

        runningAnimations.Clear();

        foreach (
            TMPTextAnimationConfig config
            in animations)
        {
            RestoreText(
                config
            );
        }
    }

    // ============================================================
    // ANIMATION
    // ============================================================

    private void StartAnimation(
        TMPTextAnimationConfig config)
    {
        if (config?.text == null)
            return;

        StopAnimation(
            config
        );

        RefreshLayout(
            config
        );

        Coroutine routine =
            StartCoroutine(
                AnimateText(
                    config
                )
            );

        runningAnimations[
            config
        ] = routine;
    }

    private IEnumerator AnimateText(
        TMPTextAnimationConfig config)
    {
        TextMeshProUGUI text =
            config.text;

        text.ForceMeshUpdate();

        TMP_TextInfo textInfo =
            text.textInfo;

        Vector3[][] originalVertices =
            TMPTextMeshUtility.CopyVertices(
                textInfo
            );

        Color32[][] originalColors =
            TMPTextMeshUtility.CopyColors(
                textInfo
            );

        int elementCount =
            TMPTextMeshUtility.GetElementCount(
                config.revealMode,
                textInfo
            );

        if (elementCount <= 0)
        {
            FinishAnimation(
                config
            );

            yield break;
        }

        HideElements(
            config,
            textInfo,
            originalVertices,
            originalColors,
            elementCount
        );

        if (config.startDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    config.startDelay
                );
        }

        for (int i = 0;
             i < elementCount;
             i++)
        {
            yield return
                AnimateElement(
                    config,
                    textInfo,
                    originalVertices,
                    originalColors,
                    i
                );

            if (config.delayBetweenElements > 0f &&
                i < elementCount - 1)
            {
                yield return
                    new WaitForSecondsRealtime(
                        config.delayBetweenElements
                    );
            }
        }

        FinishAnimation(
            config
        );
    }

    private void HideElements(
        TMPTextAnimationConfig config,
        TMP_TextInfo textInfo,
        Vector3[][] vertices,
        Color32[][] colors,
        int elementCount)
    {
        for (int i = 0;
             i < elementCount;
             i++)
        {
            TMPTextMeshUtility.ApplyElement(
                config,
                textInfo,
                vertices,
                colors,
                i,
                0f
            );
        }

        TMPTextMeshUtility.ApplyMesh(
            config.text,
            textInfo
        );
    }

    private IEnumerator AnimateElement(
        TMPTextAnimationConfig config,
        TMP_TextInfo textInfo,
        Vector3[][] vertices,
        Color32[][] colors,
        int elementIndex)
    {
        float elapsed = 0f;

        while (
            elapsed <
            config.elementDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float normalized =
                Mathf.Clamp01(
                    elapsed /
                    config.elementDuration
                );

            TMPTextMeshUtility.ApplyElement(
                config,
                textInfo,
                vertices,
                colors,
                elementIndex,
                normalized
            );

            TMPTextMeshUtility.ApplyMesh(
                config.text,
                textInfo
            );

            yield return null;
        }

        TMPTextMeshUtility.ApplyElement(
            config,
            textInfo,
            vertices,
            colors,
            elementIndex,
            1f
        );

        TMPTextMeshUtility.ApplyMesh(
            config.text,
            textInfo
        );
    }

    // ============================================================
    // LOOKUP / CLEANUP
    // ============================================================

    private bool TryGetAnimation(
        int index,
        out TMPTextAnimationConfig config)
    {
        config = null;

        if (animations == null ||
            index < 0 ||
            index >= animations.Count)
        {
            return false;
        }

        config =
            animations[index];

        return
            config != null &&
            config.text != null;
    }

    private TMPTextAnimationConfig
        GetAnimationById(string id)
    {
        if (animations == null ||
            string.IsNullOrEmpty(id))
        {
            return null;
        }

        return animations.Find(
            config =>
                config != null &&
                config.id == id
        );
    }

    private void StopAnimation(
        TMPTextAnimationConfig config)
    {
        if (!runningAnimations.TryGetValue(
                config,
                out Coroutine routine))
        {
            return;
        }

        if (routine != null)
            StopCoroutine(routine);

        runningAnimations.Remove(
            config
        );
    }

    private void FinishAnimation(
        TMPTextAnimationConfig config)
    {
        runningAnimations.Remove(
            config
        );
    }

    private void RestoreText(
        TMPTextAnimationConfig config)
    {
        if (config?.text == null)
            return;

        config.text.ForceMeshUpdate();

        config.text.UpdateVertexData(
            TMP_VertexDataUpdateFlags.All
        );
    }

    private static void ApplyDialogueEntry(
        TMPTextAnimationConfig config,
        TutorialDialogueData.Entry entry)
    {
        if (config?.text == null ||
            entry == null)
        {
            return;
        }

        config.text.color =
            entry.textColor;

        config.text.richText =
            entry.UsesRichText();

        config.text.text =
            entry.GetFormattedMessage();
    }

    private static void RefreshLayout(
    TMPTextAnimationConfig config)
    {
        if (config == null ||
            config.text == null ||
            !config.refreshLayoutBeforePlay)
        {
            return;
        }

        // Update TMP using the newly assigned text.
        config.text.ForceMeshUpdate();

        // Let Unity process pending UI layout changes.
        Canvas.ForceUpdateCanvases();

        RectTransform layoutRoot =
            config.layoutRoot != null
                ? config.layoutRoot
                : config.text.rectTransform;

        LayoutRebuilder
            .ForceRebuildLayoutImmediate(
                layoutRoot
            );

        Canvas.ForceUpdateCanvases();

        // Important:
        // rebuild TMP again AFTER the layout changed,
        // because the text geometry may now wrap differently.
        config.text.ForceMeshUpdate();
    }
}