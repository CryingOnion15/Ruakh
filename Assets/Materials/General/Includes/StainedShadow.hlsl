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

float _CascadeBlendRange = 20;
float _CascadeSlopBias[4] = {0.1, 0.1, 0.1, 0.5};
float4 _ShadowParams;
float2 _ShadowTexelSize;
float3 _LightDirection;

static const float2 poisson[8] = {
    float2(-0.326, -0.406),
    float2(-0.840, -0.074),
    float2(-0.696,  0.457),
    float2(-0.203,  0.621),
    float2( 0.962, -0.195),
    float2( 0.473, -0.480),
    float2( 0.519,  0.767),
    float2( 0.185, -0.893)
};


float GetCameraDepth(float3 worldPos) {
    float3 viewPos = TransformWorldToView(worldPos);
    float depth = -viewPos.z;
    depth = (depth - _ProjectionParams.y) / (+_ProjectionParams.z - _ProjectionParams.y);
    return depth;
}

float GetCameraDepthTest(float3 worldPos)
{
    float3 viewPos = TransformWorldToView(worldPos);
    return -viewPos.z;
}


uint GetCascadeIndex(float3 worldPos) {
    float depth = GetCameraDepthTest(worldPos);

    uint index = 0;
    index += depth > _StainedCascadeBounds[1];
    index += depth > _StainedCascadeBounds[2];
    index += depth > _StainedCascadeBounds[3];

    return index;
}

float GetCascadeBlend(float3 worldPos, uint cascadeIndex) {

    if (cascadeIndex >= 3) return 0.0;

    float depth = GetCameraDepthTest(worldPos);

    // Blend based on distance between bounds
    float lower = _StainedCascadeBounds[cascadeIndex];
    float upper = _StainedCascadeBounds[cascadeIndex + 1];

    return saturate((depth - lower) / _CascadeBlendRange);
}

float2 GetLightSpaceUV(float3 worldPos, uint cascadeIndex) {
    float4 clipPos = mul(_StainedShadowVPMatrix[cascadeIndex], float4(worldPos, 1.0));
    float2 uv = clipPos.xy / clipPos.w * 0.5 + 0.5;

    return floor(uv / _ShadowTexelSize) * _ShadowTexelSize;
}

// Sample for the shadow color.
float4 SampleStainedShadowColor(float3 worldPos)
{
    float depth = GetCameraDepthTest(worldPos);
    uint cascadeIndex = GetCascadeIndex(depth);
    float blend = GetCascadeBlend(depth, cascadeIndex);

    float2 uv = GetLightSpaceUV(cascadeIndex, worldPos);
    float4 color = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowColorMap, sampler_StainedShadowColorMap, uv.xy, cascadeIndex);

    if (cascadeIndex < 3 && blend > 0.0)
    {
        float2 uv1 = GetLightSpaceUV(cascadeIndex + 1, worldPos);
        float4 color1 = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowColorMap, sampler_StainedShadowColorMap, uv1.xy, cascadeIndex + 1);

        return lerp(color, color1, blend);
    }

    return color;
}


// Sample shadow depth with cascade blending
float SampleShadowDepthBlend(float3 worldPos, uint cascadeIndex, float2 poissonOffset)
{
    float2 uv = GetLightSpaceUV(worldPos, cascadeIndex) + poissonOffset;

    if (cascadeIndex >= 3) 
        return SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, uv, cascadeIndex).r;

    // Compute blend factor
    float blend = GetCascadeBlend(worldPos, cascadeIndex);

    // Get UVs for next cascade
    float2 uv1 = GetLightSpaceUV(worldPos, cascadeIndex + 1) + poissonOffset;

    // Sample both cascades
    float depth = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, uv, cascadeIndex).r;
    float depth1 = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, uv1, cascadeIndex + 1).r;

    return lerp(depth, depth1, blend);
}

float GetLightSpaceDepth(float3 worldPos) {
    uint cascadeIndex = GetCascadeIndex(worldPos);
    float4 lightViewPos = mul(_StainedShadowViewMatrix[cascadeIndex], float4(worldPos, 1.0));
    float lightViewDepth = lightViewPos.z;
    
    return saturate(lightViewDepth * _ShadowParams.z - _ShadowParams.w);
}

float GetDepthBlend(float2 uv, uint cascadeIndex, float scale, float3 worldPos, uint poissonIndex) {
    float2 sample = saturate(uv + (poisson[poissonIndex] * _ShadowTexelSize.xy * scale));
    float blend = 0.0;

    if(cascadeIndex < 3) {
        blend = GetCascadeBlend(worldPos, cascadeIndex);
    }

    float sampledDepth = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, sample, cascadeIndex).r;

    if(blend > 0.0) {
        float2 blendSample = saturate(GetLightSpaceUV(worldPos, cascadeIndex + 1) + (poisson[poissonIndex] * _ShadowTexelSize.xy * scale));
        float blendDepth = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, blendSample, cascadeIndex + 1).r;

        return lerp(sampledDepth, blendDepth, blend);
    }

    return sampledDepth; 
}

float GetBias(float3 normal, uint cascadeIndex) {
    //float bias = 0.005;
    float slopeBias = _CascadeSlopBias[cascadeIndex];
    return slopeBias * (1.0 - dot(normal, _LightDirection));
}

// Main shadow test with Poisson sampling
float SHADOW_TEST(float3 worldPos, float3 normal)
{
    uint cascadeIndex = GetCascadeIndex(worldPos);
    float currentDepth = GetLightSpaceDepth(worldPos);
    float scale = lerp(0.75, 2.5, cascadeIndex / 3.0);

    float shadow = 0.0;

    // Loop through Poisson offsets
    for (int i = 0; i < 8; i++)
    {
        float2 poissonOffset = poisson[i] * _ShadowTexelSize.xy * scale;
        float sampledDepth = SampleShadowDepthBlend(worldPos, cascadeIndex, poissonOffset);
        shadow += step(sampledDepth, currentDepth + GetBias(normal, cascadeIndex));
    }

    return shadow / 8.0;
}
