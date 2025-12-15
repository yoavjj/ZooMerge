Shader "Custom/StencilWriter2"
{
    SubShader
    {
        Tags { "Queue"="Geometry+10" }
        ColorMask 0
        ZWrite Off
        Stencil
        {
            Ref 2
            Comp Always
            Pass Replace
            WriteMask 2
        }
        Pass {}
    }
}
