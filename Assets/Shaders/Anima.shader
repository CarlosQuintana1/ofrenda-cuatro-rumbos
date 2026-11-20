// Luces que viajan por el camino de petalos hacia el centro de la ofrenda.
Shader "MemoriaViva/Anima"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Intensity ("Intensidad", Float) = 1
        _Speed ("Velocidad", Float) = .07
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
            float _Speed;

            v2f vert(appdata v)
            {
                // data.xyz = destino en el objeto, data.w = altura del arco
                float f = frac(_Time.y * _Speed * (.7 + v.color.a * .6) + v.color.a * 7.31);
                float3 p = lerp(v.vertex.xyz, v.data.xyz, f);
                float phase = v.color.a * 31.0;
                p.y += sin(f * 3.14159) * v.data.w + sin(_Time.y * 2.0 + phase) * .12;
                p.x += sin(_Time.y * 1.3 + phase) * .18;
                p.z += cos(_Time.y * 1.1 + phase) * .18;
                float fade = smoothstep(0, .12, f) * (1 - smoothstep(.82, 1, f));
                float pulse = .75 + .25 * sin(_Time.y * 5.0 + phase);
                return Billboard(v, p, v.corner * (.6 + fade * .4), float4(v.color.rgb * fade * pulse, 1));
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
