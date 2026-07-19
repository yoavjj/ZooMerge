Shader "Custom/SpriteLightSweep"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _SweepColor ("Sweep Color", Color) = (1,1,1,1)
        _Speed ("Speed", Float) = 1
        _Width ("Width", Float) = 0.1
        _Intensity ("Intensity", Float) = 2
        _Direction ("Direction", Float) = 1 // 1 = forward, -1 = reverse
        _Angle ("Sweep Angle (Degrees)", Float) = 45
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _Color;
            float4 _SweepColor;

            float _Speed;
            float _Width;
            float _Intensity;
            float _Direction;
            float _Angle;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Base sprite
                fixed4 col = tex2D(_MainTex, i.uv) * i.color * _Color;

                // Animate sweep position (0 → 1 loop)
                float t = frac(_Time.y * _Speed);

                // Convert angle to direction vector
                float angleRad = radians(_Angle);
                float2 dir = float2(cos(angleRad), sin(angleRad));

                // Project UV onto slanted direction
                float uvAxis = dot(i.uv, dir);

                // Normalize range (prevents diagonal stretching issues)
                uvAxis *= 0.7071; // ~1/sqrt(2)

                // Optional direction flip
                uvAxis = (_Direction > 0) ? uvAxis : (1.0 - uvAxis);

                // Distance from moving sweep line
                float dist = abs(uvAxis - t);

                // Glow band
                float glow = smoothstep(_Width, 0, dist);

                // Apply lighting
                col.rgb += glow * _SweepColor.rgb * _Intensity;

                return col;
            }
            ENDCG
        }
    }
}