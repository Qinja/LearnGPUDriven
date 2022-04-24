Shader "Custom/ShaderDebug5"
{
    Properties
    {
        _Color ("Color", Color) = (0,1,1,1)
        _BackFaceColor ("BackFaceColor", Color) = (1,1,0,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        struct Input
        {
            bool facing : SV_IsFrontFace;
        };
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _BackFaceColor;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = IN.facing ? _Color : _BackFaceColor;
            o.Emission = 0.1 * c.rgb;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
