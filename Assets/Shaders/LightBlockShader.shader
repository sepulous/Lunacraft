Shader "Custom/LightBlockShader"
{
    Properties
    {
        _BaseTexture("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
                "UniversalMaterialType" = "SimpleLit"
            }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

            TEXTURE2D(_BaseTexture);
            SAMPLER(sampler_BaseTexture);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;

                return OUT;
            }

            half4 frag(Varyings input) : SV_Target0
            {
                float4 textureColor = SAMPLE_TEXTURE2D(_BaseTexture, sampler_BaseTexture, input.uv);

                float viewDistance = length(_WorldSpaceCameraPos - input.positionWS) - 50;
                float fogFactor = saturate(exp(-0.08 * viewDistance));
                half4 halfFogColor = half4(unity_FogColor.rgb, 1);
                half4 halfTextureColor = half4(textureColor);
                //return lerp(half4(unity_FogColor.rgb, 1), half4(textureColor), fogFactor);
                return lerp(halfFogColor + (1 - unity_FogColor.a) * (halfTextureColor - halfFogColor), halfTextureColor, fogFactor);
            }

            ENDHLSL
        }
    }
}