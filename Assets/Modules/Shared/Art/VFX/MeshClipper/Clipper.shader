Shader "Unlit/Clipper"
{    
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry-1"
        }
        LOD 100

        Pass
        {
        ZWrite On
        ColorMask 0

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 color: COLOR;
                float4 worldPosition: TEXCOORD1;
            };

            float _WaterLevel;

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.normal;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                clip(-i.worldPosition.y - _WaterLevel);
                fixed4 col = float4(i.color+1,0);
                return col;
            }
            ENDCG
        }
    }
}
