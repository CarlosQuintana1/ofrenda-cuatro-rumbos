#ifndef MEMORIA_SPRITES
#define MEMORIA_SPRITES
#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _Color;
float _Intensity;

// Cada sprite es un quad con sus 4 vertices en el mismo centro.
// uv1 = esquina en metros, uv2 = parametros, color.a = fase.
struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float2 corner : TEXCOORD1;
    float4 data : TEXCOORD2;
    float4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR;
    UNITY_FOG_COORDS(1)
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f Billboard(appdata v, float3 objectCenter, float2 corner, float4 color)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    float3 viewCenter = UnityObjectToViewPos(objectCenter);
    viewCenter.xy += corner;
    o.pos = mul(UNITY_MATRIX_P, float4(viewCenter, 1));
    o.uv = v.uv;
    o.color = color;
    UNITY_TRANSFER_FOG(o, o.pos);
    return o;
}
#endif
