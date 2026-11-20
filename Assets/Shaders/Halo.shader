// Brillos y flamas que siempre miran a la camara y titilan.
Shader "MemoriaViva/Halo"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Intensity ("Intensidad", Float) = 1
        _Flicker ("Titileo", Range(0,1)) = .2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "DisableBatching"="True" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "MemoriaSprites.cginc"
            float _Flicker;

            v2f vert(appdata v)
            {
                float phase = v.color.a * 40.0;
                float n = sin(_Time.y * 9.0 + phase) * .5 + sin(_Time.y * 23.0 + phase * 1.7) * .3 + sin(_Time.y * 3.1 + phase) * .2;
                float k = 1.0 + n * _Flicker;
                float2 corner = v.corner * float2(1.0 + n * _Flicker * .3, k);
                return Billboard(v, v.vertex.xyz, corner, float4(v.color.rgb * (1.0 + n * _Flicker * .6), 1));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 t = tex2D(_MainTex, i.uv);
                return t * t.a * i.color * _Color * _Intensity;
            }
            ENDCG
        }
    }
}
