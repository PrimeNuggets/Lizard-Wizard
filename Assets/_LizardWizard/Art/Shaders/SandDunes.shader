//-- based on NedMakesGames sand dune tutorial https://youtu.be/KqWfo6EPjCw
Shader "LizardWizard/Sand Dunes"
{
    Properties
    {
        [Header(Dune Shape)]
        _PatternScale ("Pattern Scale (per meter)", Float) = 0.025
        _SineFrequency ("Sine Frequency", Float) = 2
        _SineRotation ("Sine Rotation (radians)", Float) = 0.4
        _SineAmplitude ("Sine Amplitude", Float) = 0.3
        _DistortNoiseScale ("Distort Noise Scale", Float) = 2
        _DistortAmplitude ("Distort Amplitude", Float) = 0.15
        _PolygonSides ("Polygon Sides", Float) = 4
        _UVTiling ("Polygon UV Tiling", Vector) = (1, 1, 0, 0)

        [Header(Wind Ripples)]
        _WindDirection ("Wind Direction (XZ)", Vector) = (1, 0.35, 0, 0)
        _RippleFrequency ("Ripples Per Meter", Float) = 0.8
        _RippleSharpness ("Ripple Sharpness", Range(0.5, 0.95)) = 0.78
        _RippleWarp ("Ripple Warp", Float) = 2.5
        _RippleWarpScale ("Ripple Warp Scale", Float) = 0.08
        _RippleStrength ("Ripple Normal Strength", Float) = 0.08
        _RippleColorStrength ("Ripple Color Strength", Range(0, 1)) = 0.15
        _RippleFlatness ("Ripple Slope Cutoff", Range(0, 1)) = 0.8
        _RippleCoverage ("Ripple Coverage", Range(0, 1)) = 0.55
        _RippleCoverageScale ("Ripple Patch Scale", Float) = 0.03
        _RippleFadeDistance ("Ripple Fade Distance", Float) = 60

        [Header(Colors)]
        _SandLowColor ("Sand Low Color", Color) = (0.87, 0.6, 0.36, 1)
        _SandHighColor ("Sand High Color", Color) = (0.99, 0.82, 0.56, 1)
        _SubstrateColor ("Substrate Color", Color) = (0.8, 0.52, 0.32, 1)
        _ColorBands ("Color Bands (0 = smooth)", Float) = 4
        _BandSoftness ("Band Softness", Range(0.01, 1)) = 0.35
        _SlopeColor ("Steep Slope Color (A = amount)", Color) = (0.86, 0.52, 0.32, 0.5)
        _ValleyColor ("Valley Color (A = amount)", Color) = (0.82, 0.5, 0.33, 0.45)
        _ValleyHeightMin ("Valley Height Min", Float) = 0
        _ValleyHeightMax ("Valley Height Max", Float) = 15

        [Header(Grain)]
        _GrainDensity ("Grain Density (per meter)", Float) = 20
        _GrainColorBlend ("Grain Color Blend", Range(0, 1)) = 0.06
        _GrainFadeStart ("Grain Fade Start", Float) = 10
        _GrainFadeEnd ("Grain Fade End", Float) = 35

        [Header(Substrate)]
        _SubstrateDuneHeightMax ("Substrate Dune Height Max", Float) = 0.12
        _SubstrateGrainHeightMax ("Substrate Grain Height Max", Float) = 0.5
        _SubstrateTightening ("Substrate Tightening", Float) = 4

        [Header(Normals)]
        _NormalSampleRadius ("Normal Sample Radius", Float) = 0.05
        _NormalStrength ("Normal Strength", Float) = 0.04

        [Header(Shine)]
        [HDR] _ShineColor ("Shine Color", Color) = (1.6, 1.45, 1.2, 1)
        _GrainShineStrength ("Grain Shine Strength", Float) = 0.5
        _ShineThreshold ("Shine Threshold", Range(0, 1)) = 0.97
        _ShineDuneHeightMin ("Shine Dune Height Min", Float) = 0.5

        [Header(Toon Lighting)]
        _ShadowColor ("Shadow Color", Color) = (0.62, 0.45, 0.5, 1)
        _ShadeThreshold ("Shade Threshold", Range(-1, 1)) = 0.05
        _ShadeSoftness ("Shade Softness", Range(0.001, 1)) = 0.12
        _ShadeGradient ("Lit Side Gradient", Range(0, 1)) = 0.35
        _AmbientStrength ("Ambient Strength", Range(0, 2)) = 0.5
        _RimColor ("Crest Rim Color", Color) = (1, 0.85, 0.65, 0.35)
        _RimPower ("Crest Rim Power", Float) = 4

        [Header(Distance Haze)]
        _HazeColor ("Haze Color (A = amount)", Color) = (0.97, 0.86, 0.74, 0.45)
        _HazeStart ("Haze Start", Float) = 40
        _HazeEnd ("Haze End", Float) = 220
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _PatternScale;
            float _SineFrequency;
            float _SineRotation;
            float _SineAmplitude;
            float _DistortNoiseScale;
            float _DistortAmplitude;
            float _PolygonSides;
            float4 _UVTiling;
            float4 _WindDirection;
            float _RippleFrequency;
            float _RippleSharpness;
            float _RippleWarp;
            float _RippleWarpScale;
            float _RippleStrength;
            float _RippleColorStrength;
            float _RippleFlatness;
            float _RippleCoverage;
            float _RippleCoverageScale;
            float _RippleFadeDistance;
            half4 _SandLowColor;
            half4 _SandHighColor;
            half4 _SubstrateColor;
            float _ColorBands;
            float _BandSoftness;
            half4 _SlopeColor;
            half4 _ValleyColor;
            float _ValleyHeightMin;
            float _ValleyHeightMax;
            float _GrainDensity;
            float _GrainColorBlend;
            float _GrainFadeStart;
            float _GrainFadeEnd;
            float _SubstrateDuneHeightMax;
            float _SubstrateGrainHeightMax;
            float _SubstrateTightening;
            float _NormalSampleRadius;
            float _NormalStrength;
            half4 _ShineColor;
            float _GrainShineStrength;
            float _ShineThreshold;
            float _ShineDuneHeightMin;
            half4 _ShadowColor;
            float _ShadeThreshold;
            float _ShadeSoftness;
            float _ShadeGradient;
            float _AmbientStrength;
            half4 _RimColor;
            float _RimPower;
            half4 _HazeColor;
            float _HazeStart;
            float _HazeEnd;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "SandDunesNoise.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            float2 RotateUV(float2 uv, float radians)
            {
                float s = sin(radians);
                float c = cos(radians);
                return float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
            }

            //-- SandPattern subgraph from the tutorial
            float SandPattern(float2 uv)
            {
                float2 rotated = RotateUV(uv, _SineRotation);
                float sineWave = sin(rotated.y * _SineFrequency * TWO_PI) * _SineAmplitude;

                float noise = SandLayeredNoise(uv * _DistortNoiseScale, 0.5, 2, 3) * 2 - 1;
                float wave = sineWave + noise * _DistortAmplitude;

                float2 polygonUV = frac(uv * _UVTiling.xy + wave);
                return SandPolygonGradient(polygonUV, _PolygonSides, 1, 1);
            }

            //-- slow rise, sharp drop
            float RippleProfile(float phase)
            {
                float t = frac(phase);
                float k = _RippleSharpness;
                float tri = t < k ? t / k : (1 - t) / (1 - k);
                return smoothstep(0, 1, tri);
            }

            half3 SimpleLight(Light light, half3 albedo, float3 normalWS)
            {
                float ndl = saturate(dot(normalWS, light.direction));
                return albedo * light.color * ndl * light.distanceAttenuation * light.shadowAttenuation;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 positionWS = input.positionWS;
                float3 geoNormal = normalize(input.normalWS);
                float2 uv = positionWS.xz * _PatternScale;
                float camDist = distance(GetCameraPositionWS(), positionWS);
                float grainFade = 1 - smoothstep(_GrainFadeStart, _GrainFadeEnd, camDist);
                float flatness = smoothstep(_RippleFlatness - 0.15, _RippleFlatness + 0.1, geoNormal.y);

                float height = SandPattern(uv);

                float2 windDir = normalize(_WindDirection.xy + float2(0.0001, 0));
                float warp = (SandLayeredNoise(positionWS.xz * _RippleWarpScale, 0.5, 2, 2) * 2 - 1) * _RippleWarp;
                warp += SandPerlin(positionWS.xz * _RippleWarpScale * 6) * _RippleWarp * 0.35;
                float phase = dot(positionWS.xz, windDir) * _RippleFrequency + warp;
                float ripple = RippleProfile(phase);
                float rippleSlope = (RippleProfile(phase + 0.02) - RippleProfile(phase - 0.02)) / 0.04;
                //-- stops shimmering far away
                float rippleFade = (1 - saturate(fwidth(phase) * 3)) * flatness;
                rippleFade *= 1 - smoothstep(_RippleFadeDistance * 0.4, _RippleFadeDistance, camDist);
                //-- ripple patches
                float patchNoise = SandLayeredNoise(positionWS.xz * _RippleCoverageScale + 17.3, 0.5, 2, 2);
                float coverageEdge = 1 - _RippleCoverage;
                rippleFade *= smoothstep(coverageEdge - 0.12, coverageEdge + 0.12, patchNoise);
                float2 rippleGrad = rippleSlope * _RippleFrequency * windDir * rippleFade;

                float2 grainNoise = SandVoronoi(positionWS.xz * _GrainDensity);
                float grain = lerp(0.5, grainNoise.x, grainFade);

                float colorBlend = saturate(lerp(height, grain, _GrainColorBlend));
                colorBlend = saturate(colorBlend + (ripple - 0.5) * _RippleColorStrength * rippleFade);

                //-- toon bands
                if (_ColorBands > 0.5)
                {
                    float stepped = colorBlend * _ColorBands;
                    float band = floor(stepped);
                    float edge = smoothstep(0.5 - _BandSoftness * 0.5, 0.5 + _BandSoftness * 0.5, frac(stepped));
                    colorBlend = saturate((band + edge) / _ColorBands);
                }

                half3 albedo = lerp(_SandLowColor.rgb, _SandHighColor.rgb, colorBlend);

                //-- substrate
                float substrate = (1 - smoothstep(0, _SubstrateDuneHeightMax, height))
                                * (1 - smoothstep(0, _SubstrateGrainHeightMax, grain));
                substrate = pow(saturate(substrate), _SubstrateTightening);
                albedo = lerp(albedo, _SubstrateColor.rgb, substrate);

                //-- slope + valley color
                float steep = 1 - smoothstep(0.65, 0.92, geoNormal.y);
                albedo = lerp(albedo, _SlopeColor.rgb, steep * _SlopeColor.a);
                float valley = 1 - smoothstep(_ValleyHeightMin, _ValleyHeightMax, positionWS.y);
                albedo = lerp(albedo, _ValleyColor.rgb, valley * _ValleyColor.a);

                //-- better normal from the tutorial
                float r = _NormalSampleRadius;
                float hLeft = SandPattern(uv + float2(-r, 0));
                float hRight = SandPattern(uv + float2(r, 0));
                float hDown = SandPattern(uv + float2(0, -r));
                float hUp = SandPattern(uv + float2(0, r));
                float2 slope = float2(hLeft - hRight, hDown - hUp) / (2 * r) * _NormalStrength;
                slope -= rippleGrad * _RippleStrength;
                float3 normalTS = normalize(float3(slope, 1));

                //-- terrain has no tangents
                float3 tangentWS = cross(geoNormal, float3(0, 0, 1));
                tangentWS = dot(tangentWS, tangentWS) < 0.0001 ? float3(1, 0, 0) : normalize(tangentWS);
                float3 bitangentWS = cross(tangentWS, geoNormal);
                float3 normalWS = normalize(normalTS.x * tangentWS + normalTS.y * bitangentWS + normalTS.z * geoNormal);

                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);

                //-- toon lighting
                half4 shadowMask = half4(1, 1, 1, 1);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS), positionWS, shadowMask);
                float ndl = dot(normalWS, mainLight.direction);
                float lit = smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, ndl);
                lit *= mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                half3 litColor = mainLight.color * lerp(1, saturate(ndl), _ShadeGradient);
                half3 color = albedo * lerp(_ShadowColor.rgb, litColor, lit);

                color += albedo * SampleSH(normalWS) * _AmbientStrength;

                float rim = pow(1 - saturate(dot(normalWS, viewDirWS)), _RimPower);
                color += rim * lit * _RimColor.rgb * _RimColor.a * mainLight.color;

                //-- other lights
                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                uint pixelLightCount = GetAdditionalLightsCount();

                #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                {
                    color += SimpleLight(GetAdditionalLight(dirIndex, positionWS, shadowMask), albedo, normalWS);
                }
                #endif

                LIGHT_LOOP_BEGIN(pixelLightCount)
                    color += SimpleLight(GetAdditionalLight(lightIndex, positionWS, shadowMask), albedo, normalWS);
                LIGHT_LOOP_END
                #endif

                //-- sparkle
                float3 randomDir = SandRand1To3(grainNoise.y) * 2 - 1;
                float3 shineNormal = normalize(normalWS + randomDir * _GrainShineStrength);
                float shine = smoothstep(_ShineThreshold, 1, dot(shineNormal, viewDirWS));
                shine *= smoothstep(_ShineDuneHeightMin, 1, height) * grainFade * lit;
                color += shine * _ShineColor.rgb;

                //-- haze
                float haze = smoothstep(_HazeStart, _HazeEnd, camDist) * _HazeColor.a;
                color = lerp(color, _HazeColor.rgb, haze);

                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 ShadowVert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDir = normalize(_LightPosition - positionWS);
            #else
                float3 lightDir = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));
                return ApplyShadowClamping(positionCS);
            }

            half4 ShadowFrag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 DepthVert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.positionOS.xyz);
            }

            half4 DepthFrag(float4 positionCS : SV_POSITION) : SV_Target { return positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFrag(Varyings input) : SV_Target
            {
                return half4(normalize(input.normalWS), 0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
