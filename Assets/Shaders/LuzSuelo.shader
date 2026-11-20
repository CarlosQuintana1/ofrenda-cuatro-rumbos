// Charco de luz calida sobre el piso, aditivo y sin billboard.
Shader "MemoriaViva/LuzSuelo"
{
    Properties { _MainTex ("Textura", 2D) = "white" {} _Color ("Color", Color) = (1,.6,.3,1) _Intensity ("Intensidad", Float) = 1 }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One ZWrite Off Cull Off Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _Color; float _Intensity;
            struct a { float4 v : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct o { float4 p : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            o vert(a i) { o r; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(r); r.p = UnityObjectToClipPos(i.v); r.uv = i.uv; return r; }
            fixed4 frag(o i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 t = tex2D(_MainTex, i.uv);
                float flick = .92 + .08 * sin(_Time.y * 7.3) * sin(_Time.y * 3.1);
                return t.a * _Color * _Intensity * flick;
            }
            ENDCG
        }
    }
}
