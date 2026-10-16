Shader "Sokoban/Pressure Dissolve"
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
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        _EdgeWidth ("Edge Width", Range(0.005,0.2)) = 0.06
        _EdgeColor ("Dissolve Edge Color", Color) = (1,0.55,0.2,1)
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
                half4 _EdgeColor;
                half _Metallic;
                half _Smoothness;
                float _DissolveAmount;
                float _EdgeWidth;
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
                half fogFactor : TEXCOORD3;
                float3 positionOS : TEXCOORD4;
                float2 emissionUV : TEXCOORD5;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.emissionUV = TRANSFORM_TEX(input.uv, _EmissionMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            float HashNoise(float3 position)
            {
                position = frac(position * 0.1031);
                position += dot(position, position.yzx + 33.33);
                return frac((position.x + position.y) * position.z);
            }

            float SmoothNoise(float3 position)
            {
                float3 cell = floor(position);
                float3 blend = frac(position);
                blend = blend * blend * (3.0 - 2.0 * blend);

                float n000 = HashNoise(cell + float3(0, 0, 0));
                float n100 = HashNoise(cell + float3(1, 0, 0));
                float n010 = HashNoise(cell + float3(0, 1, 0));
                float n110 = HashNoise(cell + float3(1, 1, 0));
                float n001 = HashNoise(cell + float3(0, 0, 1));
                float n101 = HashNoise(cell + float3(1, 0, 1));
                float n011 = HashNoise(cell + float3(0, 1, 1));
                float n111 = HashNoise(cell + float3(1, 1, 1));

                float x00 = lerp(n000, n100, blend.x);
                float x10 = lerp(n010, n110, blend.x);
                float x01 = lerp(n001, n101, blend.x);
                float x11 = lerp(n011, n111, blend.x);
                return lerp(lerp(x00, x10, blend.y), lerp(x01, x11, blend.y), blend.z);
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
                // Object-space smooth noise keeps the breakup attached to the
                // pumpkin and avoids the harsh, pixel-like world-space speckle.
                float noise = SmoothNoise(input.positionOS * 3.0);
                float amount = saturate(_DissolveAmount);
                clip(noise - amount);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 albedoHSV = RGBToHSV(max(albedo.rgb, 0.0));
                float3 channelHSV = RGBToHSV(max(_ChannelColor.rgb, 0.0));
                albedoHSV.x = channelHSV.x;
                albedoHSV.y = lerp(albedoHSV.y, max(albedoHSV.y, channelHSV.y), 0.85);
                albedo.rgb = HSVToRGB(albedoHSV);
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.emissionUV).rgb * _EmissionColor.rgb;
                float edge = 1.0 - smoothstep(amount, amount + _EdgeWidth, noise);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.alpha = albedo.a;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0.04, 0.04, 0.04);
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = emission + _EdgeColor.rgb * edge * 2.0;
                surfaceData.occlusion = 1;
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }
    }
}
