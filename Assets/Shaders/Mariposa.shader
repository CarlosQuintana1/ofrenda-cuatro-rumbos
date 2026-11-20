// Mariposas monarca que vuelan en orbita alrededor de la ofrenda.
Shader "MemoriaViva/Mariposa"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Intensity ("Intensidad", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "DisableBatching"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "MemoriaSprites.cginc"

            v2f vert(appdata v)
            {
                // data.x = radio, data.y = altura, data.z = velocidad angular, data.w = amplitud vertical
                float phase = v.color.a * 6.2831;
                float a = phase + _Time.y * v.data.z;
                float r = v.data.x + sin(_Time.y * .7 + phase * 3.0) * min(.8, v.data.x * .5);
                float3 p = v.vertex.xyz + float3(sin(a) * r, v.data.y + sin(_Time.y * 1.6 + phase * 2.0) * v.data.w, cos(a) * r);
                float flap = abs(sin(_Time.y * 11.0 + phase * 5.0));
                float2 corner = v.corner * float2(.25 + flap * .75, 1);
                return Billboard(v, p, corner, float4(1, 1, 1, 1));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                c.rgb *= _Intensity;
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
}
