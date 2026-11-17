Shader "Custom/UI/HologramGlitch"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Tint", Color) = (1,1,1,1)

        // ---------------------------------------------------------
        // LOOP / SYNC
        // ---------------------------------------------------------

        [Header(Loop Sync)]
        _Phase ("Manual Phase", Range(0,1)) = 0
        _UseManualPhase ("Use Manual Phase", Range(0,1)) = 0
        _LoopSpeed ("Auto Loop Speed", Float) = 0.5
        _PhaseOffset ("Phase Offset", Range(0,1)) = 0

        // ---------------------------------------------------------
        // HOLOGRAM
        // ---------------------------------------------------------

        [Header(Hologram)]
        _HologramColor ("Hologram Color", Color) =
            (0.0, 1.0, 1.0, 1.0)

        _HologramStrength (
            "Hologram Strength",
            Range(0,1)
        ) = 0.25

        _Brightness (
            "Brightness",
            Range(0,3)
        ) = 1

        // ---------------------------------------------------------
        // SCANLINES
        // ---------------------------------------------------------

        [Header(Scanlines)]
        _ScanlineAmount (
            "Scanline Amount",
            Range(0,1)
        ) = 0.2

        _ScanlineDensity (
            "Scanline Density",
            Float
        ) = 100

        _ScanlineSpeed (
            "Scanline Speed",
            Float
        ) = 2

        // ---------------------------------------------------------
        // FLICKER
        // ---------------------------------------------------------

        [Header(Flicker)]
        _FlickerStrength (
            "Flicker Strength",
            Range(0,1)
        ) = 0.08

        _FlickerSpeed (
            "Flicker Speed",
            Float
        ) = 15

        // ---------------------------------------------------------
        // GLITCH
        // ---------------------------------------------------------

        [Header(Glitch)]
        _GlitchStrength (
            "Horizontal Strength",
            Range(0,0.15)
        ) = 0.025

        _GlitchFrequency (
            "Glitch Frequency",
            Range(1,20)
        ) = 6

        _GlitchDuration (
            "Glitch Duration",
            Range(0.01,0.5)
        ) = 0.12

        _GlitchSlices (
            "Glitch Slices",
            Range(1,100)
        ) = 35

        // ---------------------------------------------------------
        // RGB SPLIT
        // ---------------------------------------------------------

        [Header(RGB Split)]
        _RGBSplit (
            "RGB Split",
            Range(0,0.05)
        ) = 0.008

        _RGBSplitStrength (
            "RGB Split Strength",
            Range(0,1)
        ) = 0.75

        // ---------------------------------------------------------
        // GHOST
        // ---------------------------------------------------------

        [Header(Ghost)]
        _GhostStrength (
            "Ghost Strength",
            Range(0,1)
        ) = 0.2

        _GhostOffset (
            "Ghost Offset",
            Range(0,0.1)
        ) = 0.015

        // ---------------------------------------------------------
        // UNITY UI
        // ---------------------------------------------------------

        _StencilComp (
            "Stencil Comparison",
            Float
        ) = 8

        _Stencil (
            "Stencil ID",
            Float
        ) = 0

        _StencilOp (
            "Stencil Operation",
            Float
        ) = 0

        _StencilWriteMask (
            "Stencil Write Mask",
            Float
        ) = 255

        _StencilReadMask (
            "Stencil Read Mask",
            Float
        ) = 255

        _ColorMask (
            "Color Mask",
            Float
        ) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip (
            "Use Alpha Clip",
            Float
        ) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;

            fixed4 _Color;
            fixed4 _HologramColor;

            float4 _ClipRect;

            float _Phase;
            float _UseManualPhase;
            float _LoopSpeed;
            float _PhaseOffset;

            float _HologramStrength;
            float _Brightness;

            float _ScanlineAmount;
            float _ScanlineDensity;
            float _ScanlineSpeed;

            float _FlickerStrength;
            float _FlickerSpeed;

            float _GlitchStrength;
            float _GlitchFrequency;
            float _GlitchDuration;
            float _GlitchSlices;

            float _RGBSplit;
            float _RGBSplitStrength;

            float _GhostStrength;
            float _GhostOffset;

            float Hash(float n)
            {
                return frac(
                    sin(n * 12.9898) *
                    43758.5453
                );
            }

            float Hash2(float2 p)
            {
                return frac(
                    sin(
                        dot(
                            p,
                            float2(
                                12.9898,
                                78.233
                            )
                        )
                    ) *
                    43758.5453
                );
            }

            v2f vert(appdata_t v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;

                o.vertex =
                    UnityObjectToClipPos(
                        v.vertex
                    );

                o.texcoord =
                    v.texcoord;

                o.color =
                    v.color *
                    _Color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // =================================================
                // LOOP PHASE
                // =================================================

                float autoPhase =
                    frac(
                        _Time.y *
                        _LoopSpeed +
                        _PhaseOffset
                    );

                float phase =
                    lerp(
                        autoPhase,
                        frac(
                            _Phase +
                            _PhaseOffset
                        ),
                        saturate(
                            _UseManualPhase
                        )
                    );

                // =================================================
                // GLITCH WINDOW
                // =================================================

                float glitchCycle =
                    frac(
                        phase *
                        _GlitchFrequency
                    );

                float glitchPulse =
                    1.0 -
                    smoothstep(
                        0.0,
                        _GlitchDuration,
                        glitchCycle
                    );

                // Give each glitch event a different pattern.
                float glitchEvent =
                    floor(
                        phase *
                        _GlitchFrequency
                    );

                // =================================================
                // HORIZONTAL SLICES
                // =================================================

                float slice =
                    floor(
                        i.texcoord.y *
                        _GlitchSlices
                    );

                float sliceRandom =
                    Hash(
                        slice +
                        glitchEvent * 17.0
                    );

                float sliceDirection =
                    sliceRandom * 2.0 - 1.0;

                // Only some rows actually glitch.
                float sliceMask =
                    step(
                        0.55,
                        sliceRandom
                    );

                float horizontalOffset =
                    sliceDirection *
                    _GlitchStrength *
                    sliceMask *
                    glitchPulse;

                float2 uv =
                    i.texcoord;

                uv.x +=
                    horizontalOffset;

                // =================================================
                // RGB SEPARATION
                // =================================================

                float rgbAmount =
                    _RGBSplit *
                    _RGBSplitStrength *
                    glitchPulse *
                    sliceMask;

                float2 redUV =
                    uv +
                    float2(
                        rgbAmount,
                        0
                    );

                float2 blueUV =
                    uv -
                    float2(
                        rgbAmount,
                        0
                    );

                fixed4 baseSample =
                    tex2D(
                        _MainTex,
                        uv
                    );

                fixed4 redSample =
                    tex2D(
                        _MainTex,
                        redUV
                    );

                fixed4 blueSample =
                    tex2D(
                        _MainTex,
                        blueUV
                    );

                fixed4 col =
                    baseSample;

                col.r =
                    lerp(
                        baseSample.r,
                        redSample.r,
                        glitchPulse
                    );

                col.b =
                    lerp(
                        baseSample.b,
                        blueSample.b,
                        glitchPulse
                    );

                // Keep original sprite alpha.
                col.a =
                    baseSample.a;

                // =================================================
                // GHOST IMAGE
                // =================================================

                float ghostDirection =
                    Hash(
                        glitchEvent +
                        91.7
                    ) * 2.0 - 1.0;

                float2 ghostUV =
                    uv;

                ghostUV.x +=
                    ghostDirection *
                    _GhostOffset *
                    glitchPulse;

                fixed4 ghost =
                    tex2D(
                        _MainTex,
                        ghostUV
                    );

                col.rgb +=
                    ghost.rgb *
                    ghost.a *
                    _GhostStrength *
                    glitchPulse;

                // =================================================
                // SCANLINES
                // =================================================

                float scanline =
                    sin(
                        (
                            i.texcoord.y *
                            _ScanlineDensity
                        ) +
                        (
                            phase *
                            _ScanlineSpeed *
                            6.2831853
                        )
                    );

                scanline =
                    scanline *
                    0.5 +
                    0.5;

                float scanlineBrightness =
                    lerp(
                        1.0,
                        scanline,
                        _ScanlineAmount
                    );

                col.rgb *=
                    scanlineBrightness;

                // =================================================
                // FLICKER
                // =================================================

                float flicker =
                    sin(
                        phase *
                        _FlickerSpeed *
                        6.2831853
                    );

                flicker =
                    flicker *
                    0.5 +
                    0.5;

                float flickerMultiplier =
                    lerp(
                        1.0 -
                        _FlickerStrength,
                        1.0,
                        flicker
                    );

                col.rgb *=
                    flickerMultiplier;

                // =================================================
                // HOLOGRAM TINT
                // =================================================

                col.rgb =
                    lerp(
                        col.rgb,
                        col.rgb *
                        _HologramColor.rgb,
                        _HologramStrength
                    );

                col.rgb *=
                    _Brightness;

                col *=
                    i.color;

                // =================================================
                // UNITY UI CLIPPING
                // =================================================

                #ifdef UNITY_UI_CLIP_RECT

                col.a *=
                    UnityGet2DClipping(
                        i.worldPosition.xy,
                        _ClipRect
                    );

                #endif

                #ifdef UNITY_UI_ALPHACLIP

                clip(
                    col.a - 0.001
                );

                #endif

                return col;
            }

            ENDCG
        }
    }
}