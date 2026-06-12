Shader "Custom/UI/HeartLightSweep"
{
    Properties
    {
        [PerRendererData]_MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Sweep01 ("Sweep (0..1)", Range(0,1)) = 0
        _SweepWidth ("Sweep Width", Range(0.001,0.5)) = 0.12
        _Angle ("Sweep Angle (deg)", Range(0,360)) = 45

        _SweepColor ("Sweep Color", Color) = (1,1,1,1)
        _SweepIntensity ("Sweep Intensity", Range(0,10)) = 2
        _GlowIntensity ("Glow Intensity", Range(0,10)) = 1

        // NEW: extra edge highlight only where the sweep is
        _EdgeGlowIntensity ("Sweep Edge Glow Intensity", Range(0,10)) = 2
        _EdgeThickness ("Edge Thickness (px)", Range(0.5,6)) = 1.5
        _OuterEdgeAmount ("Outer Edge Amount", Range(0,1)) = 0.25
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

        Blend One OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;

            float _Sweep01;
            float _SweepWidth;
            float _Angle;

            fixed4 _SweepColor;
            float _SweepIntensity;
            float _GlowIntensity;

            float _EdgeGlowIntensity;
            float _EdgeThickness;
            float _OuterEdgeAmount;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            float2 RotateUV(float2 uv, float angleRad)
            {
                float2 p = uv - 0.5;
                float s = sin(angleRad);
                float c = cos(angleRad);
                float2 r = float2(p.x * c - p.y * s, p.x * s + p.y * c);
                return r + 0.5;
            }

            float SampleAlpha(float2 uv)
            {
                return tex2D(_MainTex, uv).a;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, IN.uv) * IN.color;
                float baseA = tex.a;

                // -------------------------
                // Sweep band
                // -------------------------
                float ang = radians(_Angle);
                float2 ruv = RotateUV(IN.uv, ang);
                float axis = ruv.x;

                // Make 0 and 1 fully outside the image
                float sweep = lerp(-_SweepWidth, 1.0 + _SweepWidth, _Sweep01);

                float d = abs(axis - sweep);
                float band = 1.0 - smoothstep(0.0, _SweepWidth, d);

                // only inside sprite for main sweep
                float sweepMask = band * baseA;

                // -------------------------
                // Edge detection from alpha
                // -------------------------
                float2 t = _MainTex_TexelSize.xy * _EdgeThickness;

                float aL  = SampleAlpha(IN.uv + float2(-t.x,  0));
                float aR  = SampleAlpha(IN.uv + float2( t.x,  0));
                float aU  = SampleAlpha(IN.uv + float2( 0,    t.y));
                float aD  = SampleAlpha(IN.uv + float2( 0,   -t.y));

                float aUL = SampleAlpha(IN.uv + float2(-t.x,  t.y));
                float aUR = SampleAlpha(IN.uv + float2( t.x,  t.y));
                float aDL = SampleAlpha(IN.uv + float2(-t.x, -t.y));
                float aDR = SampleAlpha(IN.uv + float2( t.x, -t.y));

                float maxA = max(max(max(aL, aR), max(aU, aD)), max(max(aUL, aUR), max(aDL, aDR)));
                float minA = min(min(min(aL, aR), min(aU, aD)), min(min(aUL, aUR), min(aDL, aDR)));

                // edge inside the heart
                float innerEdge = saturate(baseA - minA) * baseA;

                // tiny edge just outside the heart
                float outerEdge = saturate(maxA - baseA);

                // combined edge mask
                float edgeMask = saturate(innerEdge + outerEdge * _OuterEdgeAmount);

                // IMPORTANT:
                // only boost edge where the sweep currently is
                float sweepEdgeMask = edgeMask * band;

                // -------------------------
                // Final color
                // -------------------------

                // Premultiply base color
                tex.rgb *= baseA;

                // Main sweep contribution
                float3 sweepAdd = sweepMask * (_SweepIntensity + _GlowIntensity) * _SweepColor.rgb;

                // Extra edge boost only where sweep touches the silhouette
                float3 sweepEdgeAdd = sweepEdgeMask * _EdgeGlowIntensity * _SweepColor.rgb;

                float3 finalRGB = tex.rgb + sweepAdd + sweepEdgeAdd;

                // allow a tiny alpha outside only if outer edge is used
                float extraA = sweepEdgeMask * _OuterEdgeAmount * 0.5;
                float finalA = max(baseA, extraA);

                return float4(finalRGB, finalA);
            }
            ENDCG
        }
    }
}