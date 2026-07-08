#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AudioSfxLibrarySO.SfxEntry))]
public class AudioSfxEntryDrawer : PropertyDrawer
{
    private const float Spacing = 4f;

    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;

        if (!property.isExpanded)
            return line;

        // Header + Cue + Clip + Volume + Pitch
        // + section heading + Random Pitch Range
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
                "Could not find the 'cue' field. Unity may still be using an old drawer.",
                MessageType.Error
            );

            EditorGUI.EndProperty();
            return;
        }

        float line = EditorGUIUtility.singleLineHeight;

        string title = cueProperty.enumDisplayNames[
            Mathf.Clamp(
                cueProperty.enumValueIndex,
                0,
                cueProperty.enumDisplayNames.Length - 1
            )
        ];

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

        AudioClip clip = clipProperty.objectReferenceValue as AudioClip;

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

        // Explicit enum dropdown
        SfxCue selectedCue = (SfxCue)cueProperty.enumValueIndex;

        selectedCue = (SfxCue)EditorGUI.EnumPopup(
            row,
            "Cue",
            selectedCue
        );

        cueProperty.enumValueIndex = (int)selectedCue;

        NextRow(ref row, line);
        EditorGUI.PropertyField(row, clipProperty, new GUIContent("Clip"));

        NextRow(ref row, line);
        EditorGUI.PropertyField(row, volumeProperty, new GUIContent("Volume"));

        NextRow(ref row, line);
        EditorGUI.PropertyField(row, pitchProperty, new GUIContent("Pitch"));

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

    private static void NextRow(ref Rect row, float lineHeight)
    {
        row.y += lineHeight + Spacing;
    }
}

public static class EditorAudioPreview
{
    private static readonly Type AudioUtilType =
        typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

    public static void Play(AudioClip clip)
    {
        if (clip == null || AudioUtilType == null)
            return;

        StopAll();

        MethodInfo method = AudioUtilType.GetMethod(
            "PlayPreviewClip",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new[]
            {
                typeof(AudioClip),
                typeof(int),
                typeof(bool)
            },
            null
        );

        if (method != null)
        {
            method.Invoke(null, new object[] { clip, 0, false });
            return;
        }

        method = AudioUtilType.GetMethod(
            "PlayClip",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new[] { typeof(AudioClip) },
            null
        );

        method?.Invoke(null, new object[] { clip });
    }

    public static void StopAll()
    {
        if (AudioUtilType == null)
            return;

        MethodInfo method = AudioUtilType.GetMethod(
            "StopAllPreviewClips",
            BindingFlags.Static | BindingFlags.Public
        );

        if (method != null)
        {
            method.Invoke(null, null);
            return;
        }

        method = AudioUtilType.GetMethod(
            "StopAllClips",
            BindingFlags.Static | BindingFlags.Public
        );

        method?.Invoke(null, null);
    }
}
#endif