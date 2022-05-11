Shader "Unlit/Shader8.1.hiZ"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float frag(v2f i) : SV_Target
            {
                float4 uv = float4(-0.5, 0.5, -0.5, 0.5) * _MainTex_TexelSize.xxyy + i.uv.xxyy;
                float4 depthSample4;
                depthSample4.x = tex2Dlod(_MainTex, float4(uv.xz, 0, 0)).r;
                depthSample4.y = tex2Dlod(_MainTex, float4(uv.xw, 0, 0)).r;
                depthSample4.z = tex2Dlod(_MainTex, float4(uv.yz, 0, 0)).r;
                depthSample4.w = tex2Dlod(_MainTex, float4(uv.yw, 0, 0)).r;
                float2 depthSample2 = min(depthSample4.xy, depthSample4.zw);
                float depthSample = min(depthSample2.x, depthSample2.y);
                return depthSample;
            }
            ENDCG
        }
    }
}
