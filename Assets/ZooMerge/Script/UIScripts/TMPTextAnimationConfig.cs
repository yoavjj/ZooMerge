using System;
using TMPro;
using UnityEngine;

public enum TMPRevealMode
{
    WholeText,
    Words,
    Characters
}

[Serializable]
public class TMPTextAnimationConfig
{
    [Header("ID")]
    public string id;

    [Header("Target")]
    public TextMeshProUGUI text;

    [Header("Mode")]
    public TMPRevealMode revealMode =
        TMPRevealMode.Words;

    [Header("Timing")]
    [Min(0f)]
    public float startDelay = 0.1f;

    [Min(0.01f)]
    public float elementDuration = 0.3f;

    [Min(0f)]
    public float delayBetweenElements = 0.08f;

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
    public bool animateOpacity;

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

    [Header("Layout")]
    public bool refreshLayoutBeforePlay = true;

    public RectTransform layoutRoot;
}