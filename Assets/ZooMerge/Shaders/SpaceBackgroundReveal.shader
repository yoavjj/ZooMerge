Shader "Custom/UI/SpaceBackgroundReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Space Background", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Reveal)]
        _Reveal ("Reveal", Range(0,1)) = 0
        _RevealCenter ("Reveal Center", Vector) = (0.5,0.5,0,0)
        _RevealSoftness ("Reveal Softness", Range(0.001,0.25)) = 0.04

        [Header(Nebula Edge)]
        _NoiseScale ("Noise Scale", Float) = 7
        _NoiseStrength ("Noise Strength", Range(0,0.25)) = 0.07
        _NoiseSpeed ("Noise Speed", Float) = 0.15

        [Header(Cosmic Glow)]
        _GlowWidth ("Glow Width", Range(0.001,0.25)) = 0.08
        _GlowStrength ("Glow Strength", Range(0,8)) = 2
        _EndGlowFadeStart ("End Glow Fade Start", Range(0.5,1)) = 0.82
        _GlowColorA ("Glow Color A", Color) = (0.05,0.85,1,1)
        _GlowColorB ("Glow Color B", Color) = (0.75,0.2,1,1)

        [Header(Edge Stars)]
        _StarDensity ("Star Density", Range(4,80)) = 30
        _StarStrength ("Star Strength", Range(0,4)) = 1.2

        [Header(UI)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

            fixed4 _Color;

            float _Reveal;
            float4 _RevealCenter;
            float _RevealSoftness;

            float _NoiseScale;
            float _NoiseStrength;
            float _NoiseSpeed;

            float _GlowWidth;
            float _GlowStrength;
            float _EndGlowFadeStart;

            fixed4 _GlowColorA;
            fixed4 _GlowColorB;

            float _StarDensity;
            float _StarStrength;

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

            float Hash21(float2 p)
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

            float GetMaxDistanceFromCenter(
                float2 center)
            {
                float d0 =
                    distance(
                        center,
                        float2(0, 0)
                    );

                float d1 =
                    distance(
                        center,
                        float2(1, 0)
                    );

                float d2 =
                    distance(
                        center,
                        float2(0, 1)
                    );

                float d3 =
                    distance(
                        center,
                        float2(1, 1)
                    );

                return max(
                    max(d0, d1),
                    max(d2, d3)
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

                float random =
                    Hash21(cell);

                float visible =
                    step(
                        0.965,
                        random
                    );

                float distanceToCenter =
                    length(local);

                float star =
                    saturate(
                        0.025 /
                        max(
                            distanceToCenter,
                            0.001
                        )
                    );

                float twinkle =
                    0.55 +
                    0.45 *
                    sin(
                        timeValue * 7 +
                        random * 40
                    );

                return
                    visible *
                    star *
                    twinkle *
                    _StarStrength;
            }

            v2f vert(appdata_t IN)
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
                    IN.color *
                    _Color;

                return OUT;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 space =
                    tex2D(
                        _MainTex,
                        i.uv
                    );

                float2 center =
                    _RevealCenter.xy;

                float maxDistance =
                    max(
                        GetMaxDistanceFromCenter(
                            center
                        ),
                        0.0001
                    );

                // --------------------------------
                // RADIAL COSMIC REVEAL
                // --------------------------------

                float radial =
                    distance(
                        i.uv,
                        center
                    ) /
                    maxDistance;

                float noise =
                    ValueNoise(
                        i.uv *
                        _NoiseScale +
                        float2(
                            _Time.y * _NoiseSpeed,
                            -_Time.y *
                            _NoiseSpeed * 0.7
                        )
                    );

                noise =
                    (noise - 0.5) *
                    _NoiseStrength;

                float revealValue =
                    radial +
                    noise;

                float revealMask =
                    1.0 -
                    smoothstep(
                        _Reveal -
                        _RevealSoftness,
                        _Reveal +
                        _RevealSoftness,
                        revealValue
                    );

                const float deadZone = 0.01;

                if (_Reveal <= deadZone)
                    revealMask = 0;

                if (_Reveal >= 1.0 - deadZone)
                    revealMask = 1;

                // --------------------------------
                // BLACK -> SPACE
                // --------------------------------

                fixed3 color =
                    lerp(
                        fixed3(0, 0, 0),
                        space.rgb,
                        revealMask
                    );

            // --------------------------------
            // SMOOTH END FADE
            // --------------------------------

            float endFade =
                1.0 -
                smoothstep(
                    _EndGlowFadeStart,
                    1.0,
                    _Reveal
                );

            // Shrink the physical width of the glow
            // as the reveal approaches completion.
            float currentGlowWidth =
                max(
                    _GlowWidth * endFade,
                    0.001
                );

            float revealEdge =
                1.0 -
                saturate(
                    abs(
                        revealValue -
                        _Reveal
                    ) /
                    currentGlowWidth
                );

            // Also fade its intensity smoothly.
            revealEdge *= endFade;

            // Still prevent glow at the exact beginning.
            if (_Reveal <= deadZone)
                revealEdge = 0;

                float glowNoise =
                    ValueNoise(
                        i.uv *
                        (_NoiseScale * 0.55) +
                        float2(
                            -_Time.y * 0.08,
                            _Time.y * 0.12
                        )
                    );

                fixed3 glowColor =
                    lerp(
                        _GlowColorA.rgb,
                        _GlowColorB.rgb,
                        glowNoise
                    );

                color +=
                    glowColor *
                    revealEdge *
                    _GlowStrength;

                // --------------------------------
                // LITTLE STARS ON THE WAVE
                // --------------------------------

                float stars =
                    StarField(
                        i.uv +
                        float2(
                            _Time.y * 0.025,
                            -_Time.y * 0.02
                        ),
                        _Time.y
                    );

                color +=
                    glowColor *
                    stars *
                    revealEdge;

                fixed4 finalColor;

                finalColor.rgb =
                    color *
                    i.color.rgb;

                finalColor.a =
                    space.a *
                    i.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *=
                    UnityGet2DClipping(
                        i.worldPosition.xy,
                        _ClipRect
                    );
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(
                    finalColor.a - 0.001
                );
                #endif

                return finalColor;
            }

            ENDCG
        }
    }
}