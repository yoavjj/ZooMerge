Shader "Custom/HoverEnergyFlare"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.3, 0.8, 1.0, 1.0)
        _Intensity ("Intensity", Range(0, 5)) = 1.5

        _RadiusX ("Radius X", Range(0.01, 2)) = 0.45
        _RadiusY ("Radius Y", Range(0.01, 2)) = 0.18
        _EdgeSoftness ("Edge Softness", Range(0.01, 1)) = 0.25

        _CenterGlow ("Center Glow", Range(0, 3)) = 1.2
        _HorizontalFlare ("Horizontal Flare", Range(0, 3)) = 1.0

        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2.0
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Blend One One
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            fixed4 _Tint;
            float _Intensity;
            float _RadiusX;
            float _RadiusY;
            float _EdgeSoftness;
            float _CenterGlow;
            float _HorizontalFlare;
            float _PulseSpeed;
            float _PulseAmount;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Center UV around 0
                float2 p = uv - 0.5;

                // Elliptical radial falloff
                float2 ellipse = float2(
                    p.x / max(_RadiusX, 0.0001),
                    p.y / max(_RadiusY, 0.0001)
                );

                float dist = length(ellipse);

                // Soft radial body
                float glow = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, dist);

                // Hot center
                float center = saturate(1.0 - dist * 1.8);
                center = pow(center, 2.0) * _CenterGlow;

                // Horizontal flare band
                float flareBand = saturate(1.0 - abs(p.y) / max(_RadiusY * 0.8, 0.0001));
                flareBand = pow(flareBand, 2.5);

                float horizontalMask = saturate(1.0 - abs(p.x) / max(_RadiusX * 1.6, 0.0001));
                float horizontalFlare = flareBand * horizontalMask * _HorizontalFlare;

                // Pulse
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                float alpha = (glow + center + horizontalFlare) * _Intensity * pulse;

                fixed3 color = _Tint.rgb * alpha;

                return fixed4(color, alpha);
            }
            ENDCG
        }
    }
}