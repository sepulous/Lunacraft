Shader "Custom/OpaqueShader"
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

            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

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
                float fogFactor    : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;

                // Compute fog factor based on distance
                float distance = length(mul(UNITY_MATRIX_MV, IN.positionOS).xyz) - 60;
                OUT.fogFactor = saturate(exp(-0.08 * distance));

                return OUT;
            }

            float3 LightBlockLighting(float3 displacement, Light light)
            {
                float strength = 0;
                float3 discrete_displacement = float3(
                    floor(displacement.x + 0.5001),
                    floor(displacement.y + 0.5001),
                    floor(displacement.z + 0.5001)
                );
                float dist_squared = dot(discrete_displacement, discrete_displacement);
                if (dist_squared < 82)
                    strength = 1 / dist_squared;

                return light.color * strength;
            }

            // This function loops through the lights in the scene
            float3 MyLightLoop(float3 textureColor, InputData inputData)
            {
                float3 lighting = 0;

                // Sun (skybox)
                Light mainLight = GetMainLight();
                float NdotL = (dot(inputData.normalWS, mainLight.direction) + 1) * 0.5;
                lighting += NdotL * mainLight.color;

                // Light blocks
                #if defined(_ADDITIONAL_LIGHTS)

                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                    float3 lightPos = _AdditionalLightsPosition[lightIndex];
                    float3 displacement = inputData.positionWS - lightPos;
                    lighting += LightBlockLighting(displacement, additionalLight);
                    if (any(lighting + 0.05 >= 1.5))
                        break;
                LIGHT_LOOP_END

                #endif

                return textureColor * clamp(lighting + 0.05, 0, 1.5);
            }

            half4 frag(Varyings input) : SV_Target0
            {
                half4 halfFogColor = half4(unity_FogColor.rgb, 1);
                float viewDistance = length(_WorldSpaceCameraPos - input.positionWS) - 20;
                float fogFactor = saturate(exp(-0.12 * viewDistance));

                // The Forward+ light loop (LIGHT_LOOP_BEGIN) requires the InputData struct to be in its scope.
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = input.normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                float3 textureColor = SAMPLE_TEXTURE2D(_BaseTexture, sampler_BaseTexture, input.uv).rgb;
                float3 lighting = MyLightLoop(textureColor, inputData);

                half4 halfLighting = half4(lighting.rgb, 1);
                return lerp(halfFogColor + (1 - unity_FogColor.a)*(halfLighting - halfFogColor), halfLighting, fogFactor);
            }

            ENDHLSL
        }
    }
}
