Shader "Unlit/Shader3.4"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature INSTANCING_ON
            #define UNITY_INSTANCING_ENABLED
            #define UNITY_DONT_INSTANCE_OBJECT_MATRICES
            #define MESH_VERTEX_COUNT 2496

            #include "UnityCG.cginc"

            struct appdata
            {
                uint vertexID : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 color : COLOR;
            };

            struct InstancePara
            {
                uint indexOffset;
                float4x4 model;
                float4 color;
            };

            StructuredBuffer<float3> _VertexBuffer;
            StructuredBuffer<uint> _IndexBuffer;
            StructuredBuffer<InstancePara> _InstanceBuffer;

            v2f vert(appdata v)
            {
                uint vertexID = v.vertexID % MESH_VERTEX_COUNT;
                InstancePara para = _InstanceBuffer[v.instanceID];
                uint index = _IndexBuffer[para.indexOffset + vertexID];
                float3 vertex = _VertexBuffer[index];

                v2f o;                
                unity_ObjectToWorld = para.model;
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
