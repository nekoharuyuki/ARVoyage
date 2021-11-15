Shader "ARDK/Meshing/InvisibleNoDepth" {
    SubShader {
        Tags {"Queue" = "Geometry-10" }
 
        ColorMask 0
        ZWrite Off

        Pass {}
    }
}