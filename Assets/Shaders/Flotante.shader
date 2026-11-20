// Humo de copal que sube y petalos que caen, animados en GPU.
Shader "MemoriaViva/Flotante"
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
                // data.x = velocidad vertical (+ sube, - cae), data.y = recorrido, data.z = vaiven, data.w = crecimiento
                float phase = v.color.a * 17.0;
                float f = frac(_Time.y * abs(v.data.x) / max(v.data.y, .01) + v.color.a * 3.7);
                float3 p = v.vertex.xyz;
                p.y += (v.data.x >= 0 ? f : -f) * v.data.y;
                p.x += sin(_Time.y * .9 + phase + f * 4.0) * v.data.z * f;
                p.z += cos(_Time.y * .7 + phase + f * 3.0) * v.data.z * f;
                float fade = smoothstep(0, .15, f) * (1 - smoothstep(.6, 1, f));
                float grow = 1 + f * v.data.w;
                float s = sin(_Time.y * 2.0 + phase);
                float2 corner = v.corner * grow;
                corner = float2(corner.x * cos(s) - corner.y * sin(s), corner.x * sin(s) + corner.y * cos(s));
                return Billboard(v, p, corner, float4(v.color.rgb, fade));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 c = tex2D(_MainTex, i.uv) * _Color * i.color;
                c.rgb *= _Intensity;
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
}
