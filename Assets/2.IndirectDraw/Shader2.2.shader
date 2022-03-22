Shader "Unlit/Shader2.2"
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
            #pragma shader_feature INSTANCING_ON
            #define UNITY_INSTANCING_ENABLED
            //#define UNITY_DONT_INSTANCE_OBJECT_MATRICES

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct InstancePara
            {
                float4 position;
                float4 color;
            };
            StructuredBuffer<InstancePara> _InstanceBuffer;

            v2f vert(appdata v)
            {
                v2f o;
                float4 offset = _InstanceBuffer[v.instanceID].position;
                o.vertex = UnityObjectToClipPos(v.vertex + offset);
                o.instanceID = v.instanceID;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = _InstanceBuffer[i.instanceID].color;
                return col;
            }
            ENDCG
        }
    }
}
