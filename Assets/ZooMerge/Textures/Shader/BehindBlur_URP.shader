Shader "Custom/UI/BehindBlur_URP"
{
    Properties
    {
        // Required for UI Images
        [PerRendererData]_MainTex ("Sprite Texture", 2D) = "white" {}

        _Tint ("Tint (RGB) + Opacity (A)", Color) = (0.8,0.9,1.0,0.55)

        _BlurRadius ("Blur Radius", Range(0,6)) = 2.5
        _Distortion ("Distortion", Range(0,2)) = 0.35
        _NoiseScale ("Noise Scale", Range(5,200)) = 70
        _NoiseStrength ("Noise Strength", Range(0,1)) = 0.25

        _Brightness ("Brightness", Range(0.5,1.5)) = 1.0
        _Contrast ("Contrast", Range(0.5,1.5)) = 1.05
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex; // UI expects it

            sampler2D _CameraOpaqueTexture;
            float4 _CameraOpaqueTexture_TexelSize;

            float4 _Tint;
            float _BlurRadius;
            float _Distortion;
            float _NoiseScale;
            float _NoiseStrength;
            float _Brightness;
            float _Contrast;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

            // Cheap hash noise (no texture needed)
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 noise2(float2 p)
            {
                float n1 = hash21(p);
                float n2 = hash21(p + 17.13);
                return float2(n1, n2) * 2 - 1;
            }

            float3 ApplyBC(float3 c, float brightness, float contrast)
            {
                c *= brightness;
                c = (c - 0.5) * contrast + 0.5;
                return c;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;

                // Frost noise in screen space so it doesn't swim with UI scaling
                float2 nUV = uv * _NoiseScale;
                float2 n = noise2(nUV);

                // Distort sampling UV (frosted glass refraction)
                float2 distUV = uv + n * (_Distortion * _CameraOpaqueTexture_TexelSize.xy);

                float2 texel = _CameraOpaqueTexture_TexelSize.xy * _BlurRadius;

                // Stronger blur: 13 taps (center + ring)
                float4 c = 0;
                c += tex2D(_CameraOpaqueTexture, distUV) * 0.22;

                c += tex2D(_CameraOpaqueTexture, distUV + texel * float2( 1, 0)) * 0.08;
                c += tex2D(_CameraOpaqueTexture, distUV + texel * float2(-1, 0)) * 0.08;
                c += tex2D(_CameraOpaqueTexture, distUV + texel * float2( 0, 1)) * 0.08;
                c += tex2D(_CameraOpaqueTexture, distUV + texel * float2( 0,-1)) * 0.08;

                c += tex2D(_CameraOpaqueTexture, distUV + texel * float2( 1, 1)) * 0.06;
                c += tex2D(_CameraOpaqueTexture, distUV + texel * float2(-1, 1)) * 0.06;
                c += tex2D(_CameraOpaqueTexture, distUV + texel * float2( 1,-1)) * 0.06;
                c += tex2D(_CameraOpaqueTexture, distUV + texel * float2(-1,-1)) * 0.06;

                c += tex2D(_CameraOpaqueTexture, distUV + texel * float2( 2, 0)) * 0.05;
                c += tex2D(_CameraOpaqueTexture, distUV + texel * float2(-2, 0)) * 0.05;
                c += tex2D(_CameraOpaqueTexture, distUV + texel * float2( 0, 2)) * 0.05;
                c += tex2D(_CameraOpaqueTexture, distUV + texel * float2( 0,-2)) * 0.05;

                // Add subtle grain so it looks "frosted" not just blurred
                float grain = hash21(nUV + 33.7);
                c.rgb = lerp(c.rgb, c.rgb + (grain - 0.5) * 0.15, _NoiseStrength);

                // Tint + opacity
                c.rgb = ApplyBC(c.rgb, _Brightness, _Contrast);
                c.rgb = lerp(c.rgb, _Tint.rgb, _Tint.a);

                c.a = _Tint.a;
                return c;
            }
            ENDHLSL
        }
    }
}