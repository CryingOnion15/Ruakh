#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_StainedShadowColorMap);
SAMPLER(sampler_StainedShadowColorMap);

TEXTURE2D(_StainedShadowDepthMap);
SAMPLER(sampler_StainedShadowDepthMap);

float4x4 _StainedShadowVPMatrix;
//float4 _ZBufferParams;

float LinearEyeDepth(float rawDepth) {
    return rawDepth / _ZBufferParams.z + _ZBufferParams.w;
}

// Sample for the shadow color.
float4 SampleStainedShadowColor(float3 worldPos)
{
    float4 lightSpacePos = mul(_StainedShadowVPMatrix, float4(worldPos, 1.0));
    float2 uv = lightSpacePos.xy / lightSpacePos.w * 0.5 + 0.5;

    // Optionally clamp to 0..1
    uv = saturate(uv);

    //return float4(uv,0,1);
    return SAMPLE_TEXTURE2D(_StainedShadowColorMap, sampler_StainedShadowColorMap, uv);
}

// Sample the light space depth map.
float SampleStainedShadowDepth(float3 worldPos)
{
    float4 lightSpacePos = mul(_StainedShadowVPMatrix, float4(worldPos, 1.0));
    float2 uv = lightSpacePos.xy / lightSpacePos.w * 0.5 + 0.5;

    uv = saturate(uv);

    return SAMPLE_TEXTURE2D(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, uv).r;
}

float GetLightSpaceDepth(float3 worldPos) {
    float4 lightSpacePos = mul(_StainedShadowVPMatrix, float4(worldPos, 1.0));
    return lightSpacePos.z / lightSpacePos.w * 0.5 + 0.5;
}
