Shader "Unlit/GridCell"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            int ArrayElementWidth;
            int ArrayElemetCount;

            uniform StructuredBuffer<uint> GridCellDrawBuffer;

            v2f vert (appdata v, uint svInstanceID : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                
                uint instanceID = GetIndirectInstanceID(svInstanceID);
    
                uint positionData = GridCellDrawBuffer[instanceID];
                
                uint x = positionData % (uint)ArrayElementWidth;
                uint y = positionData / (uint)ArrayElementWidth;
                
                v.vertex.x += x;
                v.vertex.y += y;

                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
}
