using TMPro;
using UnityEngine;

public static class TMPTextMeshUtility
{
    public static Vector3[][] CopyVertices(
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
                textInfo.meshInfo[i]
                    .vertices
                    .Clone()
                    as Vector3[];
        }

        return result;
    }

    public static Color32[][] CopyColors(
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
                textInfo.meshInfo[i]
                    .colors32
                    .Clone()
                    as Color32[];
        }

        return result;
    }

    public static int GetElementCount(
        TMPRevealMode mode,
        TMP_TextInfo textInfo)
    {
        switch (mode)
        {
            case TMPRevealMode.WholeText:
                return
                    textInfo.characterCount > 0
                        ? 1
                        : 0;

            case TMPRevealMode.Words:
                return textInfo.wordCount;

            case TMPRevealMode.Characters:
                return textInfo.characterCount;

            default:
                return 0;
        }
    }

    public static bool TryGetCharacterRange(
        TMPRevealMode mode,
        TMP_TextInfo textInfo,
        int elementIndex,
        out int firstCharacter,
        out int lastCharacter)
    {
        firstCharacter = -1;
        lastCharacter = -1;

        switch (mode)
        {
            case TMPRevealMode.WholeText:
                {
                    firstCharacter = 0;
                    lastCharacter =
                        textInfo.characterCount;

                    return
                        lastCharacter > 0;
                }

            case TMPRevealMode.Words:
                {
                    if (elementIndex < 0 ||
                        elementIndex >=
                        textInfo.wordCount)
                    {
                        return false;
                    }

                    TMP_WordInfo word =
                        textInfo.wordInfo[
                            elementIndex
                        ];

                    firstCharacter =
                        word.firstCharacterIndex;

                    lastCharacter =
                        firstCharacter +
                        word.characterCount;

                    return true;
                }

            case TMPRevealMode.Characters:
                {
                    if (elementIndex < 0 ||
                        elementIndex >=
                        textInfo.characterCount)
                    {
                        return false;
                    }

                    firstCharacter =
                        elementIndex;

                    lastCharacter =
                        elementIndex + 1;

                    return true;
                }
        }

        return false;
    }

    public static void ApplyElement(
        TMPTextAnimationConfig config,
        TMP_TextInfo textInfo,
        Vector3[][] originalVertices,
        Color32[][] originalColors,
        int elementIndex,
        float normalized)
    {
        if (!TryGetCharacterRange(
                config.revealMode,
                textInfo,
                elementIndex,
                out int firstCharacter,
                out int lastCharacter))
        {
            return;
        }

        float scale =
            CalculateScale(
                config,
                normalized
            );

        float opacity =
            CalculateOpacity(
                config,
                normalized
            );

        Vector3 center =
            GetRangeCenter(
                textInfo,
                originalVertices,
                firstCharacter,
                lastCharacter
            );

        for (int i = firstCharacter;
             i < lastCharacter;
             i++)
        {
            TMP_CharacterInfo character =
                textInfo.characterInfo[i];

            // Spaces have no visible geometry.
            // Punctuation such as "." IS visible,
            // so character mode still animates it.
            if (!character.isVisible)
                continue;

            ApplyCharacterVertices(
                config,
                textInfo,
                originalVertices,
                character,
                center,
                scale
            );

            ApplyCharacterColors(
                config,
                textInfo,
                originalColors,
                character,
                opacity
            );
        }
    }

    public static void ApplyMesh(
        TextMeshProUGUI text,
        TMP_TextInfo textInfo)
    {
        for (int i = 0;
             i < textInfo.meshInfo.Length;
             i++)
        {
            TMP_MeshInfo meshInfo =
                textInfo.meshInfo[i];

            meshInfo.mesh.vertices =
                meshInfo.vertices;

            meshInfo.mesh.colors32 =
                meshInfo.colors32;

            text.UpdateGeometry(
                meshInfo.mesh,
                i
            );
        }
    }

    private static float CalculateScale(
        TMPTextAnimationConfig config,
        float normalized)
    {
        if (!config.animateScale)
            return 1f;

        float t =
            config.scaleCurve != null
                ? config.scaleCurve.Evaluate(
                    normalized
                )
                : normalized;

        return Mathf.LerpUnclamped(
            config.startScale,
            config.endScale,
            t
        );
    }

    private static float CalculateOpacity(
        TMPTextAnimationConfig config,
        float normalized)
    {
        if (!config.animateOpacity)
            return 1f;

        float t =
            config.opacityCurve != null
                ? config.opacityCurve.Evaluate(
                    normalized
                )
                : normalized;

        return Mathf.LerpUnclamped(
            config.startOpacity,
            config.endOpacity,
            t
        );
    }

    private static void ApplyCharacterVertices(
        TMPTextAnimationConfig config,
        TMP_TextInfo textInfo,
        Vector3[][] originalVertices,
        TMP_CharacterInfo character,
        Vector3 center,
        float scale)
    {
        int materialIndex =
            character.materialReferenceIndex;

        int vertexIndex =
            character.vertexIndex;

        Vector3[] source =
            originalVertices[
                materialIndex
            ];

        Vector3[] destination =
            textInfo.meshInfo[
                materialIndex
            ].vertices;

        for (int j = 0; j < 4; j++)
        {
            if (!config.animateScale)
            {
                destination[
                    vertexIndex + j
                ] =
                    source[
                        vertexIndex + j
                    ];

                continue;
            }

            Vector3 offset =
                source[
                    vertexIndex + j
                ] - center;

            destination[
                vertexIndex + j
            ] =
                center +
                offset * scale;
        }
    }

    private static void ApplyCharacterColors(
        TMPTextAnimationConfig config,
        TMP_TextInfo textInfo,
        Color32[][] originalColors,
        TMP_CharacterInfo character,
        float opacity)
    {
        int materialIndex =
            character.materialReferenceIndex;

        int vertexIndex =
            character.vertexIndex;

        Color32[] source =
            originalColors[
                materialIndex
            ];

        Color32[] destination =
            textInfo.meshInfo[
                materialIndex
            ].colors32;

        for (int j = 0; j < 4; j++)
        {
            Color32 color =
                source[
                    vertexIndex + j
                ];

            if (config.animateOpacity)
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
                vertexIndex + j
            ] = color;
        }
    }

    private static Vector3 GetRangeCenter(
        TMP_TextInfo textInfo,
        Vector3[][] originalVertices,
        int firstCharacter,
        int lastCharacter)
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

        for (int i = firstCharacter;
             i < lastCharacter;
             i++)
        {
            TMP_CharacterInfo character =
                textInfo.characterInfo[i];

            if (!character.isVisible)
                continue;

            found = true;

            int materialIndex =
                character.materialReferenceIndex;

            int vertexIndex =
                character.vertexIndex;

            Vector3[] vertices =
                originalVertices[
                    materialIndex
                ];

            for (int j = 0; j < 4; j++)
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
}