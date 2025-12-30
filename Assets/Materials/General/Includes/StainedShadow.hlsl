#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D_ARRAY(_StainedShadowColorMap);
SAMPLER(sampler_StainedShadowColorMap);

TEXTURE2D_ARRAY(_StainedShadowDepthMap);
SAMPLER(sampler_StainedShadowDepthMap);

TEXTURE2D(_LightSpaceOutlineTexture);
SAMPLER(sampler_LightSpaceOutlineTexture);

// Global Variables
CBUFFER_START(StainedShadowMatrices)
    float4x4 _StainedShadowVPMatrix[4];
    float4x4 _StainedShadowViewMatrix[4];
    //float4x4 _StainedShadowProjMatrix[4];
CBUFFER_END

CBUFFER_START(StainedCascadeBounds)
   float _StainedCascadeBounds[5];
CBUFFER_END

float4 _ShadowParams;
//float4 _ProjectionParams;
float2 _ShadowTexelSize;

uint GetCascadeIndex(float3 worldPos) {
    float3 viewPos = TransformWorldToView(worldPos);
    float depth = -viewPos.z;
    depth = (depth - _ProjectionParams.y) / (+_ProjectionParams.z - _ProjectionParams.y);

    uint index = 0;
    index += depth > _StainedCascadeBounds[1];
    index += depth > _StainedCascadeBounds[2];
    index += depth > _StainedCascadeBounds[3];

    return index;
}

float2 GetLightSpaceUV(float3 worldPos) {
    uint casIndex = GetCascadeIndex(worldPos);
    float4 clipPos = mul(_StainedShadowVPMatrix[casIndex], float4(worldPos, 1.0));
    float2 uv = clipPos.xy / clipPos.w * 0.5 + 0.5;

    return uv;
}

// Sample for the shadow color.
float4 SampleStainedShadowColor(float3 worldPos)
{
    float2 uv = GetLightSpaceUV(worldPos);
    uint casIndex = GetCascadeIndex(worldPos);

    if (any(uv.xy < 0.0) || any(uv.xy > 1.0))
        return 0.0;

    return SAMPLE_TEXTURE2D_ARRAY(_StainedShadowColorMap, sampler_StainedShadowColorMap, uv.xy, casIndex);
}

// float4 SampleStainedShadowColorAverage(float3 worldPos)
// {
//     float2 uv = GetLightSpaceUV(worldPos);

//     if (any(uv.xy < 0.0) || any(uv.xy > 1.0))
//         return 0.0;

//     return SAMPLE_TEXTURE2D(_StainedShadowColorMap, sampler_StainedShadowColorMap, uv.xy);
// }

// Sample the light space depth map.
float SampleStainedShadowDepth(float3 worldPos)
{
    float2 uv = GetLightSpaceUV(worldPos);
    uint casIndex = GetCascadeIndex(worldPos);

    if (any(uv.xy < 0.0) || any(uv.xy > 1.0))
        return 1.0;

    return SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, uv.xy, casIndex).r;
}

float GetLightSpaceDepth(float3 worldPos) {
    uint casIndex = GetCascadeIndex(worldPos);
    float4 lightViewPos = mul(_StainedShadowViewMatrix[casIndex], float4(worldPos, 1.0));
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
    uint casIndex = GetCascadeIndex(worldPos);
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
                SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, sample, casIndex).r;

            shadow += step(sampledDepth, currentDepth);
        }
    }

    return shadow / 9.0;
}
