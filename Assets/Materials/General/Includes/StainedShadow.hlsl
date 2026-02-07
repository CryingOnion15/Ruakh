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

CBUFFER_START(StainedShadowParams)
    float4 _ShadowParams[4];
CBUFFER_END

float _CascadeBlendRange = 20;
float _CascadeSlopBias[4] = {1.0, 1.0, 1.0, 2.0};
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

float2 GetLightSpaceUV(float3 worldPos, uint cascadeIndex) {
    float4 clipPos = mul(_StainedShadowVPMatrix[cascadeIndex], float4(worldPos, 1.0));
    float2 uv = clipPos.xy / clipPos.w * 0.5 + 0.5;
    return uv; // Remove the floor() operation
}

float GetCascadeBlend(float3 worldPos, uint cascadeIndex) {

    if (cascadeIndex >= 3) return 0.0;

    float depth = GetCameraDepthTest(worldPos);
    float lower = _StainedCascadeBounds[cascadeIndex];
    float upper = _StainedCascadeBounds[cascadeIndex + 1];

    // Blend range based on actual cascade bounds, not fixed value
    float cascadeRange = upper - lower;
    float blendRange = cascadeRange * 0.1; 

    return saturate((depth - (upper - blendRange)) / blendRange);
}

// Sample for the shadow color.
float4 SampleStainedShadowColor(float3 worldPos)
{
    float depth = GetCameraDepthTest(worldPos);
    uint cascadeIndex = GetCascadeIndex(worldPos);
    float blend = GetCascadeBlend(worldPos, cascadeIndex);

    float2 uv = GetLightSpaceUV(worldPos, cascadeIndex);
    float4 color = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowColorMap, sampler_StainedShadowColorMap, uv.xy, cascadeIndex);

    if (cascadeIndex < 3 && blend > 0.0)
    {
        float2 uv1 = GetLightSpaceUV(worldPos, cascadeIndex + 1);
        float4 color1 = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowColorMap, sampler_StainedShadowColorMap, uv1.xy, cascadeIndex + 1);

        return lerp(color, color1, blend);
    }

    return color;
}


// Sample shadow depth with cascade blending
float SampleShadowDepthBlend(float3 worldPos, uint cascadeIndex, float2 poissonOffset)
{
    float2 uv = saturate(GetLightSpaceUV(worldPos, cascadeIndex) + poissonOffset);

    if (cascadeIndex >= 3) 
        return SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, uv, cascadeIndex).r;

    // Compute blend factor
    float blend = GetCascadeBlend(worldPos, cascadeIndex);

    // Get UVs for next cascade
    float2 uv1 = saturate(GetLightSpaceUV(worldPos, cascadeIndex + 1) + poissonOffset);

    // Sample both cascades
    float depth = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, uv, cascadeIndex).r;
    float depth1 = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, uv1, cascadeIndex + 1).r;

    return lerp(depth, depth1, blend);
}

float GetLightSpaceDepth(float3 worldPos, uint cascadeIndex) {
    float4 lightViewPos = mul(_StainedShadowViewMatrix[cascadeIndex], float4(worldPos, 1.0));
    float lightViewDepth = lightViewPos.z;
    
    // Properly normalize to [0, 1] range
    float near = _ShadowParams[cascadeIndex].x;
    float far = _ShadowParams[cascadeIndex].y;
    return (lightViewDepth - near) / (far - near);
}

float GetLightSpaceDepthBlend(float3 worldPos, uint cascadeIndex) {
    float depth = GetLightSpaceDepth(worldPos, cascadeIndex);

    if(cascadeIndex < 3) {
        float blend = GetCascadeBlend(worldPos, cascadeIndex);
        float nextDepth = GetLightSpaceDepth(worldPos, cascadeIndex + 1);
        return lerp(depth, nextDepth, blend);
    }

    return depth;
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
    // Much more aggressive bias
    float baseBias = lerp(0.002, 0.008, cascadeIndex / 3.0);
    float slopeBias = _CascadeSlopBias[cascadeIndex] * 0.02 * (1.0 - abs(dot(normal, _LightDirection)));
    return baseBias + slopeBias;
}

// Main shadow test with Poisson sampling
float SHADOW_TEST(float3 worldPos, float3 normal)
{
    uint cascadeIndex = GetCascadeIndex(worldPos);
    float currentDepth = GetLightSpaceDepthBlend(worldPos, cascadeIndex);  // BLEND HERE
    float scale = lerp(0.75, 2.5, cascadeIndex / 3.0);
    float bias = GetBias(normal, cascadeIndex);

    float shadow = 0.0;

    // Loop through Poisson offsets
    for (int i = 0; i < 8; i++)
    {
        float2 poissonOffset = poisson[i] * _ShadowTexelSize.xy * scale;
        float sampledDepth = SampleShadowDepthBlend(worldPos, cascadeIndex, poissonOffset);
        shadow += step(sampledDepth, currentDepth + bias);
    }

    return shadow / 8.0;
}
