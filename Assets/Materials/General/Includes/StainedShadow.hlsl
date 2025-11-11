#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_StainedShadowColorMap);
SAMPLER(sampler_StainedShadowColorMap);

TEXTURE2D(_StainedShadowDepthMap);
SAMPLER(sampler_StainedShadowDepthMap);

float4x4 _StainedShadowVPMatrix;

float3 GetLightSpaceUV(float3 worldPos) {
    float4 clipPos = mul(_StainedShadowVPMatrix, float4(worldPos, 1.0));
    float3 uv = (clipPos.xyz / clipPos.w) * 0.5 + 0.5;

    #if UNITY_UV_STARTS_AT_TOP
        uv.y = 1.0 - uv.y;
    #endif

    return uv;
}

// Sample for the shadow color.
float4 SampleStainedShadowColor(float3 worldPos)
{
    float3 uv = GetLightSpaceUV(worldPos);

    // Outside light frustum → no contribution
    if (any(uv.xy < 0.0) || any(uv.xy > 1.0))
        return 0.0;

    return SAMPLE_TEXTURE2D(_StainedShadowColorMap, sampler_StainedShadowColorMap, uv.xy);
    //return float4(uv.xy, 0.0, 1.0);
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
    return 1.0 - mul(_StainedShadowVPMatrix, float4(worldPos, 1.0)).z;
}
