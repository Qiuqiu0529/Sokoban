Shader "Sokoban/Channel Color Mapping"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.25
        _EmissionMap ("Emission Map", 2D) = "white" {}
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _ChannelColor ("Channel Color", Color) = (1,0.55,0.2,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _EmissionMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
                half4 _ChannelColor;
                half _Metallic;
                half _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float2 emissionUV : TEXCOORD3;
                half fogFactor : TEXCOORD4;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.emissionUV = TRANSFORM_TEX(input.uv, _EmissionMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            float3 RGBToHSV(float3 color)
            {
                float4 k = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(color.bg, k.wz), float4(color.gb, k.xy), step(color.b, color.g));
                float4 q = lerp(float4(p.xyw, color.r), float4(color.r, p.yzx), step(p.x, color.r));
                float delta = q.x - min(q.w, q.y);
                float epsilon = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * delta + epsilon)),
                    delta / (q.x + epsilon), q.x);
            }

            float3 HSVToRGB(float3 hsv)
            {
                float4 k = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(hsv.xxx + k.xyz) * 6.0 - k.www);
                return hsv.z * lerp(k.xxx, saturate(p - k.xxx), hsv.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 source = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 sourceHSV = RGBToHSV(max(source.rgb, 0.0));
                float3 channelHSV = RGBToHSV(max(_ChannelColor.rgb, 0.0));

                // Replace hue while retaining the source texture's value and
                // detail. Low-saturation pixels pick up the channel color too.
                sourceHSV.x = channelHSV.x;
                sourceHSV.y = lerp(sourceHSV.y, max(sourceHSV.y, channelHSV.y), 0.85);
                half3 mappedAlbedo = HSVToRGB(sourceHSV);
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.emissionUV).rgb * _EmissionColor.rgb;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = mappedAlbedo;
                surfaceData.alpha = source.a;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0.04, 0.04, 0.04);
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = emission;
                surfaceData.occlusion = 1;

                half4 result = UniversalFragmentPBR(inputData, surfaceData);
                result.rgb = MixFog(result.rgb, inputData.fogCoord);
                return result;
            }
            ENDHLSL
        }
    }
}
