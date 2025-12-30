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
            
                float3 world = ComputeWorldSpacePosition(IN.uv, depth, UNITY_MATRIX_I_VP);
                float4 shadowColor = SampleStainedShadowColor(world);
                
                // Compare shadow depth vs light-space depth
                float shadowDepth = SampleStainedShadowDepth(world);
                float pixelLightDepth = GetLightSpaceDepth(world);

                // Exclude skybox pixels
                float isValidReciever = 1.0 - step(.999, linearDepth);

                // Outline Tests
                float screenOutlineTest = SCREEN_POS_OUTLINE_TEST(IN.uv);
                //float shadowOutlineTest = 1.0 - LIGHT_SPACE_OUTLINE_TEST(world);

                // Shadow test: 1 = shadowed, 0 = lit
                // Add * shadowOutlineTest back in when texture is bigger & details can be there.
                float shadowTest = step(shadowDepth, pixelLightDepth) * isValidReciever;
                float shadowPCF = GetShadowPCF(world);

                // Override scene color with shadow color.
                sceneColor = lerp(sceneColor, shadowColor, shadowTest * shadowPCF);

                // Enforce outline color and draw the rest.
                sceneColor = lerp(sceneColor, _OutlineColor, screenOutlineTest);
            
                return sceneColor;
            }
            ENDHLSL
        }
    }
}
