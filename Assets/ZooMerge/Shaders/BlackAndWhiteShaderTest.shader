Shader "Unlit/BlackAndWhiteShaderTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Brightness ("Brightnesssss", Range(0, 2)) = 1.0 // Brightness control property
        _Blend ("Color Blend", Range(0, 1)) = 0    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" } // Example: Queue set to Transparent+1 (3001)
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Brightness; // Brightness variable
            float _Blend;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);

                // Grayscale
                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float3 grayColor = float3(gray, gray, gray);

                // Blend between grayscale and original color
                float3 finalColor = lerp(grayColor, col.rgb, _Blend);

                // Apply brightness AFTER blending (optional but usually nicer)
                finalColor *= _Brightness;

                return float4(finalColor, col.a);
            }
            ENDCG
        }
    }
}