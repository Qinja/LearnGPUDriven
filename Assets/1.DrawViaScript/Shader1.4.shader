Shader "LearnGPUDriven/Shader1.4"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature INSTANCING_ON
            #define UNITY_INSTANCING_ENABLED

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

            float4 _Color[125];

            v2f vert(appdata v)
            {
                v2f o;
                o.instanceID = v.instanceID;
                unity_ObjectToWorld = unity_Builtins0Array[v.instanceID].unity_ObjectToWorldArray;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = _Color[i.instanceID];
                return col;
            }
            ENDCG
        }
    }
}
