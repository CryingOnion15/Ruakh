Shader "CustomRenderPass/ObjectOutlineShader"
{
    SubShader
    {
        Pass 
        {
            Name "EDGE DETECTION OUTLINE - UV space"
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            // Samplers 
            TEXTURE2D(_ColorTex);
            SAMPLER(sampler_ColorTex);
            TEXTURE2D(_DepthTex);
            SAMPLER(sampler_DepthTex);
            TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);

            // Globals

            // Shader properties.
            float _OutlineThickness;
            float4 _OutlineColor;
            float _depthThreshold;
            float _colorThreshold;
            float _normalThreshold;

            // Vertex shader is provided by the Blit.hlsl include
            #pragma vertex Vert 
            #pragma fragment frag

            // Edge detection kernel that works by taking the sum of the squares of the differences between diagonally adjacent pixels (Roberts Cross).
            float RobertsCross(float3 samples[4])
            {
                const float3 difference_1 = samples[1] - samples[2];
                const float3 difference_2 = samples[0] - samples[3];
                return sqrt(dot(difference_1, difference_1) + dot(difference_2, difference_2));
            }

            // The same kernel logic as above, but for a single-value instead of a vector3.
            float RobertsCross(float samples[4])
            {
                const float difference_1 = samples[1] - samples[2];
                const float difference_2 = samples[0] - samples[3];
                return sqrt(difference_1 * difference_1 + difference_2 * difference_2);
            }

            // struct appdata
            // {
            //     float2 uv : TEXCOORD0;
            // };

            // struct v2f
            // {
            //     float2 uv : TEXCOORD0;
            // };

            // v2f vert(appdata v) {
            //     v2f o;

            //     o.uv = v.uv;

            //     #if UNITY_UV_STARTS_AT_TOP
            //         o.uv.y = 1.0 - o.uv.y;
            //     #endif

            //     return o;
            // }


            float4 frag(Varyings IN) : SV_TARGET
            {
                // Screen-space coordinates which we will use to sample.
                float2 uv = IN.texcoord;
                
                // Generate 4 diagonally placed samples.
                const float half_width_f = floor(_OutlineThickness * 0.5);
                const float half_width_c = ceil(_OutlineThickness * 0.5);

                float2 texelSize = 1 / float2(1024.0, 1024.0);

                float2 uvs[4];
                uvs[0] = uv + texelSize * float2(half_width_f, half_width_c) * float2(-1, 1);  // top left
                uvs[1] = uv + texelSize * float2(half_width_c, half_width_c) * float2(1, 1);   // top right
                uvs[2] = uv + texelSize * float2(half_width_f, half_width_f) * float2(-1, -1); // bottom left
                uvs[3] = uv + texelSize * float2(half_width_c, half_width_f) * float2(1, -1);  // bottom right
                
                float3 normal_samples[4];
                float depth_samples[4], luminance_samples[4];
                
                for (int i = 0; i < 4; i++) {
                    depth_samples[i] = SAMPLE_TEXTURE2D(_DepthTex, sampler_DepthTex, uvs[i]);
                    luminance_samples[i] = SAMPLE_TEXTURE2D(_ColorTex, sampler_ColorTex, uvs[i]);

                    // Unpack normal values from 0-1 back to -1-1.
                    normal_samples[i] = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, uvs[i]) * 2.0 - 1.0;
                }
                
                // Apply edge detection kernel on the samples to compute edges.
                float edge_depth = RobertsCross(depth_samples);
                float edge_normal = RobertsCross(normal_samples);
                float edge_luminance = RobertsCross(luminance_samples);
                
                // Threshold the edges (discontinuity must be above certain threshold to be counted as an edge).
                edge_depth = edge_depth > _depthThreshold ? 1 : 0;
                
                edge_normal = edge_normal > _normalThreshold ? 1 : 0;
                
                edge_luminance = edge_luminance > _colorThreshold ? 1 : 0;
                
                // Combine the edges from depth/normals/luminance using the max operator.
                float edge = max(edge_depth, max(edge_normal, edge_luminance));

                // Return if there is an edge or not.
                return float4(edge,0,0,1);
            }
            ENDHLSL
        }
    }
}