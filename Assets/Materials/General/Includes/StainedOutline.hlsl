#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_ScreenSpaceOutlineTexture);
SAMPLER(sampler_ScreenSpaceOutlineTexture);

// TEXTURE2D(_StainedShadowDepthMap);
// SAMPLER(sampler_StainedShadowDepthMap);

//float4x4 _StainedShadowVPMatrix;
// Global screen parames set by unity. x = ScreenWidth, y = ScreenHeight
//float2 _ScreenParams;

float SCREEN_OUTLINE_TEST(float4 pos) {
    float2 uv = pos.xy / _ScreenParams.xy;
#if UNITY_UV_STARTS_AT_TOP
    uv.y = 1.0 - uv.y;
#endif

    float4 ScreenTest = SAMPLE_TEXTURE2D(_ScreenSpaceOutlineTexture, sampler_ScreenSpaceOutlineTexture, float2(.5,.5));
    return ScreenTest.r;
}
