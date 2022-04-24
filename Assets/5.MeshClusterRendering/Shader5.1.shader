Shader "Unlit/Shader5.1"
{
    SubShader
    {
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
            };

            uint _ClusterCount;
            StructuredBuffer<float3> _VertexBuffer;
            StructuredBuffer<InstancePara> _InstanceBuffer;

            v2f vert(appdata v)
            {
                uint instanceID = v.instanceID / _ClusterCount;
                uint clusterID = v.instanceID % _ClusterCount;
                InstancePara para = _InstanceBuffer[instanceID];
                uint index = clusterID * CLUSTER_VERTEX_COUNT + v.vertexID;
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
