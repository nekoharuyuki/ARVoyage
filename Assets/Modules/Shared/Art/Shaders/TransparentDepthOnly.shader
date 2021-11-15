Shader "Nexus/TransparentDepthOnly" {
    Properties {
    }
    
    SubShader {
        Tags {"RenderType"="Opaque" "Queue"="Geometry"}
        Pass {
            ColorMask 0
        }
    }
}