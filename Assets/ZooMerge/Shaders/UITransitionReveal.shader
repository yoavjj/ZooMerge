Shader "Custom/UI/TransitionReveal"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Gradient)]
        _ColorA ("Gradient Color A", Color) = (0.05,0.15,0.55,1)
        _ColorB ("Gradient Color B", Color) = (0.15,0.45,1,1)

        _GradientDirection (
            "Gradient Direction",
            Vector
        ) = (0,1,0,0)

        _GradientPower (
            "Gradient Power",
            Range(0.2,4)
        ) = 1

        [Header(Reveal)]
        _Reveal (
            "Reveal In",
            Range(0,1)
        ) = 0

        _OutReveal (
            "Reveal Out",
            Range(0,1)
        ) = 0

        _RevealSoftness (
            "Reveal Softness",
            Range(0.001,0.2)
        ) = 0.03

        [Header(Wipe Direction)]
        _WipeDirection (
            "Wipe Direction",
            Vector
        ) = (0,-1,0,0)

        [Header(Edge Shape)]
        _NoiseScale (
            "Noise Scale",
            Float
        ) = 7

        _NoiseStrength (
            "Noise Strength",
            Range(0,0.25)
        ) = 0.05

        [Header(Glow)]
        _GlowColor (
            "Glow Color",
            Color
        ) = (0.1,0.8,1,1)

        _GlowWidth (
            "Glow Width",
            Range(0.001,0.25)
        ) = 0.06

        _GlowStrength (
            "Glow Strength",
            Range(0,8)
        ) = 2

        _GlowAlpha (
            "Glow Alpha",
            Range(0,1)
        ) = 0.65

        [Header(UI)]
        _StencilComp (
            "Stencil Comparison",
            Float
        ) = 8

        _Stencil (
            "Stencil ID",
            Float
        ) = 0

        _StencilOp (
            "Stencil Operation",
            Float
        ) = 0

        _StencilWriteMask (
            "Stencil Write Mask",
            Float
        ) = 255

        _StencilReadMask (
            "Stencil Read Mask",
            Float
        ) = 255

        _ColorMask (
            "Color Mask",
            Float
        ) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip (
            "Use Alpha Clip",
            Float
        ) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off

        ZTest [unity_GUIZTestMode]

        Blend SrcAlpha OneMinusSrcAlpha

        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            sampler2D _MainTex;

            fixed4 _ColorA;
            fixed4 _ColorB;

            float4 _GradientDirection;
            float _GradientPower;

            float _Reveal;
            float _OutReveal;
            float _RevealSoftness;

            float4 _WipeDirection;

            float _NoiseScale;
            float _NoiseStrength;

            fixed4 _GlowColor;
            float _GlowWidth;
            float _GlowStrength;
            float _GlowAlpha;

            float4 _ClipRect;

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
                float4 worldPosition : TEXCOORD1;
            };

            // =====================================================
            // NOISE
            // =====================================================

            float Hash21(
                float2 p)
            {
                p = frac(
                    p *
                    float2(
                        123.34,
                        456.21
                    )
                );

                p += dot(
                    p,
                    p + 45.32
                );

                return frac(
                    p.x *
                    p.y
                );
            }

            float ValueNoise(
                float2 uv)
            {
                float2 cell =
                    floor(
                        uv
                    );

                float2 local =
                    frac(
                        uv
                    );

                float a =
                    Hash21(
                        cell
                    );

                float b =
                    Hash21(
                        cell +
                        float2(
                            1,
                            0
                        )
                    );

                float c =
                    Hash21(
                        cell +
                        float2(
                            0,
                            1
                        )
                    );

                float d =
                    Hash21(
                        cell +
                        float2(
                            1,
                            1
                        )
                    );

                float2 smoothLocal =
                    local *
                    local *
                    (
                        3.0 -
                        2.0 *
                        local
                    );

                return lerp(
                    lerp(
                        a,
                        b,
                        smoothLocal.x
                    ),
                    lerp(
                        c,
                        d,
                        smoothLocal.x
                    ),
                    smoothLocal.y
                );
            }

            // =====================================================
            // DIRECTION
            // =====================================================

            float GetDirectionalValue(
                float2 uv,
                float2 direction)
            {
                if (
                    dot(
                        direction,
                        direction
                    ) <
                    0.0001
                )
                {
                    direction =
                        float2(
                            0,
                            -1
                        );
                }

                direction =
                    normalize(
                        direction
                    );

                float value =
                    dot(
                        uv,
                        direction
                    );

                float corner0 =
                    dot(
                        float2(0,0),
                        direction
                    );

                float corner1 =
                    dot(
                        float2(1,0),
                        direction
                    );

                float corner2 =
                    dot(
                        float2(0,1),
                        direction
                    );

                float corner3 =
                    dot(
                        float2(1,1),
                        direction
                    );

                float minValue =
                    min(
                        min(
                            corner0,
                            corner1
                        ),
                        min(
                            corner2,
                            corner3
                        )
                    );

                float maxValue =
                    max(
                        max(
                            corner0,
                            corner1
                        ),
                        max(
                            corner2,
                            corner3
                        )
                    );

                return saturate(
                    (
                        value -
                        minValue
                    ) /
                    max(
                        maxValue -
                        minValue,
                        0.0001
                    )
                );
            }

            // =====================================================
            // VERTEX
            // =====================================================

            v2f vert(
                appdata_t IN)
            {
                v2f OUT;

                OUT.worldPosition =
                    IN.vertex;

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

            // =====================================================
            // FRAGMENT
            // =====================================================

            fixed4 frag(
                v2f i) : SV_Target
            {
                fixed4 sprite =
                    tex2D(
                        _MainTex,
                        i.uv
                    );

                // =================================================
                // GRADIENT
                // =================================================

                float2 gradientDirection =
                    _GradientDirection.xy;

                if (
                    dot(
                        gradientDirection,
                        gradientDirection
                    ) <
                    0.0001
                )
                {
                    gradientDirection =
                        float2(
                            0,
                            1
                        );
                }

                gradientDirection =
                    normalize(
                        gradientDirection
                    );

                float gradient =
                    dot(
                        i.uv - 0.5,
                        gradientDirection
                    ) +
                    0.5;

                gradient =
                    saturate(
                        gradient
                    );

                gradient =
                    pow(
                        gradient,
                        _GradientPower
                    );

                fixed4 baseColor =
                    lerp(
                        _ColorA,
                        _ColorB,
                        gradient
                    );

                // =================================================
                // WIPE POSITION
                // =================================================

                float revealValue =
                    GetDirectionalValue(
                        i.uv,
                        _WipeDirection.xy
                    );

                float noise =
                    ValueNoise(
                        i.uv *
                        _NoiseScale
                    );

                noise =
                    (
                        noise -
                        0.5
                    ) *
                    _NoiseStrength;

                revealValue +=
                    noise;

                // =================================================
                // REVEAL IN
                // =================================================

                float inMask =
                    1.0 -
                    smoothstep(
                        _Reveal -
                        _RevealSoftness,
                        _Reveal +
                        _RevealSoftness,
                        revealValue
                    );

                if (_Reveal <= 0.001)
                {
                    inMask = 0;
                }

                if (_Reveal >= 0.999)
                {
                    inMask = 1;
                }

                // =================================================
                // REVEAL OUT
                // =================================================

                float outMask =
                    1.0 -
                    smoothstep(
                        _OutReveal -
                        _RevealSoftness,
                        _OutReveal +
                        _RevealSoftness,
                        revealValue
                    );

                if (_OutReveal <= 0.001)
                {
                    outMask = 0;
                }

                if (_OutReveal >= 0.999)
                {
                    outMask = 1;
                }

                // Reveal In paints the overlay.
                //
                // Reveal Out removes it using
                // exactly the same direction.

                float finalMask =
                    inMask *
                    (
                        1.0 -
                        outMask
                    );

                // =================================================
                // REVEAL IN GLOW
                // =================================================

                float inEdgeDistance =
                    abs(
                        revealValue -
                        _Reveal
                    );

                float inGlow =
                    1.0 -
                    saturate(
                        inEdgeDistance /
                        max(
                            _GlowWidth,
                            0.0001
                        )
                    );

                inGlow *=
                    inGlow;

                float inGlowLife =
                    saturate(
                        min(
                            _Reveal,
                            1.0 -
                            _Reveal
                        ) *
                        12.0
                    );

                inGlow *=
                    inGlowLife;

                // =================================================
                // REVEAL OUT GLOW
                // =================================================

                float outEdgeDistance =
                    abs(
                        revealValue -
                        _OutReveal
                    );

                float outGlow =
                    1.0 -
                    saturate(
                        outEdgeDistance /
                        max(
                            _GlowWidth,
                            0.0001
                        )
                    );

                outGlow *=
                    outGlow;

                float outGlowLife =
                    saturate(
                        min(
                            _OutReveal,
                            1.0 -
                            _OutReveal
                        ) *
                        12.0
                    );

                outGlow *=
                    outGlowLife;

                float glow =
                    max(
                        inGlow,
                        outGlow
                    );

                // =================================================
                // FINAL COLOR
                // =================================================

                fixed3 finalRGB =
                    baseColor.rgb;

                finalRGB +=
                    _GlowColor.rgb *
                    glow *
                    _GlowStrength;

                fixed4 finalColor;

                finalColor.rgb =
                    finalRGB *
                    i.color.rgb;

                float overlayAlpha =
                    baseColor.a *
                    sprite.a *
                    finalMask;

                float glowAlpha =
                    glow *
                    _GlowAlpha *
                    _GlowColor.a *
                    sprite.a;

                finalColor.a =
                    max(
                        overlayAlpha,
                        glowAlpha
                    ) *
                    i.color.a;

                // =================================================
                // UI CLIPPING
                // =================================================

                #ifdef UNITY_UI_CLIP_RECT

                finalColor.a *=
                    UnityGet2DClipping(
                        i.worldPosition.xy,
                        _ClipRect
                    );

                #endif

                #ifdef UNITY_UI_ALPHACLIP

                clip(
                    finalColor.a -
                    0.001
                );

                #endif

                return finalColor;
            }

            ENDCG
        }
    }
}