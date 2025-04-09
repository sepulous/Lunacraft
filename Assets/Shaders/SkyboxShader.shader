Shader "Custom/SkyboxShader"
{
    Properties
    {
        _MainTex("Cubemap", Cube) = "" {}
        _RotationAngle("Rotation Angle", Range(0, 360)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Background" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            samplerCUBE _MainTex;
            float _RotationAngle;

            struct appdata_t
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float c = cos(radians(_RotationAngle));
                float s = sin(radians(_RotationAngle));
                float3x3 rotationMatrix = float3x3(
                    1, 0, 0,
                    0, c, -s,
                    0, s, c
                );
                float3 rotatedCoord = mul(rotationMatrix, i.texcoord);
                return texCUBE(_MainTex, rotatedCoord);
            }
            ENDCG
        }
    }
}