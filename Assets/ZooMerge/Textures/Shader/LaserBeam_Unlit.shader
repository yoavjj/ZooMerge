Shader "VFX/LaserBeam_Unlit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _MainColor ("Main Color", Color) = (1,0.2,0.2,1)
        _GlowColor ("Glow Color", Color) = (1,0.6,0.6,1)

        _CoreWidth ("Core Width", Range(0.001,0.5)) = 0.06
        _GlowWidth ("Glow Width", Range(0.001,0.8)) = 0.22

        _CoreIntensity ("Core Intensity", Range(0,10)) = 4
        _GlowIntensity ("Glow Intensity", Range(0,10)) = 2

        _PulseSpeed ("Pulse Speed", Range(0,20)) = 6
        _PulseAmount ("Pulse Amount", Range(0,1)) = 0.25
        _PulseFrequency ("Pulse Frequency", Range(0.5,30)) = 10

        _FlickerSpeed ("Flicker Speed", Range(0,40)) = 18
        _FlickerAmount ("Flicker Amount", Range(0,1)) = 0.15

        _Opacity ("Opacity", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off

        // Additive blending = laser glow look
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
                float2 uv     : TEXCOORD0; // x = along beam (0..1), y = across beam (0..1)
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            fixed4 _MainColor;
            fixed4 _GlowColor;

            sampler2D _MainTex;

            float _CoreWidth;
            float _GlowWidth;
            float _CoreIntensity;
            float _GlowIntensity;

            float _PulseSpeed;
            float _PulseAmount;
            float _PulseFrequency;

            float _FlickerSpeed;
            float _FlickerAmount;

            float _Opacity;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // cheap hash noise (no texture)
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Across-beam coordinate: center at 0, edges at 0.5
                float y = abs(i.uv.y - 0.5);

                // Core + glow profiles (smooth falloff)
                float core = 1.0 - smoothstep(_CoreWidth, _CoreWidth * 1.5, y);
                float glow = 1.0 - smoothstep(_GlowWidth, _GlowWidth * 1.5, y);

                // Pulse along length (x axis)
                float pulse = sin(i.uv.x * _PulseFrequency - _Time.y * _PulseSpeed) * 0.5 + 0.5;
                float pulseMul = 1.0 + (pulse - 0.5) * 2.0 * _PulseAmount;

                // Flicker (time + position dependent)
                float n = hash21(float2(i.uv.x * 40.0, _Time.y * _FlickerSpeed));
                float flickerMul = 1.0 + (n - 0.5) * 2.0 * _FlickerAmount;

                float coreAmt = core * _CoreIntensity * pulseMul * flickerMul;
                float glowAmt = glow * _GlowIntensity * flickerMul;

                float3 col = _MainColor.rgb * coreAmt + _GlowColor.rgb * glowAmt;

                fixed4 sprite = tex2D(_MainTex, i.uv);
                float mask = sprite.a;

                return fixed4(col * mask, _Opacity * mask);
            }
            ENDCG
        }
    }
}