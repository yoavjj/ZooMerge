using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(
    typeof(AudioSfxLibrarySO.MergeBlockedSfxEntry)
)]
public class MergeBlockedSfxEntryDrawer : PropertyDrawer
{
    private const float Spacing = 4f;

    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;

        if (!property.isExpanded)
            return line;

        return (line * 7f) + (Spacing * 6f);
    }

    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty cueProperty =
            property.FindPropertyRelative("cue");

        SerializedProperty clipProperty =
            property.FindPropertyRelative("clip");

        SerializedProperty volumeProperty =
            property.FindPropertyRelative("volume");

        SerializedProperty pitchProperty =
            property.FindPropertyRelative("pitch");

        SerializedProperty randomPitchProperty =
            property.FindPropertyRelative("randomPitchRange");

        if (cueProperty == null)
        {
            EditorGUI.HelpBox(
                position,
                "Could not find the blocked merge cue.",
                MessageType.Error
            );

            EditorGUI.EndProperty();
            return;
        }

        float line = EditorGUIUtility.singleLineHeight;

        int safeIndex = Mathf.Clamp(
            cueProperty.enumValueIndex,
            0,
            cueProperty.enumDisplayNames.Length - 1
        );

        string title =
            cueProperty.enumDisplayNames[safeIndex];

        Rect row = new Rect(
            position.x,
            position.y,
            position.width,
            line
        );

        Rect foldoutRect = new Rect(
            row.x,
            row.y,
            row.width - 144f,
            line
        );

        Rect playRect = new Rect(
            row.xMax - 140f,
            row.y,
            66f,
            line
        );

        Rect stopRect = new Rect(
            row.xMax - 70f,
            row.y,
            66f,
            line
        );

        property.isExpanded = EditorGUI.Foldout(
            foldoutRect,
            property.isExpanded,
            title,
            true
        );

        AudioClip clip =
            clipProperty.objectReferenceValue as AudioClip;

        using (new EditorGUI.DisabledScope(clip == null))
        {
            if (GUI.Button(playRect, "Play"))
                EditorAudioPreview.Play(clip);

            if (GUI.Button(stopRect, "Stop"))
                EditorAudioPreview.StopAll();
        }

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        NextRow(ref row, line);

        SfxMergeBlocked selectedCue =
            (SfxMergeBlocked)cueProperty.enumValueIndex;

        selectedCue =
            (SfxMergeBlocked)EditorGUI.EnumPopup(
                row,
                "Blocked Merge Cue",
                selectedCue
            );

        cueProperty.enumValueIndex = (int)selectedCue;

        NextRow(ref row, line);
        EditorGUI.PropertyField(
            row,
            clipProperty,
            new GUIContent("Clip")
        );

        NextRow(ref row, line);
        EditorGUI.PropertyField(
            row,
            volumeProperty,
            new GUIContent("Volume")
        );

        NextRow(ref row, line);
        EditorGUI.PropertyField(
            row,
            pitchProperty,
            new GUIContent("Pitch")
        );

        NextRow(ref row, line);
        EditorGUI.LabelField(
            row,
            "Optional Pitch Randomness",
            EditorStyles.boldLabel
        );

        NextRow(ref row, line);
        EditorGUI.PropertyField(
            row,
            randomPitchProperty,
            new GUIContent("Random Pitch Range")
        );

        EditorGUI.indentLevel--;

        EditorGUI.EndProperty();
    }

    private static void NextRow(
        ref Rect row,
        float lineHeight)
    {
        row.y += lineHeight + Spacing;
    }
}