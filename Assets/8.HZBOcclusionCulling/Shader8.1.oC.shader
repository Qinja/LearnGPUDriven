Shader "LearnGPUDriven/Shader8.1.oC"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Never
            ColorMask 0
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct InstancePara
            {
                float4x4 model;
                float4 color;
            };
            RWStructuredBuffer<uint> _VisibilityBuffer: register(u1);
            RWStructuredBuffer<uint> _ArgsBuffer: register(u2);
            StructuredBuffer<InstancePara> _InstanceBuffer;
            float3 _BoundsExtent;
            float3 _BoundsCenter;
            sampler2D _HiZBuffer;
            float4 _HiZBuffer_TexelSize;

            void vert(uint vertexID : SV_VertexID)
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
                InstancePara para = _InstanceBuffer[vertexID];
                float4x4 localToClip = mul(UNITY_MATRIX_VP, para.model);
                float3 boundsMinClip = float3(10, 10, 10);
                float3 boundsMaxClip = float3(-10, -10, -10);
                [unroll(8)]
                for (int i = 0; i < 8; i++)
                {
                    float3 cornerLocal = _BoundsCenter + _BoundsExtent * cubeCorner[i];
                    float4 cornerClip = mul(localToClip, float4(cornerLocal, 1));
                    if (cornerClip.w > 0.0001)
                    {
                        cornerClip.xyz = cornerClip.xyz / cornerClip.w;
                        boundsMinClip = min(boundsMinClip, cornerClip.xyz);
                        boundsMaxClip = max(boundsMaxClip, cornerClip.xyz);
                    }
                }
                boundsMinClip = clamp(boundsMinClip, float3(-1, -1, 0), 1);
                boundsMaxClip = clamp(boundsMaxClip, float3(-1, -1, 0), 1);
                if (all(boundsMaxClip > boundsMinClip))
                {
                    float4 boundsTexCoord = float4(0.5, -0.5, 0.5, -0.5) * float4(boundsMinClip.xy, boundsMaxClip.xy) + 0.5;
                    float2 boundsSizeTexCoord = boundsTexCoord.zy - boundsTexCoord.xw;
                    float2 boundsSizeScreen = boundsSizeTexCoord * _HiZBuffer_TexelSize.zw;
                    int boundsLevel = ceil(log2(max(boundsSizeScreen.x, boundsSizeScreen.y)));

                    float4 depthSample4;
                    depthSample4.x = tex2Dlod(_HiZBuffer, float4(boundsTexCoord.xy, 0, boundsLevel)).r;
                    depthSample4.y = tex2Dlod(_HiZBuffer, float4(boundsTexCoord.xw, 0, boundsLevel)).r;
                    depthSample4.z = tex2Dlod(_HiZBuffer, float4(boundsTexCoord.zy, 0, boundsLevel)).r;
                    depthSample4.w = tex2Dlod(_HiZBuffer, float4(boundsTexCoord.zw, 0, boundsLevel)).r;
                    float2 depthSample2 = min(depthSample4.xy, depthSample4.zw);
                    float depthSample = min(depthSample2.x, depthSample2.y);
                    if (boundsMaxClip.z >= depthSample)
                    {
                        uint currentIndex;
                        InterlockedAdd(_ArgsBuffer[1], 1, currentIndex);
                        _VisibilityBuffer[currentIndex] = vertexID;
                    }
                }
            }

            void frag() { }
            ENDCG
        }
    }
}
