Shader "Custom/SpaceshipSkinReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Current Skin", 2D) = "white" {}
        _NextTex ("Next Skin", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Reveal)]
        _Blend ("Blend", Range(0,1)) = 0
        _RevealWidth ("Reveal Width", Range(0.001,0.25)) = 0.06
        _NoiseScale ("Noise Scale", Float) = 10
        _NoiseStrength ("Noise Strength", Range(0,0.3)) = 0.08

        [Header(Reveal Glow)]
        _RevealGlowStrength ("Reveal Glow Strength", Range(0,5)) = 1.2
        _GlowColorA ("Glow Color A", Color) = (0.10,0.90,1.00,1.00)
        _GlowColorB ("Glow Color B", Color) = (0.85,0.30,1.00,1.00)

        [Header(Sprite Edge)]
        _SpriteEdgeWidth ("Sprite Edge Width", Range(0.5,8)) = 2
        _SpriteEdgeStrength ("Sprite Edge Strength", Range(0,8)) = 1.5
        _SpriteEdgeColor ("Sprite Edge Color", Color) = (0.1,0.85,1,1)

        [Header(Stars)]
        _StarDensity ("Star Density", Range(2,60)) = 18
        _StarIntensity ("Star Intensity", Range(0,2)) = 0.7
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _NextTex;

            float4 _MainTex_TexelSize;
            float4 _NextTex_TexelSize;

            fixed4 _Color;

            float _Blend;
            float _RevealWidth;
            float _NoiseScale;
            float _NoiseStrength;

            float _RevealGlowStrength;

            fixed4 _GlowColorA;
            fixed4 _GlowColorB;

            float _SpriteEdgeWidth;
            float _SpriteEdgeStrength;
            fixed4 _SpriteEdgeColor;

            float _StarDensity;
            float _StarIntensity;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            float Hash21(float2 p)
            {
                p = frac(
                    p * float2(
                        123.34,
                        456.21
                    )
                );

                p += dot(
                    p,
                    p + 45.32
                );

                return frac(
                    p.x * p.y
                );
            }

            float ValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                float2 u =
                    f * f *
                    (3.0 - 2.0 * f);

                return lerp(
                    lerp(a, b, u.x),
                    lerp(c, d, u.x),
                    u.y
                );
            }

            float StarField(
                float2 uv,
                float timeValue)
            {
                float2 p =
                    uv * _StarDensity;

                float2 cell =
                    floor(p);

                float2 local =
                    frac(p) - 0.5;

                float rnd =
                    Hash21(cell);

                float starMask =
                    step(0.965, rnd);

                float twinkle =
                    0.5 +
                    0.5 *
                    sin(
                        timeValue * 6.0 +
                        rnd * 30.0
                    );

                float d =
                    length(local);

                float star =
                    saturate(
                        0.03 /
                        max(d, 0.001)
                    );

                return
                    starMask *
                    star *
                    twinkle *
                    _StarIntensity;
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;

                OUT.vertex =
                    UnityObjectToClipPos(
                        IN.vertex
                    );

                OUT.uv =
                    IN.texcoord;

                OUT.color =
                    IN.color;

                return OUT;
            }

            float GetCombinedAlpha(float2 uv)
            {
                float oldAlpha =
                    tex2D(
                        _MainTex,
                        uv
                    ).a;

                float newAlpha =
                    tex2D(
                        _NextTex,
                        uv
                    ).a;

                return max(
                    oldAlpha,
                    newAlpha
                );
            }

            float GetSpriteEdge(
                float2 uv,
                float centerAlpha)
            {
                float2 texel =
                    max(
                        _MainTex_TexelSize.xy,
                        _NextTex_TexelSize.xy
                    );

                texel *=
                    _SpriteEdgeWidth;

                float left =
                    GetCombinedAlpha(
                        uv + float2(-texel.x, 0)
                    );

                float right =
                    GetCombinedAlpha(
                        uv + float2(texel.x, 0)
                    );

                float up =
                    GetCombinedAlpha(
                        uv + float2(0, texel.y)
                    );

                float down =
                    GetCombinedAlpha(
                        uv + float2(0, -texel.y)
                    );

                float edge = 0;

                edge = max(
                    edge,
                    abs(centerAlpha - left)
                );

                edge = max(
                    edge,
                    abs(centerAlpha - right)
                );

                edge = max(
                    edge,
                    abs(centerAlpha - up)
                );

                edge = max(
                    edge,
                    abs(centerAlpha - down)
                );

                // Keep the glow INSIDE
                // the actual spaceship silhouette.
                edge *= centerAlpha;

                return saturate(edge);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 oldCol =
                    tex2D(
                        _MainTex,
                        i.uv
                    );

                fixed4 newCol =
                    tex2D(
                        _NextTex,
                        i.uv
                    );

                float combinedAlpha =
                    max(
                        oldCol.a,
                        newCol.a
                    );

                // Completely discard pixels outside
                // both spaceship sprites.
                clip(
                    combinedAlpha - 0.001
                );

                // --------------------------------
                // REVEAL
                // --------------------------------

                float revealAxis =
                    (i.uv.x + i.uv.y) *
                    0.5;

                float noise =
                    ValueNoise(
                        i.uv *
                        _NoiseScale +
                        float2(
                            _Time.y * 0.25,
                            -_Time.y * 0.18
                        )
                    );

                noise =
                    (noise - 0.5) *
                    _NoiseStrength;

                float revealValue =
                    revealAxis +
                    noise;

                // IMPORTANT:
                // Blend 0 = OLD
                // Blend 1 = NEW
                float revealMask =
                    1.0 -
                    smoothstep(
                        _Blend - _RevealWidth,
                        _Blend + _RevealWidth,
                        revealValue
                    );

                // Force clean endpoints with a larger safety zone.
                const float blendDeadZone = 0.03;

                if (_Blend <= blendDeadZone)
                    revealMask = 0;

                if (_Blend >= 1.0 - blendDeadZone)
                    revealMask = 1;
                fixed4 baseCol =
                    lerp(
                        oldCol,
                        newCol,
                        revealMask
                    );

                // --------------------------------
                // MOVING REVEAL EDGE
                // --------------------------------

                float revealEdge =
                    1.0 -
                    saturate(
                        abs(
                            revealValue -
                            _Blend
                        ) /
                        max(
                            _RevealWidth,
                            0.0001
                        )
                    );

                revealEdge *=
                    combinedAlpha;

                    // Hide all reveal FX near the exact start/end.
                    if (_Blend <= blendDeadZone ||
                        _Blend >= 1.0 - blendDeadZone)
                    {
                        revealEdge = 0;
                    }

                float glowNoise =
                    ValueNoise(
                        i.uv *
                        (_NoiseScale * 0.6) +
                        float2(
                            _Time.y * 0.1,
                            _Time.y * 0.12
                        )
                    );

                fixed3 revealGlowColor =
                    lerp(
                        _GlowColorA.rgb,
                        _GlowColorB.rgb,
                        glowNoise
                    );

                float stars =
                    StarField(
                        i.uv +
                        float2(
                            _Time.y * 0.05,
                            -_Time.y * 0.03
                        ),
                        _Time.y
                    );

                baseCol.rgb +=
                    revealGlowColor *
                    revealEdge *
                    _RevealGlowStrength;

                baseCol.rgb +=
                    revealGlowColor *
                    stars *
                    revealEdge;

                // --------------------------------
                // ACTUAL SPRITE SILHOUETTE EDGE
                // --------------------------------

                float spriteEdge =
                    GetSpriteEdge(
                        i.uv,
                        combinedAlpha
                    );

                baseCol.rgb +=
                    _SpriteEdgeColor.rgb *
                    spriteEdge *
                    _SpriteEdgeStrength;

                // Never let transparent polygon area
                // become visible.
                baseCol.a =
                    min(
                        baseCol.a,
                        combinedAlpha
                    );

                baseCol *=
                    i.color *
                    _Color;

                return baseCol;
            }

            ENDCG
        }
    }
}