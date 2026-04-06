Shader "UI/VortexPulseGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _GlowColor ("Glow Color", Color) = (1,0.6,1,1)
        _GlowStrength ("Glow Strength", Range(0,2)) = 0.4
        _PulseSpeed ("Pulse Speed", Range(0,10)) = 1.2
        _PulseAmount ("Pulse Amount", Range(0,1)) = 0.25
        _HueShift ("Hue Shift Amount", Range(0,1)) = 0.05

        _Opacity ("Opacity", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
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

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            fixed4 _GlowColor;
            half _GlowStrength;
            half _PulseSpeed;
            half _PulseAmount;
            half _HueShift;
            half _Opacity;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // quick RGB<->HSV helpers (lightweight)
            float3 rgb2hsv(float3 c)
            {
                float4 K = float4(0., -1./3., 2./3., -1.);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.*d + e)), d / (q.x + e), q.x);
            }

            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1., 2./3., 1./3., 3.);
                float3 p = abs(frac(c.xxx + K.xyz) * 6. - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * i.color;

                // Pulse 0..1
                float t = _Time.y * _PulseSpeed;
                float pulse = 0.5 + 0.5 * sin(t);

                // Small hue shift over time
                float3 hsv = rgb2hsv(tex.rgb);
                hsv.x = frac(hsv.x + (pulse - 0.5) * _HueShift);
                float3 shifted = hsv2rgb(hsv);

                // "Glow" is just a brightness/tint lift (looks great on space art)
                float glow = 1.0 + _GlowStrength * (pulse * _PulseAmount);
                float3 glowTint = lerp(shifted, shifted + _GlowColor.rgb * _GlowStrength, 0.5);

                fixed4 col;
                col.rgb = glowTint * glow;
                col.a = tex.a * _Opacity;
                return col;
            }
            ENDCG
        }
    }
}