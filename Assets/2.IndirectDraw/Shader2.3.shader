Shader "Unlit/Shader2.3"
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
            #define UNITY_DONT_INSTANCE_OBJECT_MATRICES

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

            float4 _ParentPosition;
            int _InstanceCount;

            v2f vert(appdata v)
            {
                v2f o;
                int row = ceil(sqrt(_InstanceCount));
                float3 offset = float3(0, 2.0f * floor(v.instanceID / row), 2.0f * (v.instanceID % row));
                float3 worldPos = v.vertex.xyz + _ParentPosition + offset;
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
                o.instanceID = v.instanceID;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float r = sin(1234.0f * i.instanceID);
                float g = sin(5678.0f * i.instanceID);
                float b = sin(9009.0f * i.instanceID);
                fixed4 col = float4(0.5f * float3(r, g, b) + 0.5f, 1.0f);
                return col;
            }
            ENDCG
        }
    }
}
