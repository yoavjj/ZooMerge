Shader "Custom/StencilWriter1"
{
    SubShader
    {
        Tags { "Queue"="Geometry+11" }
        ColorMask 0
        ZWrite Off
        Stencil
        {
            Ref 1
            Comp Always
            Pass Replace
            WriteMask 1
        }
        Pass {}
    }
}
