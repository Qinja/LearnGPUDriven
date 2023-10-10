Shader "LearnGPUDriven/Shader8.1.hiZ"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Texture2D _DepthTex;
            SamplerState sampler_DepthTex;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float frag(v2f i) : SV_Target
            {
                float4 depthSample4 = _DepthTex.Gather(sampler_DepthTex, i.uv);
                float2 depthSample2 = min(depthSample4.xy, depthSample4.zw);
                float depthSample = min(depthSample2.x, depthSample2.y);
                return depthSample;
            }
            ENDCG
        }
    }
}
