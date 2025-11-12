Shader "Custom/BeamRevealShader"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // ---- Beam parameters ----
        _BeamPos ("Beam World Position", Vector) = (0,0,0,0)
        _BeamDir ("Beam Direction", Vector) = (0,-1,0,0)
        _BeamAngle ("Cone Angle (radians)", Range(0.01,1.0)) = 0.35
        _BeamHeight ("Beam Height", Float) = 6.0
        _BeamSoftness ("Edge Softness", Range(0.001,1)) = 0.3
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            float4 _BeamPos;
            float4 _BeamDir;
            float _BeamAngle;
            float _BeamHeight;
            float _BeamSoftness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                // --- Cone fade computation ---
                float3 dirToPoint = i.worldPos - _BeamPos.xyz;

                // Project onto beam axis
                float3 beamDir = normalize(_BeamDir.xyz);
                float heightAlongBeam = dot(dirToPoint, beamDir);

                // Reject if outside beam height
                if (heightAlongBeam < 0 || heightAlongBeam > _BeamHeight)
                {
                    col.a = 0;
                    return col;
                }

                // Horizontal distance from axis
                float3 radial = dirToPoint - beamDir * heightAlongBeam;
                float distFromAxis = length(radial);

                // Radius of cone at this height
                float radiusAtHeight = tan(_BeamAngle) * heightAlongBeam;

                // Soft edge fade
                float edge = smoothstep(radiusAtHeight, radiusAtHeight * (1.0 - _BeamSoftness), distFromAxis);

                // Invert so 1 = inside beam
                float inside = 1.0 - edge;

                col.a *= inside;
                return col;
            }
            ENDCG
        }
    }
}
