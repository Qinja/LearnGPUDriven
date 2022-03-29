Shader "Unlit/Shader5.1"
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
                float4x4 model;
                float4 color;
            };

            int _ClusterCount;
            StructuredBuffer<float3> _VBO;
            StructuredBuffer<InstancePara> _InstanceBuffer;

            v2f vert(appdata v)
            {
                int VSIZE = 64;
                int instance_id = v.instanceID / _ClusterCount;
                int cluster_id = v.instanceID % _ClusterCount;
                int vertex_id = v.vid;
                InstancePara para = _InstanceBuffer[instance_id];
                int index = cluster_id * VSIZE + vertex_id;
                float3 vertex = _VBO[index];

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
