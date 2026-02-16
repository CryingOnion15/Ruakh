Shader "Fullscreen/StainedGlassPostProcess"
{
    SubShader
    {
        Pass
        {
            Name "Full Screen Post Process Pass"
            Tags { "RenderType"="Opaque" "Queue"="Overlay" }
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Assets/Materials/General/Includes/StainedOutline.hlsl"
            #include "Assets/Materials/General/Includes/StainedShadow.hlsl"

            // Textures
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            TEXTURE2D(_StainedShadowMask);
            SAMPLER(sampler_StainedShadowMask);

            // Variables
            float4 _OutlineColor;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(uint vertexID : SV_VertexID)
            {
                float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
                v2f o;
                o.uv = uv;
                o.pos = float4(uv * 2.0 - 1.0, 0, 1);
                return o;
            }

            float3 ReconstructNormal(float3 world)
            {
                float3 dx = float3(ddx(world.x), ddx(world.y), ddx(world.z));
                float3 dy = float3(ddy(world.x), ddy(world.y), ddy(world.z));
                return normalize(cross(dy, dx));
            }

            float4 frag(v2f IN) : SV_Target
            {
                #if UNITY_UV_STARTS_AT_TOP
                    IN.uv.y = 1.0 - IN.uv.y;
                #endif

                // --- Sample the opaque screen color ---
                float4 screen = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, IN.uv);

                // --- Sample shadow mask ---
                float4 shadow = SAMPLE_TEXTURE2D(_StainedShadowMask, sampler_StainedShadowMask, IN.uv);

                return shadow;
            }

            ENDHLSL
        }
    }
}
