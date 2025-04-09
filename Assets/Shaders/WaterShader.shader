Shader "Custom/WaterShader"
{
    Properties
    {
        _Color("Water Color", Color) = (0.46, 0.57, 0.69, 0.9)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Replace
            }

            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha // Premultiplied alpha
            ZWrite Off                 // Disable depth writing for transparency
            Cull Back                  // Only render front
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

            float4 _Color;

            struct VertexData
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct v2f
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
            };

            v2f vert(VertexData IN)
            {
                v2f OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);

                return OUT;
            }

            half4 frag(v2f v) : SV_Target
            {
                Light mainLight = GetMainLight();
                float correctedIntensity = saturate(length(mainLight.color) + 0.2) * mainLight.shadowAttenuation;

                float viewDistance = length(_WorldSpaceCameraPos - v.positionWS) - 50;
                float fogFactor = saturate(exp(-0.08 * viewDistance));
                half4 halfFogColor = half4(unity_FogColor.rgb, 1);
                half4 halfWaterColor = half4(_Color.rgb * correctedIntensity, _Color.a);
                //return lerp(half4(unity_FogColor.rgb, 1), half4(_Color.rgb * correctedIntensity, _Color.a), fogFactor);
                return lerp(halfFogColor + (1 - unity_FogColor.a) * (halfWaterColor - halfFogColor), halfWaterColor, fogFactor);
            }

            ENDHLSL
        }
    }
}