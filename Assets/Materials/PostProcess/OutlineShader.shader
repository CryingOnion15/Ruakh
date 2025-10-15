Shader "CustomRenderPass/OutlineShader"
{
    Properties
    {
        _OutlineThickness ("Outline Thickness", Float) = 1
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _FilteredColor ("Filtered Color", 2D) = "black" {}
        _FilteredDepth ("Filtered Depth", 2D) = "black" {}
        _FilteredNormals ("Filtered Normals", 2D) = "black" {}
    }

    SubShader
    {
        Pass 
        {
            Name "EDGE DETECTION OUTLINE"
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            // Samplers 
            TEXTURE2D(_FilteredColor);
            SAMPLER(sampler_FilteredColor);
            TEXTURE2D(_FilteredDepth);
            SAMPLER(sampler_FilteredDepth);
            TEXTURE2D(_FilteredNormals);
            SAMPLER(sampler_FilteredNormals);

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

            half4 frag(Varyings IN) : SV_TARGET
            {
                // Screen-space coordinates which we will use to sample.
                float2 uv = IN.texcoord;
                float2 texel_size = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);
                
                // Generate 4 diagonally placed samples.
                const float half_width_f = floor(_OutlineThickness * 0.5);
                const float half_width_c = ceil(_OutlineThickness * 0.5);

                float2 uvs[4];
                uvs[0] = uv + texel_size * float2(half_width_f, half_width_c) * float2(-1, 1);  // top left
                uvs[1] = uv + texel_size * float2(half_width_c, half_width_c) * float2(1, 1);   // top right
                uvs[2] = uv + texel_size * float2(half_width_f, half_width_f) * float2(-1, -1); // bottom left
                uvs[3] = uv + texel_size * float2(half_width_c, half_width_f) * float2(1, -1);  // bottom right
                
                float3 normal_samples[4];
                float depth_samples[4], luminance_samples[4];
                
                for (int i = 0; i < 4; i++) {
                    depth_samples[i] = SAMPLE_TEXTURE2D(_FilteredDepth, sampler_FilteredDepth, uvs[i]);
                    luminance_samples[i] = SAMPLE_TEXTURE2D(_FilteredColor, sampler_FilteredColor, uvs[i]);

                    // Unpack normal values from 0-1 back to -1-1.
                    normal_samples[i] = SAMPLE_TEXTURE2D(_FilteredNormals, sampler_FilteredNormals, uvs[i]) * 2.0 - 1.0;
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

                // Get screen color
                float4 screenColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);

                // Color the edge with a custom color.
                return lerp(screenColor, _OutlineColor, edge);
            }
            ENDHLSL
        }
    }
}