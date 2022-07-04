Shader "LearnGPUDriven/Shader5.3"
{
    Properties
    {
        _BackFaceColor("Back Face Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define CLUSTER_VERTEX_COUNT 64

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
                float4x4 model;
                float4 color;
                uint vertexOffset;
                uint clusterIndex;
            };

            StructuredBuffer<float3> _VertexBuffer;
            StructuredBuffer<uint> _VisibilityBuffer;
            StructuredBuffer<InstancePara> _InstanceBuffer;
            StructuredBuffer<uint> _ClusterBuffer;
            float4 _BackFaceColor;

            v2f vert(appdata v)
            {
                uint visibleID = _VisibilityBuffer[v.instanceID];
                uint instanceID = _ClusterBuffer[visibleID];
                InstancePara para = _InstanceBuffer[instanceID];
                uint index = para.vertexOffset + CLUSTER_VERTEX_COUNT * (visibleID - para.clusterIndex) + v.vertexID;
                float3 vertex = _VertexBuffer[index];

                v2f o;
                unity_ObjectToWorld = para.model;
                o.vertex = UnityObjectToClipPos(vertex);
                o.color = para.color;
                return o;
            }

            fixed4 frag(v2f i, bool facing : SV_IsFrontFace) : SV_Target
            {
                fixed4 col = facing ? fixed4(i.color,1) : _BackFaceColor;
                return col;
            }
            ENDCG
        }
    }
}
