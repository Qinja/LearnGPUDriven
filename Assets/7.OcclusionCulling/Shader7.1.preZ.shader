Shader "LearnGPUDriven/Shader7.1.preZ"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            ColorMask 0
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                uint instanceID : SV_InstanceID;
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct InstancePara
            {
                float4x4 model;
                float4 color;
            };

            RWStructuredBuffer<uint> _VisibilityFrameIndexBuffer : register(u1);
            StructuredBuffer<InstancePara> _InstanceBuffer;
            float3 _BoundsExtent;
            float3 _BoundsCenter;
            uint _CurrentFrameIndex;

            v2f vert(appdata v)
            {
                static float3 cubeCorner[8] =
                {
                    float3(+1, +1, -1),
                    float3(-1, +1, -1),
                    float3(-1, -1, -1),
                    float3(+1, -1, -1),
                    float3(+1, +1, +1),
                    float3(-1, +1, +1),
                    float3(-1, -1, +1),
                    float3(+1, -1, +1),
                };
                InstancePara para = _InstanceBuffer[v.instanceID];
                unity_ObjectToWorld = para.model;
                v2f o;
                
                float3 vertex = cubeCorner[v.vertexID] * _BoundsExtent + _BoundsCenter;
                o.vertex = UnityObjectToClipPos(vertex);
                o.instanceID = v.instanceID;
                return o;
            }

            [earlydepthstencil]
            void frag(v2f i)
            {
                _VisibilityFrameIndexBuffer[i.instanceID] = _CurrentFrameIndex;
            }
            ENDCG
        }
    }
}
