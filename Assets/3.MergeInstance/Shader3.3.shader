Shader "Unlit/Shader3.3"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature INSTANCING_ON
            #define UNITY_INSTANCING_ENABLED

            #include "UnityCG.cginc"

            struct appdata
            {
                uint vid : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 color : COLOR;
            };

            struct InstancePara
            {
                uint index_offset;
                float4 color;
            };

            StructuredBuffer<float3> _VBO;
            StructuredBuffer<uint> _IBO;
            StructuredBuffer<InstancePara> _InstanceBuffer;

            v2f vert(appdata v)
            {
                int VSIZE = 2496;
                int cid = v.instanceID;
                int vid = v.vid % VSIZE;
                InstancePara para = _InstanceBuffer[cid];
                int index_offset = para.index_offset;
                int index = _IBO[index_offset + vid];
                float3 vertex = _VBO[index];

                v2f o;                
                unity_ObjectToWorld = unity_Builtins0Array[cid].unity_ObjectToWorldArray;
                o.vertex = UnityObjectToClipPos(vertex);
                o.color = para.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = fixed4(i.color,1);
                return col;
            }
            ENDCG
        }
    }
}
