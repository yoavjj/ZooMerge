Shader "Custom/UI/GroupRevealMask"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Reveal)]
        _Reveal ("Reveal", Range(0,1)) = 0
        _RevealCenter ("Reveal Center", Vector) = (0.5,0.5,0,0)

        [Header(Edge)]
        _NoiseScale ("Noise Scale", Float) = 8
        _NoiseStrength ("Noise Strength", Range(0,0.3)) = 0.06

        [Header(UI)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip ("Use Alpha Clip", Float) = 1
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

            float _NoiseScale;
            float _NoiseStrength;

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
                float2 i =
                    floor(uv);

                float2 f =
                    frac(uv);

                float a =
                    Hash21(i);

                float b =
                    Hash21(
                        i +
                        float2(1, 0)
                    );

                float c =
                    Hash21(
                        i +
                        float2(0, 1)
                    );

                float d =
                    Hash21(
                        i +
                        float2(1, 1)
                    );

                float2 u =
                    f * f *
                    (3.0 - 2.0 * f);

                return lerp(
                    lerp(
                        a,
                        b,
                        u.x
                    ),
                    lerp(
                        c,
                        d,
                        u.x
                    ),
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
                    max(
                        d0,
                        d1
                    ),
                    max(
                        d2,
                        d3
                    )
                );
            }

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
                    IN.color *
                    _Color;

                return OUT;
            }

            fixed4 frag(
                v2f i) : SV_Target
            {
                fixed4 tex =
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

                float radial =
                    distance(
                        i.uv,
                        center
                    ) /
                    maxDistance;

                float noise =
                    ValueNoise(
                        i.uv *
                        _NoiseScale
                    );

                noise =
                    (noise - 0.5) *
                    _NoiseStrength;

                float revealValue =
                    radial +
                    noise;

                // Completely hidden.
                if (_Reveal <= 0.001)
                {
                    clip(-1);
                }

                // During reveal:
                // discard everything outside
                // the expanding boundary.
                if (_Reveal < 0.999)
                {
                    clip(
                        _Reveal -
                        revealValue
                    );
                }

                fixed4 finalColor =
                    tex *
                    i.color;

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