//-- toon lighting shared by characters and props (see Toon_Credits.txt)

#ifndef LIZARD_TOON_LIGHTING
#define LIZARD_TOON_LIGHTING

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct ToonSettings
{
    float ambient;
    float shadowRampPoint;
    half4 specularColor;
    float specularSize;
    half4 rimColor;
    float rimSize;
};

//-- point/spot lights get a hard edge and two falloff bands so they don't look smooth next to the sun
half3 ToonExtraLight(Light light, half3 albedo, float3 normalWS)
{
    float lit = smoothstep(0, 0.05, dot(normalWS, light.direction));
    float atten = saturate(light.distanceAttenuation * light.shadowAttenuation);
    atten = 0.5 * smoothstep(0.08, 0.12, atten) + 0.5 * smoothstep(0.45, 0.5, atten);
    return albedo * light.color * lit * atten;
}

half3 ToonShade(half3 albedo, float3 positionWS, float3 normalWS, float3 viewDirWS, float4 positionCS, ToonSettings s,
                TEXTURE2D_PARAM(rampMap, rampSampler))
{
    half4 shadowMask = half4(1, 1, 1, 1);
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS), positionWS, shadowMask);

    //-- half lambert picks the spot on the ramp, left side of the ramp is shadow, right side is lit
    //-- cast shadows clamp it down to the shadow part so they match the ramp colors
    float shadow = smoothstep(0.2, 0.8, mainLight.shadowAttenuation) * mainLight.distanceAttenuation;
    float u = saturate(dot(normalWS, mainLight.direction) * 0.5 + 0.5);
    u = min(u, lerp(s.shadowRampPoint, 1, shadow));
    half3 ramp = SAMPLE_TEXTURE2D(rampMap, rampSampler, float2(u, 0.5)).rgb;

    half3 color = albedo * ramp * mainLight.color;
    color += albedo * SampleSH(normalWS) * s.ambient;

    //-- highlights only on the lit side of the ramp
    float lit = smoothstep(0.6, 0.64, u);

    //-- hard specular dot using the half angle
    float ndh = saturate(dot(normalWS, SafeNormalize(mainLight.direction + viewDirWS)));
    float specEdge = 1 - s.specularSize;
    float specWidth = max(fwidth(ndh), 0.002);
    float spec = smoothstep(specEdge - specWidth, specEdge + specWidth, ndh) * lit;
    color += spec * s.specularColor.rgb * s.specularColor.a * mainLight.color;

    //-- fresnel rim on the lit side, helps characters pop off the sand
    float fresnel = 1 - saturate(dot(normalWS, viewDirWS));
    float rimEdge = 1 - s.rimSize;
    float rimWidth = max(fwidth(fresnel), 0.01);
    float rim = smoothstep(rimEdge - rimWidth, rimEdge + rimWidth, fresnel) * lit;
    color += rim * s.rimColor.rgb * s.rimColor.a * mainLight.color;

    #if defined(_ADDITIONAL_LIGHTS)
    InputData inputData = (InputData)0;
    inputData.positionWS = positionWS;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionCS);
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
    {
        color += ToonExtraLight(GetAdditionalLight(dirIndex, positionWS, shadowMask), albedo, normalWS);
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        color += ToonExtraLight(GetAdditionalLight(lightIndex, positionWS, shadowMask), albedo, normalWS);
    LIGHT_LOOP_END
    #endif

    return color;
}

#endif
