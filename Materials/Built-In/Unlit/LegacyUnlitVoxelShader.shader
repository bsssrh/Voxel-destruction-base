Shader "Custom/LegacyUnlitVoxelShader"
{
SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        CGPROGRAM
        #pragma surface surf Unlit vertex:vert
        #pragma target 3.0

        struct Input
        {
            float4 vertColor;
        };

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.vertColor = v.color;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            o.Albedo = GammaToLinearSpace(IN.vertColor.rgb);
            o.Alpha = IN.vertColor.a;
        }

        float4 LightingUnlit(SurfaceOutput s, float3 lightDir, float atten)
        {
            return float4(s.Albedo, s.Alpha);
        }
        ENDCG
    }
    FallBack "Unlit/Color"
}
