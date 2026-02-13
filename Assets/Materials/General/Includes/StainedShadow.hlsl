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
CBUFFER_END

CBUFFER_START(StainedCascadeBounds)
   float _StainedCascadeBounds[5];
CBUFFER_END

CBUFFER_START(StainedShadowParams)
    float4 _ShadowParams[4];
CBUFFER_END

float _CascadeBlendRange = 0.1; // fraction of cascade
float _CascadeSlopBias[4] = {.05, .05, .08, .1};
float2 _ShadowTexelSize;
float3 _LightDirection;

static const float2 poisson[8] = {
    float2(-0.326, -0.406), float2(-0.840, -0.074),
    float2(-0.696,  0.457), float2(-0.203,  0.621),
    float2( 0.962, -0.195), float2( 0.473, -0.480),
    float2( 0.519,  0.767), float2( 0.185, -0.893)
};

//-------------------------------------------
// Camera depth utilities
//-------------------------------------------
float GetCameraDepth(float3 worldPos) {
    float3 viewPos = TransformWorldToView(worldPos);
    float depth = -viewPos.z;
    depth = (depth - _ProjectionParams.y) / (_ProjectionParams.z - _ProjectionParams.y);
    return depth;
}

float GetCameraDepthTest(float3 worldPos)
{
    float3 viewPos = TransformWorldToView(worldPos);
    return -viewPos.z;
}

//-------------------------------------------
// Cascade utilities
//-------------------------------------------
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
    float lower = _StainedCascadeBounds[cascadeIndex];
    float upper = _StainedCascadeBounds[cascadeIndex + 1];

    float blendRange = (upper - lower) * _CascadeBlendRange;
    return saturate((depth - (upper - blendRange)) / blendRange);
}

//-------------------------------------------
// Light-space utilities
//-------------------------------------------
float2 GetLightSpaceUV(float3 worldPos, uint cascadeIndex) {
    float4 clipPos = mul(_StainedShadowVPMatrix[cascadeIndex], float4(worldPos, 1.0));
    float2 uv = clipPos.xy / clipPos.w * 0.5 + 0.5;
    return saturate(uv);
}

float GetLightSpaceDepth(float3 worldPos, uint cascadeIndex) {
    float4 lightViewPos = mul(_StainedShadowViewMatrix[cascadeIndex], float4(worldPos,1.0));
    float near = _ShadowParams[cascadeIndex].x;
    float far  = _ShadowParams[cascadeIndex].y;
    return saturate((lightViewPos.z - near) / (far - near));
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

//-------------------------------------------
// Sample functions
//-------------------------------------------
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

float SampleShadowDepthBlend(float3 worldPos, uint cascadeIndex, float2 poissonOffset)
{
    float2 uv = saturate(GetLightSpaceUV(worldPos, cascadeIndex) + poissonOffset);

    if(cascadeIndex >= 3)
        return SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, uv, cascadeIndex).r;

    float blend = GetCascadeBlend(worldPos, cascadeIndex);
    float depth0 = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, uv, cascadeIndex).r;

    float2 uv1 = saturate(GetLightSpaceUV(worldPos, cascadeIndex + 1) + poissonOffset);
    float depth1 = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, uv1, cascadeIndex + 1).r;

    return lerp(depth0, depth1, blend);
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

//-------------------------------------------
// Robust bias
//-------------------------------------------
float GetBias(float3 worldPos, float3 normal, uint cascadeIndex)
{
    float3 L = normalize(-_LightDirection);

    // Fragment distance in light space
    float4 lightPos4 = mul(_StainedShadowViewMatrix[cascadeIndex], float4(worldPos,1));
    float lightDepth = lightPos4.z;
    float near = _ShadowParams[cascadeIndex].x;
    float far = _ShadowParams[cascadeIndex].y;
    float depthRange = far - near;

    // Slope-based bias (higher at grazing angles)
    float slopeBias = saturate(1.0 - abs(dot(normal, L)));

    // Distance-based bias (larger bias for fragments near the near plane)
    float distanceBias = pow((lightDepth - near) / depthRange, 0.75);

    // Base world bias in meters
    float baseBias = 0.01 + 0.02 * cascadeIndex;

    // Total normalized bias
    float totalBias = (baseBias + slopeBias * 0.1 + distanceBias * 0.05) / depthRange;
    return totalBias;
}


//-------------------------------------------
// Main shadow test
//-------------------------------------------
float SHADOW_TEST(float3 worldPos, float3 normal)
{
    uint cascadeIndex = GetCascadeIndex(worldPos);
    float bias = GetBias(worldPos, normal, cascadeIndex);

    // Small offset along normal to reduce self-shadowing
    float3 offsetPos = worldPos + normal * 0.001; // ~1mm along normal
    float currentDepth = GetLightSpaceDepthBlend(offsetPos, cascadeIndex);

    float scale = lerp(0.75, 2.5, cascadeIndex / 3.0);
    float shadow = 0.0;

    [unroll]
    for (int i = 0; i < 8; i++)
    {
        float2 poissonOffset = poisson[i] * _ShadowTexelSize * scale;

        // Sample depth with cascade blending
        float sampledDepth = SampleShadowDepthBlend(offsetPos, cascadeIndex, poissonOffset);

        // Step comparison using your convention
        shadow += step(sampledDepth, currentDepth + bias);
    }

    return shadow / 8.0;
}


