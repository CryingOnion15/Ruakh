#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_StainedShadowColorMap);
SAMPLER(sampler_StainedShadowColorMap);

TEXTURE2D(_StainedShadowDepthMap);
SAMPLER(sampler_StainedShadowDepthMap);

TEXTURE2D(_LightSpaceOutlineTexture);
SAMPLER(sampler_LightSpaceOutlineTexture);

float4x4 _StainedShadowVPMatrix;

float3 GetLightSpaceUV(float3 worldPos) {
    float4 clipPos = mul(_StainedShadowVPMatrix, float4(worldPos, 1.0));
    float3 uv = float3((clipPos.xy / clipPos.w) * 0.5 + 0.5, clipPos.z / clipPos.w);

    return uv;
}

float4 TestValue(float3 world) {
    float4 clip = mul(_StainedShadowVPMatrix, float4(world, 1.0));
    float3 NDC = clip.xyz / clip.w;
    float3 uv = NDC * 0.5 + 0.5;

    if (any(NDC < -1.0) || any(NDC > 1.0)) {
        return 0.0;
    }

    return float4(uv.xy, 0.0, 1.0);
}

// Sample for the shadow color.
float4 SampleStainedShadowColor(float3 worldPos)
{
    float3 uv = GetLightSpaceUV(worldPos);

    if (any(uv.xy < 0.0) || any(uv.xy > 1.0))
        return 0.0;

    return SAMPLE_TEXTURE2D(_StainedShadowColorMap, sampler_StainedShadowColorMap, uv.xy);
}

// Sample the light space depth map.
float SampleStainedShadowDepth(float3 worldPos)
{
    float3 uv = GetLightSpaceUV(worldPos);

    if (any(uv.xy < 0.0) || any(uv.xy > 1.0))
        return 1.0;

    return SAMPLE_TEXTURE2D(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, uv.xy).r;
}

float GetLightSpaceDepth(float3 worldPos) {
    float3 uv = GetLightSpaceUV(worldPos);

    if (any(uv.xy < 0.0) || any(uv.xy > 1.0))
        return 1.0;

    return uv.z;
}

float LIGHT_SPACE_OUTLINE_TEST(float3 worldPos) {
    float3 uv = GetLightSpaceUV(worldPos);

    if (any(uv.xy < 0.0) || any(uv.xy > 1.0))
        return 1.0;

    return SAMPLE_TEXTURE2D(_LightSpaceOutlineTexture, sampler_LightSpaceOutlineTexture, uv.xy).r;
}
