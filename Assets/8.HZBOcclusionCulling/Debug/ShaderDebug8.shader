Shader "Custom/ShaderDebug8"
{
    Properties
    {
        _Color("Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" }
        Pass
        {
            ZTest Less
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                uint instanceID : SV_InstanceID;
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

            StructuredBuffer<InstancePara> _InstanceBuffer;
            fixed4 _Color;

            v2f vert(appdata v)
            {
                InstancePara para = _InstanceBuffer[v.instanceID];
                unity_ObjectToWorld = para.model;

                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = _Color;
                return col;
            }
            ENDCG
        }
    }
}
