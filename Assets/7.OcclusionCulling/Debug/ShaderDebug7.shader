Shader "Unlit/ShaderDebug7"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Never
            ColorMask 0
            Cull Off
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

            RWStructuredBuffer<uint> _DebugBuffer : register(u1);
            uint _Hash;

            v2f vert(appdata v)
            {
                static float4 fullScreenTriangle[3] =
                {
                    float4(-1, +3, +1, +1),
                    float4(-1, -1, +1, +1),
                    float4(+3, -1, +1, +1),
                };
                v2f o;
                o.vertex = fullScreenTriangle[min(v.vertexID, 2u)];
                return o;
            }

            [earlydepthstencil]
            fixed4 frag(v2f i) : SV_Target
            {
                _DebugBuffer[0] = _Hash;
                return 1;
            }
            ENDCG
        }
    }
}
