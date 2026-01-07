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
            //Blend SrcAlpha OneMinusSrcAlpha

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

            float3 ReconstructNormal(float3 world) {
                float3 dx = float3(ddx(world.x), ddx(world.y), ddx(world.z));
                float3 dy = float3(ddy(world.x), ddy(world.y), ddy(world.z));

                return normalize(cross(dy, dx));
            }

            float4 frag (v2f IN) : SV_Target
            {
                // Revers the uv if is starts from the top.
                #if UNITY_UV_STARTS_AT_TOP
                    IN.uv.y = 1.0 - IN.uv.y;
                #endif

                // Get the depth at the pixel.
                float depth = SampleSceneDepth(IN.uv);
                float linearDepth = Linear01Depth(depth, _ZBufferParams);

                // Get the scene color.
                float4 sceneColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, IN.uv);

                // Get the world point.
                float3 world = ComputeWorldSpacePosition(IN.uv, depth, UNITY_MATRIX_I_VP);
                float3 normal = ReconstructNormal(world);

                // Get the shadow color.
                float4 shadowColor = SampleStainedShadowColor(world);

                // Exclude skybox pixels
                float isValidReciever = 1.0 - step(.999, linearDepth);

                // Outline Tests
                float screenOutlineTest = SCREEN_POS_OUTLINE_TEST(IN.uv);
                //float shadowOutlineTest = 1.0 - LIGHT_SPACE_OUTLINE_TEST(world);

                // Shadow test: 1 = shadowed, 0 = lit
                float shadowTest = SHADOW_TEST(world, normal) * isValidReciever;

                // Override scene color with shadow color.
                sceneColor = lerp(sceneColor, shadowColor, shadowTest);

                // Enforce outline color and draw the rest.
                sceneColor = lerp(sceneColor, _OutlineColor, screenOutlineTest);

                //Debug Cascades
                uint cIndex = GetCascadeIndex(world);

                float4 color;

                if(cIndex == 0) {
                    color = float4(1.0,0.0,1.0,1.0);
                } else if(cIndex == 1) {
                    color = float4(0.0,1.0,0.0,1.0);
                } else if(cIndex == 2) {
                    color = float4(0.0,0.0,1.0,1.0);
                } else {
                    color = float4(1.0,1.0,0.0,1.0);
                }

                return lerp(sceneColor, color, 0.5);
                return sceneColor;
            }
            ENDHLSL
        }
    }
}
