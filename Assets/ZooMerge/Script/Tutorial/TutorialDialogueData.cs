using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "TutorialDialogue",
    menuName = "Game/Tutorial/Dialogue")]
public class TutorialDialogueData : ScriptableObject
{
    [Serializable]
    public class TextHighlight
    {
        public string text;
        public Color color = Color.green;
    }

    [Serializable]
    public class Entry
    {
        [Header("ID")]
        public string id;

        [Header("Text")]
        [TextArea(3, 8)]
        public string message;

        public Color textColor =
            Color.white;

        [Tooltip(
            "Allows TMP rich text written directly " +
            "inside the message."
        )]
        public bool allowRichText = true;

        [Header("Highlights")]
        public List<TextHighlight> highlights =
            new();

        // ============================================================
        // PRESENTATION
        // ============================================================

        [Header("Presentation")]

        [Tooltip(
            "Animate this dialogue using TutorialAnimatedText."
        )]
        public bool useAnimatedText;

        [Tooltip(
            "Automatically play the dialogue Out animation " +
            "after the text has finished."
        )]
        public bool autoHide;

        [Min(0f)]
        [Tooltip(
            "How long to keep the finished text visible " +
            "before playing the Out animation."
        )]
        public float autoHideDelay =
            1.5f;

        [Header("Dialogue Position")]
        public bool useDialoguePosition =
            true;

        public Vector2 dialogueAnchoredPosition;

        [Header("Crown")]
        public bool useCrownPosition = true;

        public Vector2 crownAnchoredPosition;

        [Header("Tip")]
        public bool useTip =
            true;

        [Tooltip(
            "If enabled, uses the Up tip. " +
            "If disabled, uses the Down tip."
        )]
        public bool tipUp;

        public Vector2 tipAnchoredPosition;

        public float tipRotation;

        [Header("Tip Scale")]
        [Range(-1f, 1f)]
        [Tooltip(
            "Horizontal tip scale. " +
            "Use 1 for normal and -1 to flip horizontally."
        )]
        public float tipScaleX =
            1f;

        public string GetFormattedMessage()
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;

            string result =
                message;

            if (highlights == null)
                return result;

            foreach (
                TextHighlight highlight
                in highlights)
            {
                if (highlight == null ||
                    string.IsNullOrEmpty(
                        highlight.text))
                {
                    continue;
                }

                string colorHex =
                    ColorUtility.ToHtmlStringRGBA(
                        highlight.color
                    );

                string coloredText =
                    $"<color=#{colorHex}>" +
                    $"{highlight.text}" +
                    "</color>";

                result =
                    result.Replace(
                        highlight.text,
                        coloredText
                    );
            }

            return result;
        }

        public bool UsesRichText()
        {
            return
                allowRichText ||
                (
                    highlights != null &&
                    highlights.Count > 0
                );
        }
    }

    [Header("Dialogues")]
    [SerializeField]
    private List<Entry> dialogues = new();

    public Entry Get(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        return dialogues.Find(
            entry =>
                entry != null &&
                entry.id == id
        );
    }
}