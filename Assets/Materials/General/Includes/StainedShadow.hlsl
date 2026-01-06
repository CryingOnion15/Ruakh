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

float _CascadeBlendRange = 0.05;
float4 _ShadowParams;
float2 _ShadowTexelSize;

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

uint GetCascadeIndex(float3 worldPos) {
    float depth = GetCameraDepth(worldPos);

    uint index = 0;
    index += depth > _StainedCascadeBounds[1];
    index += depth > _StainedCascadeBounds[2];
    index += depth > _StainedCascadeBounds[3];

    return index;
}

float GetCascadeBlend(float3 worldPos, uint cascadeIndex) {
    float depth = GetCameraDepth(worldPos);
    float upper = _StainedCascadeBounds[cascadeIndex + 1];

    float blend = saturate((depth - (upper - _CascadeBlendRange)) / _CascadeBlendRange);

    return blend;
}

float2 GetLightSpaceUV(float3 worldPos, uint cascadeIndex) {
    float4 clipPos = mul(_StainedShadowVPMatrix[cascadeIndex], float4(worldPos, 1.0));
    float2 uv = clipPos.xy / clipPos.w * 0.5 + 0.5;

    return uv;
}

// Sample for the shadow color.
float4 SampleStainedShadowColor(float3 worldPos)
{
    uint cascadeIndex = GetCascadeIndex(worldPos);
    float2 uv = GetLightSpaceUV(worldPos, cascadeIndex);
    float blend = 0.0;
    
    if(cascadeIndex < 3) {
        blend = GetCascadeBlend(worldPos, cascadeIndex);
    }

    if (any(uv.xy < 0.0) || any(uv.xy > 1.0))
        return 0.0;

    float4 shadowColor = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowColorMap, sampler_StainedShadowColorMap, uv.xy, cascadeIndex);

    if(blend > 0.0) {
        float2 uv2 = GetLightSpaceUV(worldPos, cascadeIndex + 1);

        float4 shadowBlend = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowColorMap, sampler_StainedShadowColorMap, uv2.xy, cascadeIndex + 1);
        
        return lerp(shadowColor, shadowBlend, blend);
    }

    return shadowColor;
}

// Sample the light space depth map.
float SampleStainedShadowDepth(float3 worldPos)
{
    uint cascadeIndex = GetCascadeIndex(worldPos);
    float2 uv = GetLightSpaceUV(worldPos, cascadeIndex);
    float blend = 0.0;
    
    if(cascadeIndex < 3) {
        blend = GetCascadeBlend(worldPos, cascadeIndex);
    }
    
    if (any(uv.xy < 0.0) || any(uv.xy > 1.0))
        return 1.0;

    float shadowDepth = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthMap, sampler_StainedShadowDepthMap, uv.xy, cascadeIndex).r;

    if(blend > 0.0) {
        float2 uv2 = GetLightSpaceUV(worldPos, cascadeIndex + 1);

        float shadowBlend = SAMPLE_TEXTURE2D_ARRAY(_StainedShadowDepthMap, sampler_StainedShadowColorMap, uv2.xy, cascadeIndex + 1);
        
        return lerp(shadowDepth, shadowBlend, blend);
    }

    return shadowDepth;
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

// Get the shadow value based on poisson distribution offsets.
float SHADOW_TEST(float3 worldPos) {
    uint cascadeIndex = GetCascadeIndex(worldPos);
    float2 uv = GetLightSpaceUV(worldPos, cascadeIndex);
    float scale = lerp(.75, 2.5, cascadeIndex / 3.0);

    float currentDepth = GetLightSpaceDepth(worldPos);
    float shadow = 0.0;

    for (int i = 0; i < 8; i++)
    {
        float sampledDepth = GetDepthBlend(uv, cascadeIndex, scale, worldPos, i);

        shadow += step(sampledDepth, currentDepth);
    }

    return shadow / 8.0;
}
