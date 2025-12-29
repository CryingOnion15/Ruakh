#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_StainedShadowColorMap);
SAMPLER(sampler_StainedShadowColorMap);

TEXTURE2D(_StainedShadowDepthMap);
SAMPLER(sampler_StainedShadowDepthMap);

TEXTURE2D(_LightSpaceOutlineTexture);
SAMPLER(sampler_LightSpaceOutlineTexture);

// Global Variables
float4x4 _StainedShadowVPMatrix;
float4x4 _StainedShadowViewMatrix;
float4 _ShadowParams;
float2 _ShadowTexelSize;

float2 GetLightSpaceUV(float3 worldPos) {
    float4 clipPos = mul(_StainedShadowVPMatrix, float4(worldPos, 1.0));
    float2 uv = clipPos.xy / clipPos.w * 0.5 + 0.5;

    return uv;
}

// Sample for the shadow color.
float4 SampleStainedShadowColor(float3 worldPos)
{
    float2 uv = GetLightSpaceUV(worldPos);

    if (any(uv.xy < 0.0) || any(uv.xy > 1.0))
        return 0.0;

    return SAMPLE_TEXTURE2D(_StainedShadowColorMap, sampler_StainedShadowColorMap, uv.xy);
}

float4 SampleStainedShadowColorAverage(float3 worldPos)
{
    float2 uv = GetLightSpaceUV(worldPos);

    if (any(uv.xy < 0.0) || any(uv.xy > 1.0))
        return 0.0;

    return SAMPLE_TEXTURE2D(_StainedShadowColorMap, sampler_StainedShadowColorMap, uv.xy);
}

// Sample the light space depth map.
float SampleStainedShadowDepth(float3 worldPos)
{
    float2 uv = GetLightSpaceUV(worldPos);

    if (any(uv.xy < 0.0) || any(uv.xy > 1.0))
        return 1.0;

    return SAMPLE_TEXTURE2D(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, uv.xy).r;
}

float GetLightSpaceDepth(float3 worldPos) {
    float4 lightViewPos = mul(_StainedShadowViewMatrix, float4(worldPos, 1.0));
    float lightViewDepth = lightViewPos.z;
    
    return saturate(lightViewDepth * _ShadowParams.z - _ShadowParams.w);
}

float LIGHT_SPACE_OUTLINE_TEST(float3 worldPos) {
    float2 uv = GetLightSpaceUV(worldPos);

    if (any(uv.xy < 0.0) || any(uv.xy > 1.0))
        return 1.0;

    return SAMPLE_TEXTURE2D(_LightSpaceOutlineTexture, sampler_LightSpaceOutlineTexture, uv.xy).r;
}

// 3x3 PCF filtering step.
float GetShadowPCF(float3 worldPos) {
    float2 uv = GetLightSpaceUV(worldPos);
    float currentDepth = GetLightSpaceDepth(worldPos);
    float shadow = 0.0;

    // 3x3 PCF kernel
    for (int x = -1; x <= 1; x++)
    {
        for (int y = -1; y <= 1; y++)
        {
            float2 sample = uv + (float2(x, y) * _ShadowTexelSize.xy);
            if (any(sample < 0.0) || any(sample > 1.0))
                continue;

            float sampledDepth =
                SAMPLE_TEXTURE2D(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, sample).r;

            shadow += step(sampledDepth, currentDepth);
        }
    }

    return shadow / 9.0;
}
