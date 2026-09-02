Shader "VFX/LightBeam_Volumetric2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _BeamColor ("Beam Color", Color) = (0.35, 0.8, 1, 1)
        _GlowColor ("Glow Color", Color) = (0.6, 0.95, 1, 1)

        _CoreWidth ("Core Width", Range(0.001, 0.5)) = 0.10
        _EdgeWidth ("Edge Softness", Range(0.001, 0.8)) = 0.28

        _CoreIntensity ("Core Intensity", Range(0, 10)) = 2.5
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 1.5

        _ScrollSpeed ("Scroll Speed", Range(-10, 10)) = 1.2
        _BandFrequency ("Band Frequency", Range(0.5, 40)) = 10
        _BandStrength ("Band Strength", Range(0, 1)) = 0.25

        _FlickerSpeed ("Flicker Speed", Range(0, 40)) = 10
        _FlickerAmount ("Flicker Amount", Range(0, 1)) = 0.15

        _TopFade ("Top Fade", Range(0,1)) = 0.15
        _BottomFade ("Bottom Fade", Range(0,1)) = 0.10

        [Header(Reveal)]
        _Reveal ("Reveal", Range(0,1)) = 0
        _RevealSoftness ("Reveal Softness", Range(0.001,0.25)) = 0.04

        _Opacity ("Opacity", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull Off
        ZWrite Off
        Blend One One

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;

            fixed4 _BeamColor;
            fixed4 _GlowColor;

            float _CoreWidth;
            float _EdgeWidth;
            float _CoreIntensity;
            float _GlowIntensity;

            float _ScrollSpeed;
            float _BandFrequency;
            float _BandStrength;

            float _FlickerSpeed;
            float _FlickerAmount;

            float _TopFade;
            float _BottomFade;

            float _Reveal;
            float _RevealSoftness;

            float _Opacity;

            v2f vert(appdata v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;

                return o;
            }

            float hash21(float2 p)
            {
                p = frac(
                    p * float2(
                        123.34,
                        345.45
                    )
                );

                p += dot(
                    p,
                    p + 34.345
                );

                return frac(
                    p.x * p.y
                );
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 sprite =
                    tex2D(
                        _MainTex,
                        i.uv
                    );

                float mask =
                    sprite.a *
                    i.color.a;

                // --------------------------------
                // BEAM WIDTH
                // --------------------------------

                float across =
                    abs(
                        i.uv.x - 0.5
                    );

                float core =
                    1.0 -
                    smoothstep(
                        _CoreWidth,
                        _CoreWidth * 1.5,
                        across
                    );

                float edge =
                    1.0 -
                    smoothstep(
                        _EdgeWidth,
                        _EdgeWidth * 1.5,
                        across
                    );

                float v =
                    i.uv.y;

                float t =
                    _Time.y;

                // --------------------------------
                // MOVING BANDS
                // --------------------------------

                float band =
                    sin(
                        (v * _BandFrequency) +
                        (t * _ScrollSpeed)
                    ) *
                    0.5 +
                    0.5;

                float bandMul =
                    1.0 +
                    (band - 0.5) *
                    2.0 *
                    _BandStrength;

                // --------------------------------
                // FLICKER
                // --------------------------------

                float n =
                    hash21(
                        float2(
                            v * 30.0,
                            t * _FlickerSpeed
                        )
                    );

                float flickerMul =
                    1.0 +
                    (n - 0.5) *
                    2.0 *
                    _FlickerAmount;

                // --------------------------------
                // HEIGHT FADES
                // --------------------------------

                float topFade =
                    smoothstep(
                        1.0 - _TopFade,
                        1.0,
                        v
                    );

                float bottomFade =
                    1.0 -
                    smoothstep(
                        0.0,
                        _BottomFade,
                        v
                    );

                float heightMask =
                    (1.0 - topFade) *
                    bottomFade;

                // --------------------------------
                // TOP -> BOTTOM REVEAL
                // --------------------------------

                float revealThreshold =
                    1.0 - _Reveal;

                float revealMask =
                    smoothstep(
                        revealThreshold -
                        _RevealSoftness,

                        revealThreshold +
                        _RevealSoftness,

                        v
                    );

                // Perfect endpoints.
                if (_Reveal <= 0.001)
                    revealMask = 0.0;

                if (_Reveal >= 0.999)
                    revealMask = 1.0;

                // --------------------------------
                // FINAL BEAM
                // --------------------------------

                float coreAmt =
                    core *
                    _CoreIntensity *
                    bandMul *
                    flickerMul;

                float glowAmt =
                    edge *
                    _GlowIntensity *
                    flickerMul;

                float3 col =
                    _BeamColor.rgb *
                    coreAmt +

                    _GlowColor.rgb *
                    glowAmt;

                float finalMask =
                    mask *
                    heightMask *
                    revealMask *
                    _Opacity;

                return fixed4(
                    col * finalMask,
                    finalMask
                );
            }

            ENDCG
        }
    }
}