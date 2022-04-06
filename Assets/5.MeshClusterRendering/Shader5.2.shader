Shader "Unlit/Shader5.2"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

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
            };

            int _ClusterCount;
            StructuredBuffer<float3> _VertexBuffer;
            StructuredBuffer<uint> _VisibilityBuffer;
            StructuredBuffer<InstancePara> _InstanceBuffer;

            v2f vert(appdata v)
            {
                uint visibleIndex = _VisibilityBuffer[v.instanceID];
                uint instanceID = visibleIndex / _ClusterCount;
                uint clusterID = visibleIndex % _ClusterCount;
                InstancePara para = _InstanceBuffer[instanceID];
                uint index = clusterID * CLUSTER_VERTEX_COUNT + v.vertexID;
                float3 vertex = _VertexBuffer[index];

                v2f o;
                unity_ObjectToWorld = para.model;
                o.vertex = UnityObjectToClipPos(vertex);
                o.color = para.color;
                return o;
            }

            fixed4 frag(v2f i, bool facing : SV_IsFrontFace) : SV_Target
            {
                fixed4 col = facing ? fixed4(i.color,1) : fixed4(1,1,0,1);
                return col;
            }
            ENDCG
        }
    }
}
