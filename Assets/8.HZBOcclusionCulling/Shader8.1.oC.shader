Shader "Unlit/Shader8.1.oC"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Never
            ColorMask A
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
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
            sampler2D _HiZBuffer;
            float4 _HiZBuffer_TexelSize;
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
                InstancePara para = _InstanceBuffer[v.vertexID];
                float3 boundsMinClip = float3(10, 10, 10);
                float3 boundsMaxClip = float3(-10, -10, -10);
                [unroll(8)]
                for (int i = 0; i < 8; i++)
                {
                    float3 cornerLocal = _BoundsCenter + _BoundsExtent * cubeCorner[i];
                    float4 cornerWorld = mul(para.model, float4(cornerLocal, 1));
                    float4 cornerClip = mul(UNITY_MATRIX_VP, cornerWorld);
                    cornerClip.xyz = cornerClip.xyz / cornerClip.w;
                    boundsMinClip = min(boundsMinClip, cornerClip.xyz);
                    boundsMaxClip = max(boundsMaxClip, cornerClip.xyz);
                }
                if (boundsMinClip.x <= 1 && boundsMaxClip.x >= -1 && boundsMinClip.y <= 1 && boundsMaxClip.y >= -1 && boundsMinClip.z <= 1 && boundsMaxClip.z >= 0)
                {
                    float2 boundsSizeClip = 0.5 * (boundsMaxClip.xy - boundsMinClip.xy);
                    float2 boundsSizeScreen = min(boundsSizeClip.xy, 1) * _HiZBuffer_TexelSize.zw;
                    int2 boundsLevel = ceil(log2(boundsSizeScreen));
                    int maxLevel =  max(boundsLevel.x, boundsLevel.y);
                    float2 boundsCenterClip = float2(0.25, -0.25) * (boundsMinClip.xy + boundsMaxClip.xy) + 0.5;
                    float depth = tex2Dlod(_HiZBuffer, float4(boundsCenterClip.xy, 0, maxLevel)).r;

                    if (boundsMaxClip.z >= depth)
                    {
                        uint frameIndex;
                        InterlockedMax(_VisibilityFrameIndexBuffer[v.vertexID], _CurrentFrameIndex, frameIndex);
                        if (frameIndex < _CurrentFrameIndex)
                        {
                            uint currentIndex;
                            InterlockedAdd(_ArgsBuffer[1], 1, currentIndex);
                            _VisibilityBuffer[currentIndex] = v.vertexID;
                        }
                    }
                }

                v2f o;
                o.vertex = float4(10, 10, 10, 1);
                return o;
            }

            fixed frag() : SV_Target
            {
                return 0;
            }
            ENDCG
        }
    }
}
