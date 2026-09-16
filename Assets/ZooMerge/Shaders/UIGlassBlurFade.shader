Shader "Custom/UIGlassBlurFade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Glass Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Glass)]
        _GlassFade ("Glass Fade", Range(0,1)) = 1
        _BlurStrength ("Blur Strength", Range(0,20)) = 0
        _GlassStrength ("Glass Strength", Range(0,2)) = 1

        [Header(UI)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            fixed4 _Color;

            float _GlassFade;
            float _BlurStrength;
            float _GlassStrength;

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_t IN)
            {
                v2f OUT;

                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv = IN.texcoord;
                OUT.color = IN.color;

                return OUT;
            }

            fixed4 SampleBlurredGlass(float2 uv)
            {
                if (_BlurStrength <= 0.001)
                    return tex2D(_MainTex, uv);

                float2 texel =
                    _MainTex_TexelSize.xy *
                    _BlurStrength;

                fixed4 result = 0;

                result += tex2D(_MainTex, uv) * 0.16;

                result += tex2D(
                    _MainTex,
                    uv + float2(texel.x, 0)
                ) * 0.08;

                result += tex2D(
                    _MainTex,
                    uv - float2(texel.x, 0)
                ) * 0.08;

                result += tex2D(
                    _MainTex,
                    uv + float2(0, texel.y)
                ) * 0.08;

                result += tex2D(
                    _MainTex,
                    uv - float2(0, texel.y)
                ) * 0.08;

                result += tex2D(
                    _MainTex,
                    uv + float2(texel.x, texel.y)
                ) * 0.07;

                result += tex2D(
                    _MainTex,
                    uv + float2(-texel.x, texel.y)
                ) * 0.07;

                result += tex2D(
                    _MainTex,
                    uv + float2(texel.x, -texel.y)
                ) * 0.07;

                result += tex2D(
                    _MainTex,
                    uv + float2(-texel.x, -texel.y)
                ) * 0.07;

                float2 farTexel =
                    texel * 2.0;

                result += tex2D(
                    _MainTex,
                    uv + float2(farTexel.x, 0)
                ) * 0.06;

                result += tex2D(
                    _MainTex,
                    uv - float2(farTexel.x, 0)
                ) * 0.06;

                result += tex2D(
                    _MainTex,
                    uv + float2(0, farTexel.y)
                ) * 0.06;

                result += tex2D(
                    _MainTex,
                    uv - float2(0, farTexel.y)
                ) * 0.06;

                return result;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 glass =
                    SampleBlurredGlass(i.uv);

                glass *=
                    i.color *
                    _Color;

                glass.rgb *=
                    _GlassStrength;

                glass.a *=
                    _GlassFade;

                if (glass.a <= 0.001)
                    discard;

                return glass;
            }

            ENDCG
        }
    }
}