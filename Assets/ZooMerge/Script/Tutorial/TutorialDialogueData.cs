using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "TutorialDialogue",
    menuName = "Game/Tutorial/Dialogue")]
public class TutorialDialogueData : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Header("ID")]
        public string id;

        [Header("Text")]
        [TextArea(3, 8)]
        public string message;

        public Color textColor = Color.white;

        [Tooltip(
            "Supports TMP rich text, for example: " +
            "Pick a <color=#FFD84A>spot</color>."
        )]
        public bool allowRichText = true;

        [Header("Dialogue Position")]
        public bool useDialoguePosition = true;

        public Vector2 dialogueAnchoredPosition;

        [Header("Tip")]
        public bool useTip = true;

        public Vector2 tipAnchoredPosition;
        public float tipRotation;
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