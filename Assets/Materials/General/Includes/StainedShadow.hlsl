#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D_ARRAY(_StainedShadowColorMap);
SAMPLER(sampler_StainedShadowColorMap);

TEXTURE2D_ARRAY(_StainedShadowDepthTexture);
SAMPLER(sampler_StainedShadowDepthTexture);

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

float GetCameraWorldDepth(float3 worldPos)
{
    float3 viewPos = TransformWorldToView(worldPos);
    return -viewPos.z;
}

uint GetCascadeIndex(float3 worldPos) {
    float depth = GetCameraWorldDepth(worldPos);

    uint index = 0;
    index += depth > _StainedCascadeBounds[1];
    index += depth > _StainedCascadeBounds[2];
    index += depth > _StainedCascadeBounds[3];

    return index;
}

float GetCascadeBlend(float3 worldPos, uint cascadeIndex) {
    if (cascadeIndex >= 3) return 0.0;

    float depth = GetCameraWorldDepth(worldPos);
    float lower = _StainedCascadeBounds[cascadeIndex];
    float upper = _StainedCascadeBounds[cascadeIndex + 1];

    float blendStart = upper - (upper - lower) * _CascadeBlendRange;
    
    return saturate((depth - blendStart) / ((upper - lower) * _CascadeBlendRange));
}

//-------------------------------------------
// Light-space utilities
//-------------------------------------------
float2 GetLightSpaceUV(float3 worldPos, uint cascadeIndex) {
    float4 clipPos = mul(_StainedShadowVPMatrix[cascadeIndex], float4(worldPos, 1.0));
    float2 uv = clipPos.xy / clipPos.w * 0.5 + 0.5;
    return uv;
}

float GetLightSpaceDepth(float3 worldPos, uint cascadeIndex)
{
    float4 lightClip = mul(_StainedShadowVPMatrix[cascadeIndex], float4(worldPos,1));
    float linearDepth = lightClip.z;
    return linearDepth;
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
        return SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthTexture, sampler_StainedShadowDepthTexture, uv, cascadeIndex).r;

    float blend = GetCascadeBlend(worldPos, cascadeIndex);
    float depth0 = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthTexture, sampler_StainedShadowDepthTexture, uv, cascadeIndex).r;

    float2 uv1 = saturate(GetLightSpaceUV(worldPos, cascadeIndex + 1) + poissonOffset);
    float depth1 = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthTexture, sampler_StainedShadowDepthTexture, uv1, cascadeIndex + 1).r;

    return lerp(depth0, depth1, blend);
}

//-------------------------------------------
// Bias in NDC units (0–1), ready for step()
//-------------------------------------------
float GetBias(float3 normal, uint cascadeIndex)
{
    float3 L = normalize(_LightDirection);
    float slope = max(0.0, 1.0 - dot(normal,L)); // safe slope
    float bias = 0.005 + slope * 0.01 + cascadeIndex * 0.002; // tweak in shadow map units
    return bias;
}


//-------------------------------------------
// Main shadow test with proper bias
//-------------------------------------------
float SHADOW_TEST(float3 worldPos, float3 normal)
{
    uint cascadeIndex = GetCascadeIndex(worldPos);
    float bias = GetBias(normal, cascadeIndex);

    // Current fragment depth in light space (already linearized)
    float currentDepth = GetLightSpaceDepthBlend(worldPos, cascadeIndex);

    float shadow = 0.0;
    float totalWeight = 0.0;

    // Poisson disk scale based on cascade
    float scale = 1.0 + cascadeIndex * 0.5; // near cascades smaller, far cascades wider
    float2 texelSize = _ShadowTexelSize * scale;

    [unroll]
    for (int i = 0; i < 8; i++)
    {
        float2 offset = poisson[i] * texelSize;

        // Clamp UVs instead of saturate to avoid wrapping artifacts
        float2 uv = saturate(GetLightSpaceUV(worldPos, cascadeIndex) + offset);

        // Sample depth with cascade blending
        float sampledDepth = SampleShadowDepthBlend(worldPos, cascadeIndex, offset);

        // Optional: simple weight based on distance from center
        float weight = 1.0; // could use gaussian: weight = exp(-length(offset*10.0)^2);

        shadow += step(currentDepth, sampledDepth + bias + 0.0001) * weight;
        totalWeight += weight;
    }

    return shadow / totalWeight;
}



