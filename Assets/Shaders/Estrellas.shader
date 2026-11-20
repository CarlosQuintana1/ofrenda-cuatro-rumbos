// Estrellas y luna: sin niebla, solo aparecen de noche segun _Noche (lo pone CicloDeLuz).
Shader "MemoriaViva/Estrellas"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Titileo ("Titileo", Range(0,1)) = .35
    }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Color;
            float _Titileo;
            float _Noche;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                float fase = v.color.a * 50.0;
                float t = 1.0 - _Titileo * (.5 + .5 * sin(_Time.y * (1.3 + v.color.a * 2.0) + fase));
                o.color = float4(v.color.rgb * t * saturate(_Noche), 1);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                return fixed4(c.rgb * c.a * i.color.rgb * _Color.rgb, 1);
            }
            ENDCG
        }
    }
}
