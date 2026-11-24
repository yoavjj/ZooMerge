using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialAnimatedText : MonoBehaviour
{
    private void OnEnable()
    {
        Canvas.willRenderCanvases +=
            ReapplyActiveVisualStates;
    }

    private void ReapplyActiveVisualStates()
    {
        foreach (
            KeyValuePair<
                TextAnimationSettings,
                RuntimeState
            > pair
            in runtimeStates)
        {
            TextAnimationSettings settings =
                pair.Key;

            RuntimeState state =
                pair.Value;

            if (!state.enforceVisualState ||
                settings == null ||
                settings.text == null)
            {
                continue;
            }

            ApplyVisualState(
                settings,
                state
            );
        }
    }

    // ============================================================
    // SETTINGS
    // ============================================================

    [Serializable]
    private class PausePoint
    {
        [Tooltip(
            "Pause AFTER this animated element. " +
            "Element 0 is the first word / punctuation."
        )]
        [Min(0)]
        public int afterElement;

        [Min(0f)]
        public float duration = 0.5f;

        [Tooltip(
            "If enabled, the text waits until Resume(id) is called."
        )]
        public bool manualPause;
    }

    [Serializable]
    private class TextAnimationSettings
    {
        [Header("ID")]
        [Tooltip(
            "Should match the ID inside TutorialDialogueData."
        )]
        public string id;

        [Header("Target")]
        public TextMeshProUGUI text;

        public RectTransform layoutRoot;

        [Header("Timing")]
        [Min(0f)]
        public float startDelay;

        [Min(0.01f)]
        public float elementDuration = 0.25f;

        [Min(0f)]
        public float delayBetweenElements = 0.08f;

        public bool useUnscaledTime = true;

        [Header("Scale")]
        public bool animateScale = true;

        public float startScale = 0f;
        public float endScale = 1f;

        public AnimationCurve scaleCurve =
            AnimationCurve.EaseInOut(
                0f,
                0f,
                1f,
                1f
            );

        [Header("Opacity")]
        public bool animateOpacity = true;

        [Range(0f, 1f)]
        public float startOpacity = 0f;

        [Range(0f, 1f)]
        public float endOpacity = 1f;

        public AnimationCurve opacityCurve =
            AnimationCurve.EaseInOut(
                0f,
                0f,
                1f,
                1f
            );

        [Header("Punctuation")]
        [Tooltip(
            "If enabled, punctuation such as ! ? and . " +
            "animates independently."
        )]
        public bool animatePunctuationSeparately = true;

        [Header("Pause Points")]
        public List<PausePoint> pausePoints =
            new();
    }

    // ============================================================
    // DATA
    // ============================================================

    [Header("Data")]
    [SerializeField]
    private TutorialDialogueData dialogueDatabase;

    [Header("Animations")]
    [SerializeField]
    private List<TextAnimationSettings> animations =
        new();

    // ============================================================
    // RUNTIME
    // ============================================================

    private struct TextElement
    {
        public int firstCharacter;
        public int characterCount;

        public TextElement(
            int firstCharacter,
            int characterCount)
        {
            this.firstCharacter =
                firstCharacter;

            this.characterCount =
                characterCount;
        }
    }

    private class RuntimeState
    {
        public Coroutine routine;

        public bool waitingForResume;

        public Vector3[][]
            originalVertices;

        public Color32[][]
            originalColors;

        public readonly List<TextElement>
            elements = new();

        public int activeElementIndex = -1;

        public float activeElementProgress;

        public bool enforceVisualState;
    }

    private readonly Dictionary<
        TextAnimationSettings,
        RuntimeState
    > runtimeStates = new();

    // ============================================================
    // PUBLIC
    // ============================================================

    public void Play(
        string id)
    {
        TextAnimationSettings settings =
            GetSettings(
                id
            );

        if (settings == null)
        {
            Debug.LogWarning(
                $"[TutorialAnimatedText] " +
                $"Animation settings '{id}' were not found."
            );

            return;
        }

        if (settings.text == null)
        {
            Debug.LogWarning(
                $"[TutorialAnimatedText] " +
                $"Text target for '{id}' is missing."
            );

            return;
        }

        if (dialogueDatabase == null)
        {
            Debug.LogWarning(
                "[TutorialAnimatedText] " +
                "Dialogue database is missing."
            );

            return;
        }

        TutorialDialogueData.Entry entry =
            dialogueDatabase.Get(
                id
            );

        if (entry == null)
        {
            Debug.LogWarning(
                $"[TutorialAnimatedText] " +
                $"Dialogue '{id}' was not found."
            );

            return;
        }

        RuntimeState state =
            GetRuntimeState(
                settings
            );

        StopCurrentAnimation(
            settings,
            state
        );

        PrepareText(
            settings,
            state,
            entry
        );

        state.routine =
            StartCoroutine(
                AnimateElements(
                    settings,
                    state
                )
            );
    }

    public IEnumerator PlayAndWait(
    string id)
    {
        TextAnimationSettings settings =
            GetSettings(
                id
            );

        if (settings == null)
        {
            Debug.LogWarning(
                $"[TutorialAnimatedText] " +
                $"Animation settings '{id}' were not found."
            );

            yield break;
        }

        Play(
            id
        );

        RuntimeState state =
            GetRuntimeState(
                settings
            );

        while (state.routine != null)
        {
            yield return null;
        }
    }

    public void Resume(
        string id)
    {
        TextAnimationSettings settings =
            GetSettings(
                id
            );

        if (settings == null)
            return;

        RuntimeState state =
            GetRuntimeState(
                settings
            );

        state.waitingForResume =
            false;
    }

    public void ResumeAll()
    {
        foreach (
            RuntimeState state
            in runtimeStates.Values)
        {
            state.waitingForResume =
                false;
        }
    }

    public void Clear(
        string id)
    {
        TextAnimationSettings settings =
            GetSettings(
                id
            );

        if (settings == null ||
            settings.text == null)
        {
            return;
        }

        RuntimeState state =
            GetRuntimeState(
                settings
            );

        StopCurrentAnimation(
            settings,
            state
        );

        state.elements.Clear();

        settings.text.text =
            string.Empty;

        settings.text.ForceMeshUpdate(
            true,
            true
        );
    }

    public void ClearAll()
    {
        foreach (
            TextAnimationSettings settings
            in animations)
        {
            if (settings == null ||
                settings.text == null)
            {
                continue;
            }

            RuntimeState state =
                GetRuntimeState(
                    settings
                );

            StopCurrentAnimation(
                settings,
                state
            );

            state.elements.Clear();

            settings.text.text =
                string.Empty;

            settings.text.ForceMeshUpdate(
                true,
                true
            );
        }
    }

    // ============================================================
    // PREPARE
    // ============================================================

    private void PrepareText(
        TextAnimationSettings settings,
        RuntimeState state,
        TutorialDialogueData.Entry entry)
    {
        TextMeshProUGUI text =
            settings.text;

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
        // APPLY DATABASE TEXT
        // --------------------------------

        text.color =
            entry.textColor;

        text.richText =
            entry.UsesRichText();

        text.text =
            entry.GetFormattedMessage();

        // --------------------------------
        // BUILD FINAL LAYOUT
        // --------------------------------

        Canvas.ForceUpdateCanvases();

        RectTransform root =
            settings.layoutRoot != null
                ? settings.layoutRoot
                : text.rectTransform;

        LayoutRebuilder
            .ForceRebuildLayoutImmediate(
                root
            );

        Canvas.ForceUpdateCanvases();

        text.ForceMeshUpdate(
            true,
            true
        );

        TMP_TextInfo textInfo =
            text.textInfo;

        // --------------------------------
        // SAVE ORIGINAL MESH
        // --------------------------------

        state.originalVertices =
            CopyVertices(
                textInfo
            );

        state.originalColors =
            CopyColors(
                textInfo
            );

        // --------------------------------
        // BUILD ELEMENTS
        // --------------------------------

        state.elements.Clear();

        state.elements.AddRange(
            BuildElements(
                settings,
                textInfo
            )
        );

        // --------------------------------
        // INITIAL VISUAL STATE
        // --------------------------------

        state.activeElementIndex =
            -1;

        state.activeElementProgress =
            0f;

        state.enforceVisualState =
            true;

        // Every element starts hidden.
        ApplyVisualState(
            settings,
            state
        );
    }

    private void ApplyVisualState(
    TextAnimationSettings settings,
    RuntimeState state)
    {
        if (settings?.text == null ||
            state.originalVertices == null ||
            state.originalColors == null)
        {
            return;
        }

        TMP_TextInfo textInfo =
            settings.text.textInfo;

        for (int i = 0;
             i < state.elements.Count;
             i++)
        {
            float progress;

            // Animation completely finished.
            if (state.activeElementIndex >=
                state.elements.Count)
            {
                progress = 1f;
            }
            // Nothing has started yet.
            else if (state.activeElementIndex < 0)
            {
                progress = 0f;
            }
            // Elements before current are finished.
            else if (i <
                     state.activeElementIndex)
            {
                progress = 1f;
            }
            // Current element is animating.
            else if (i ==
                     state.activeElementIndex)
            {
                progress =
                    state.activeElementProgress;
            }
            // Everything after current stays hidden.
            else
            {
                progress = 0f;
            }

            ApplyElement(
                settings,
                state,
                textInfo,
                state.elements[i],
                progress
            );
        }

        UploadMesh(
            settings.text
        );
    }

    // ============================================================
    // BUILD ELEMENTS
    // ============================================================

    private List<TextElement> BuildElements(
        TextAnimationSettings settings,
        TMP_TextInfo textInfo)
    {
        List<TextElement> elements =
            new();

        int wordStart = -1;
        int wordLength = 0;

        for (int i = 0;
             i < textInfo.characterCount;
             i++)
        {
            char character =
                textInfo
                    .characterInfo[i]
                    .character;

            // --------------------------------
            // SPACE / NEW LINE
            // --------------------------------

            if (char.IsWhiteSpace(
                    character))
            {
                AddCurrentWord(
                    elements,
                    ref wordStart,
                    ref wordLength
                );

                continue;
            }

            // --------------------------------
            // PUNCTUATION
            // --------------------------------

            if (
                settings
                    .animatePunctuationSeparately &&
                IsSeparatePunctuation(
                    character))
            {
                AddCurrentWord(
                    elements,
                    ref wordStart,
                    ref wordLength
                );

                elements.Add(
                    new TextElement(
                        i,
                        1
                    )
                );

                continue;
            }

            // --------------------------------
            // NORMAL CHARACTER
            // --------------------------------

            if (wordStart < 0)
            {
                wordStart = i;
                wordLength = 0;
            }

            wordLength++;
        }

        AddCurrentWord(
            elements,
            ref wordStart,
            ref wordLength
        );

        return elements;
    }

    private static bool IsSeparatePunctuation(
        char character)
    {
        if (!char.IsPunctuation(
                character))
        {
            return false;
        }

        // Keep apostrophes inside words:
        // Someone's
        // don't
        // we're

        if (character == '\'' ||
            character == '’')
        {
            return false;
        }

        return true;
    }

    private static void AddCurrentWord(
        List<TextElement> elements,
        ref int wordStart,
        ref int wordLength)
    {
        if (wordStart < 0 ||
            wordLength <= 0)
        {
            return;
        }

        elements.Add(
            new TextElement(
                wordStart,
                wordLength
            )
        );

        wordStart = -1;
        wordLength = 0;
    }

    // ============================================================
    // ANIMATION
    // ============================================================

    private IEnumerator AnimateElements(
        TextAnimationSettings settings,
        RuntimeState state)
    {
        if (settings.startDelay > 0f)
        {
            yield return
                Wait(
                    settings,
                    settings.startDelay
                );
        }

        TMP_TextInfo textInfo =
            settings.text.textInfo;

        for (int i = 0;
             i < state.elements.Count;
             i++)
        {

            state.activeElementIndex = i;

            state.activeElementProgress = 0f;

            yield return
                AnimateElement(
                    settings,
                    state,
                    textInfo,
                    state.elements[i]
                );

            // --------------------------------
            // PAUSE POINT
            // --------------------------------

            yield return
                HandlePausePoint(
                    settings,
                    state,
                    i
                );

            // --------------------------------
            // NORMAL DELAY
            // --------------------------------

            if (
                settings
                    .delayBetweenElements > 0f &&
                i < state.elements.Count - 1)
            {
                yield return
                    Wait(
                        settings,
                        settings
                            .delayBetweenElements
                    );
            }
        }

        state.activeElementIndex =
    state.elements.Count;

        state.activeElementProgress =
            1f;

        ApplyVisualState(
            settings,
            state
        );

        state.enforceVisualState =
            false;

        state.routine =
            null;
    }

    private IEnumerator AnimateElement(
        TextAnimationSettings settings,
        RuntimeState state,
        TMP_TextInfo textInfo,
        TextElement element)
    {
        float elapsed =
            0f;

        while (
            elapsed <
            settings.elementDuration)
        {
            elapsed +=
                GetDeltaTime(
                    settings
                );

            float normalized =
                Mathf.Clamp01(
                    elapsed /
                    settings.elementDuration
                );

            state.activeElementProgress =
                normalized;

            ApplyVisualState(
                settings,
                state
            );

            yield return null;
        }

        state.activeElementProgress =
            1f;

        ApplyVisualState(
            settings,
            state
        );
    }

    // ============================================================
    // PAUSES
    // ============================================================

    private IEnumerator HandlePausePoint(
        TextAnimationSettings settings,
        RuntimeState state,
        int elementIndex)
    {
        if (settings.pausePoints == null)
            yield break;

        foreach (
            PausePoint pause
            in settings.pausePoints)
        {
            if (pause == null ||
                pause.afterElement !=
                elementIndex)
            {
                continue;
            }

            // --------------------------------
            // MANUAL
            // --------------------------------

            if (pause.manualPause)
            {
                state.waitingForResume =
                    true;

                while (
                    state.waitingForResume)
                {
                    yield return null;
                }

                yield break;
            }

            // --------------------------------
            // TIMED
            // --------------------------------

            if (pause.duration > 0f)
            {
                yield return
                    Wait(
                        settings,
                        pause.duration
                    );
            }

            yield break;
        }
    }

    // ============================================================
    // ELEMENT
    // ============================================================

    private void ApplyElement(
        TextAnimationSettings settings,
        RuntimeState state,
        TMP_TextInfo textInfo,
        TextElement element,
        float normalized)
    {
        float scale =
            GetScale(
                settings,
                normalized
            );

        float opacity =
            GetOpacity(
                settings,
                normalized
            );

        Vector3 center =
            GetElementCenter(
                state,
                textInfo,
                element
            );

        int lastCharacter =
            element.firstCharacter +
            element.characterCount;

        for (
            int i = element.firstCharacter;
            i < lastCharacter;
            i++)
        {
            TMP_CharacterInfo character =
                textInfo.characterInfo[i];

            if (!character.isVisible)
                continue;

            int materialIndex =
                character
                    .materialReferenceIndex;

            int vertexIndex =
                character.vertexIndex;

            ApplyCharacterScale(
                settings,
                state,
                textInfo,
                materialIndex,
                vertexIndex,
                center,
                scale
            );

            ApplyCharacterOpacity(
                settings,
                state,
                textInfo,
                materialIndex,
                vertexIndex,
                opacity
            );
        }
    }

    // ============================================================
    // SCALE
    // ============================================================

    private static void ApplyCharacterScale(
        TextAnimationSettings settings,
        RuntimeState state,
        TMP_TextInfo textInfo,
        int materialIndex,
        int vertexIndex,
        Vector3 center,
        float scale)
    {
        Vector3[] source =
            state.originalVertices[
                materialIndex
            ];

        Vector3[] destination =
            textInfo.meshInfo[
                materialIndex
            ].vertices;

        for (int i = 0;
             i < 4;
             i++)
        {
            Vector3 sourceVertex =
                source[
                    vertexIndex + i
                ];

            if (!settings.animateScale)
            {
                destination[
                    vertexIndex + i
                ] =
                    sourceVertex;

                continue;
            }

            Vector3 offset =
                sourceVertex -
                center;

            destination[
                vertexIndex + i
            ] =
                center +
                offset *
                scale;
        }
    }

    // ============================================================
    // OPACITY
    // ============================================================

    private static void ApplyCharacterOpacity(
        TextAnimationSettings settings,
        RuntimeState state,
        TMP_TextInfo textInfo,
        int materialIndex,
        int vertexIndex,
        float opacity)
    {
        Color32[] source =
            state.originalColors[
                materialIndex
            ];

        Color32[] destination =
            textInfo.meshInfo[
                materialIndex
            ].colors32;

        for (int i = 0;
             i < 4;
             i++)
        {
            Color32 color =
                source[
                    vertexIndex + i
                ];

            if (settings.animateOpacity)
            {
                color.a =
                    (byte)Mathf.Clamp(
                        Mathf.RoundToInt(
                            color.a *
                            opacity
                        ),
                        0,
                        255
                    );
            }

            destination[
                vertexIndex + i
            ] =
                color;
        }
    }

    // ============================================================
    // VALUES
    // ============================================================

    private static float GetScale(
        TextAnimationSettings settings,
        float normalized)
    {
        if (!settings.animateScale)
            return 1f;

        float t =
            settings.scaleCurve != null
                ? settings.scaleCurve.Evaluate(
                    normalized
                )
                : normalized;

        return Mathf.LerpUnclamped(
            settings.startScale,
            settings.endScale,
            t
        );
    }

    private static float GetOpacity(
        TextAnimationSettings settings,
        float normalized)
    {
        if (!settings.animateOpacity)
            return 1f;

        float t =
            settings.opacityCurve != null
                ? settings.opacityCurve.Evaluate(
                    normalized
                )
                : normalized;

        return Mathf.LerpUnclamped(
            settings.startOpacity,
            settings.endOpacity,
            t
        );
    }

    // ============================================================
    // ELEMENT CENTER
    // ============================================================

    private static Vector3 GetElementCenter(
        RuntimeState state,
        TMP_TextInfo textInfo,
        TextElement element)
    {
        Vector3 min =
            new Vector3(
                float.MaxValue,
                float.MaxValue,
                0f
            );

        Vector3 max =
            new Vector3(
                float.MinValue,
                float.MinValue,
                0f
            );

        bool found =
            false;

        int lastCharacter =
            element.firstCharacter +
            element.characterCount;

        for (
            int i = element.firstCharacter;
            i < lastCharacter;
            i++)
        {
            TMP_CharacterInfo character =
                textInfo.characterInfo[i];

            if (!character.isVisible)
                continue;

            found = true;

            int materialIndex =
                character
                    .materialReferenceIndex;

            int vertexIndex =
                character.vertexIndex;

            Vector3[] vertices =
                state.originalVertices[
                    materialIndex
                ];

            for (int j = 0;
                 j < 4;
                 j++)
            {
                Vector3 vertex =
                    vertices[
                        vertexIndex + j
                    ];

                min =
                    Vector3.Min(
                        min,
                        vertex
                    );

                max =
                    Vector3.Max(
                        max,
                        vertex
                    );
            }
        }

        return found
            ? (min + max) * 0.5f
            : Vector3.zero;
    }

    // ============================================================
    // LOOKUP
    // ============================================================

    private TextAnimationSettings GetSettings(
        string id)
    {
        if (animations == null ||
            string.IsNullOrEmpty(id))
        {
            return null;
        }

        return animations.Find(
            settings =>
                settings != null &&
                settings.id == id
        );
    }

    private RuntimeState GetRuntimeState(
        TextAnimationSettings settings)
    {
        if (runtimeStates.TryGetValue(
                settings,
                out RuntimeState state))
        {
            return state;
        }

        state =
            new RuntimeState();

        runtimeStates.Add(
            settings,
            state
        );

        return state;
    }

    // ============================================================
    // MESH
    // ============================================================

    private static void UploadMesh(
        TextMeshProUGUI text)
    {
        text.UpdateVertexData(
            TMP_VertexDataUpdateFlags.Vertices |
            TMP_VertexDataUpdateFlags.Colors32
        );
    }

    private static Vector3[][]
        CopyVertices(
            TMP_TextInfo textInfo)
    {
        Vector3[][] result =
            new Vector3[
                textInfo.meshInfo.Length
            ][];

        for (int i = 0;
             i < result.Length;
             i++)
        {
            result[i] =
                (Vector3[])
                textInfo.meshInfo[i]
                    .vertices.Clone();
        }

        return result;
    }

    private static Color32[][]
        CopyColors(
            TMP_TextInfo textInfo)
    {
        Color32[][] result =
            new Color32[
                textInfo.meshInfo.Length
            ][];

        for (int i = 0;
             i < result.Length;
             i++)
        {
            result[i] =
                (Color32[])
                textInfo.meshInfo[i]
                    .colors32.Clone();
        }

        return result;
    }

    // ============================================================
    // TIME
    // ============================================================

    private IEnumerator Wait(
        TextAnimationSettings settings,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed +=
                GetDeltaTime(
                    settings
                );

            yield return null;
        }
    }

    private static float GetDeltaTime(
        TextAnimationSettings settings)
    {
        return settings.useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;
    }

    // ============================================================
    // CLEANUP
    // ============================================================

    private void StopCurrentAnimation(
        TextAnimationSettings settings,
        RuntimeState state)
    {
        state.waitingForResume =
            false;

        state.enforceVisualState =
            false;

        if (state.routine == null)
            return;

        StopCoroutine(
            state.routine
        );

        state.routine =
            null;
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -=
            ReapplyActiveVisualStates;

        foreach (
            RuntimeState state
            in runtimeStates.Values)
        {
            state.waitingForResume =
                false;

            state.enforceVisualState =
                false;

            if (state.routine != null)
            {
                StopCoroutine(
                    state.routine
                );

                state.routine =
                    null;
            }
        }
    }
}