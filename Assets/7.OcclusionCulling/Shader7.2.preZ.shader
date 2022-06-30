Shader "Unlit/Shader7.2.preZ"
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
            RWStructuredBuffer<uint> _VisibilityBuffer: register(u2);
            RWStructuredBuffer<uint> _ArgsBuffer: register(u3);
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
            fixed4 frag(v2f i) : SV_Target
            {
                uint frameIndex;
                InterlockedMax(_VisibilityFrameIndexBuffer[i.instanceID], _CurrentFrameIndex, frameIndex);
                if (frameIndex < _CurrentFrameIndex)
                {
                    uint currentIndex;
                    InterlockedAdd(_ArgsBuffer[1], 1, currentIndex);
                    _VisibilityBuffer[currentIndex] = i.instanceID;
                }
                return 1;
            }
            ENDCG
        }
    }
}
