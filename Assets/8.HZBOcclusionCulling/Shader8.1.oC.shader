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

            RWStructuredBuffer<uint> _VisibilityBuffer: register(u1);
            RWStructuredBuffer<uint> _ArgsBuffer: register(u2);
            StructuredBuffer<InstancePara> _InstanceBuffer;
            float3 _BoundsExtent;
            float3 _BoundsCenter;
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
                    float4 boundsClip = float4(0.5, -0.5, 0.5, -0.5) * float4(boundsMinClip.xy, boundsMaxClip.xy) + 0.5;
                    float2 boundsSizeClip = boundsClip.zw - boundsClip.xy;
                    float2 boundsSizeScreen = boundsSizeClip * _HiZBuffer_TexelSize.zw;
                    int boundsLevel = ceil(log2(max(boundsSizeScreen.x, boundsSizeScreen.y)));

                    float4 depthSample4;
                    depthSample4.x = tex2Dlod(_HiZBuffer, float4(boundsClip.xy, 0, boundsLevel)).r;
                    depthSample4.y = tex2Dlod(_HiZBuffer, float4(boundsClip.xw, 0, boundsLevel)).r;
                    depthSample4.z = tex2Dlod(_HiZBuffer, float4(boundsClip.zy, 0, boundsLevel)).r;
                    depthSample4.w = tex2Dlod(_HiZBuffer, float4(boundsClip.zw, 0, boundsLevel)).r;
                    float2 depthSample2 = min(depthSample4.xy, depthSample4.zw);
                    float depthSample = min(depthSample2.x, depthSample2.y);
                    
                    if (boundsMaxClip.z >= depthSample)
                    {
                        uint currentIndex;
                        InterlockedAdd(_ArgsBuffer[1], 1, currentIndex);
                        _VisibilityBuffer[currentIndex] = v.vertexID;
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
