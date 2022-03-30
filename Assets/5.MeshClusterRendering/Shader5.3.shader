Shader "Unlit/Shader5.3"
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

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = fixed4(i.color,1);
                return col;
            }
            ENDCG
        }
    }
}
