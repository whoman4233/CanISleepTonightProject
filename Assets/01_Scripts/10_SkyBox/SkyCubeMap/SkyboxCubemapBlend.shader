// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Skybox/CubemapBlend"
{
    Properties
    {
        _Tex1 ("Cubemap 1", CUBE) = "" {}
        _Tex2 ("Cubemap 2", CUBE) = "" {}
        _Blend ("Blend", Range(0,1)) = 0
        _Rotation ("Rotation", Range(0,360)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" }
        Cull Off
        ZWrite Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            samplerCUBE _Tex1;
            samplerCUBE _Tex2;
            float _Blend;
float _Rotation;

// ★ 여기에 함수 추가 ★
float3 RotateAroundYInDegrees(float3 dir, float degrees)
{
    float rad = radians(degrees);
    float s = sin(rad);
    float c = cos(rad);

    float3x3 rotationMatrix = float3x3(
        c, 0, -s,
        0, 1,  0,
        s, 0,  c
    );

    return mul(rotationMatrix, dir);
}

struct appdata
{
    float4 vertex : POSITION;
};

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // 카메라에서 보는 방향
                o.dir = mul((float3x3)unity_ObjectToWorld, v.vertex.xyz);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed3 dir = normalize(i.dir);
                dir = RotateAroundYInDegrees(dir, _Rotation);
                fixed4 c1 = texCUBE(_Tex1, dir);
                fixed4 c2 = texCUBE(_Tex2, dir);
                return lerp(c1, c2, _Blend);
            }
            ENDCG
        }
    }
}