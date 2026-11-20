// Resplandor de flamas y faroles: umbral, reduccion en cadena y suma de vuelta.
Shader "Hidden/MemoriaViva/Bloom"
{
    Properties { _MainTex ("", 2D) = "white" {} }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex, _BloomTex;
    float4 _MainTex_TexelSize;
    float _Threshold, _Intensity;
    half3 Box(float2 uv, float d)
    {
        float4 o = _MainTex_TexelSize.xyxy * float2(-d, d).xxyy;
        return (tex2D(_MainTex, uv + o.xy).rgb + tex2D(_MainTex, uv + o.zy).rgb + tex2D(_MainTex, uv + o.xw).rgb + tex2D(_MainTex, uv + o.zw).rgb) * .25;
    }
    half4 Prefilter(v2f_img i) : SV_Target
    {
        half3 c = Box(i.uv, 1);
        half b = max(c.r, max(c.g, c.b));
        half k = max(0, b - _Threshold) / max(b, 1e-4);
        return half4(c * k, 1);
    }
    half4 Down(v2f_img i) : SV_Target { return half4(Box(i.uv, 1), 1); }
    half4 Up(v2f_img i) : SV_Target { return half4(Box(i.uv, .5), 1); }
    half4 Combine(v2f_img i) : SV_Target
    {
        half4 c = tex2D(_MainTex, i.uv);
        c.rgb += tex2D(_BloomTex, i.uv).rgb * _Intensity;
        return c;
    }
    ENDCG
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Prefilter
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Down
            ENDCG }
        Pass { Blend One One
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Up
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Combine
            ENDCG }
    }
}
