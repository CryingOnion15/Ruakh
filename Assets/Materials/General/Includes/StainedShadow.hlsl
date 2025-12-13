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
float _ShadowNear;
float _ShadowFar;

float3 GetLightSpaceUV(float3 worldPos) {
    float4 clipPos = mul(_StainedShadowVPMatrix, float4(worldPos, 1.0));
    float3 uv = clipPos.xyz / clipPos.w * 0.5 + 0.5;

    return uv;
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
    float4 lightViewPos = mul(_StainedShadowViewMatrix, float4(worldPos, 1.0));
    float lightViewDepth = lightViewPos.z;
    
    return saturate((lightViewDepth - _ShadowNear) / (_ShadowFar - _ShadowNear));
}

float LIGHT_SPACE_OUTLINE_TEST(float3 worldPos) {
    float3 uv = GetLightSpaceUV(worldPos);

    if (any(uv.xy < 0.0) || any(uv.xy > 1.0))
        return 1.0;

    return SAMPLE_TEXTURE2D(_LightSpaceOutlineTexture, sampler_LightSpaceOutlineTexture, uv.xy).r;
}
