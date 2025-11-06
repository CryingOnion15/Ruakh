Shader "Fullscreen/StainedGlassPostProcess"
{
    SubShader
    {
        Pass
        {
            Name "Full Screen Post Process Pass"
            Tags { "LightMode" = "Always" "RenderType"="Opaque" "Queue"="Overlay" }
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Materials/General/Includes/StainedShadow.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // Textures
            //TEXTURE2D(_CameraDepthTexture);
            //SAMPLER(sampler_CameraDepthTexture);

            // Variables
            float4 _OutlineColor;

            struct attributes
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(uint vertexID : SV_VertexID)
            {
                float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
                v2f o;
                o.uv = uv;
                o.pos = float4(uv * 2.0 - 1.0, 0, 1);
                return o;
            }

            float3 GetWorldPosition(float2 uv) {
                // Sample depth
                float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv);

                // Convert to clip space
                float4 clipPos;
                clipPos.xy = uv * 2.0 - 1.0;
                clipPos.z = depth * 2.0 - 1.0;
                clipPos.w = 1.0;

                // Transform to world space
                float4 worldPos = mul(UNITY_MATRIX_I_VP, clipPos);
                return worldPos.xyz / worldPos.w;
            }

            float4 frag (v2f IN) : SV_Target
            {
                #if UNITY_UV_STARTS_AT_TOP
                    IN.uv.y = 1.0 - IN.uv.y;
                #endif

                float3 world = GetWorldPosition(IN.uv);

                float depth = SampleSceneDepth(IN.uv).r;
                depth = LinearEyeDepth(depth)

                // //Convert the Position to Light Space Coordinates.
                float pixelLightDepth = GetLightSpaceDepth(world);
                // float3 shadowColor = SampleStainedShadowColor(world);
                // float shadowDepth = SampleStainedShadowDepth(world);

                // // Returns 1 if shadow color should be used.
                // // If pixel depth is greater than the shadow map depth.
                // float shadowTest = step(0, shadowDepth - pixelLightDepth);
                //return float4(lerp(_BaseColor.rgb, shadowColor, shadowTest), 1);
                return float4(,0,0,1);
            }
            ENDHLSL
        }
    }
}
