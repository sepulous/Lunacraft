Shader "Custom/SulphurCrystalShader"
{
    Properties
    {
        _MainTex("Crystal Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Stencil
            {
                Ref 1
                Comp GEqual
                Pass Keep
            }

            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha // Premultiplied alpha
            ZWrite Off                 // Disable depth writing for transparency
            Cull Back                  // Only render front

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct VertexData
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct v2f
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float2 uv          : TEXCOORD1;
            };

            v2f vert(VertexData IN)
            {
                v2f i;
                i.positionHCS = TransformObjectToHClip(IN.positionOS);
                i.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                i.uv = IN.uv;

                return i;
            }

            half4 frag(v2f i) : SV_Target
            {
                Light mainLight = GetMainLight();
                float correctedIntensity = saturate(length(mainLight.color) + 0.2);
                float4 textureColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                float viewDistance = length(_WorldSpaceCameraPos - i.positionWS) - 40;
                float fogFactor = saturate(exp(-0.12 * viewDistance));
                half4 halfFogColor = half4(unity_FogColor.rgb, 1);
                half4 halfTextureColor = half4(textureColor.rgb * correctedIntensity, textureColor.a);
                //return lerp(half4(unity_FogColor.rgb, 1), half4(textureColor.rgb * correctedIntensity, textureColor.a), fogFactor);
                return lerp(halfFogColor + (1 - unity_FogColor.a) * (halfTextureColor - halfFogColor), halfTextureColor, fogFactor);
            }

            ENDHLSL
        }
    }
}