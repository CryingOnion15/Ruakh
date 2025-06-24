Shader "Custom/LightParticleVertAndFrag"
{
    Properties{
        color1 ("Color 1", Color) = (1,1,1,1)
        color2 ("Color 2", Color) = (1,1,1,1)
        color3 ("Color 3", Color) = (1,1,1,1)
        color4 ("Color 4", Color) = (1,1,1,1)
        EdgeColor ("EdgeColor", Color) = (1,1,1,1)
        WaveTexture("Wave Texture", 2D) = "white" {}
        EdgeThreshold ("Edge Threshold", float) = 0.1
        Displacement("Displacement", float) = 3.0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Buffer
            StructuredBuffer<float3> vertices;
            StructuredBuffer<float3> baryCoords;
            StructuredBuffer<float2> uvs;
            StructuredBuffer<int> colors;

            // Colors
            float4 color1;
            float4 color2;
            float4 color3;
            float4 color4;
            float4 EdgeColor;

            // Texture Sampling
            Texture2D WaveTexture;
            SamplerState sampler_WaveTexture;

            float EdgeThreshold;
            int cellIndex;
            float cellSize;

            // Axis & Positioning
            float3 xAxis;
            float3 yAxis;
            float3 zAxis;
            float3 world;
            float Displacement;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 bary : TEXCOORD1;

            };
            
            float4 GetColor(int index) {
                if(index == 0) return color1;
                if(index == 1) return color2;
                if(index == 2) return color3;
                return color4;
            }

            float3 GetDisplacement(int triIndex) {
                int base = triIndex * 3;

                int x = cellIndex % 4;
                int y = 3 - (cellIndex / 4); // We do 3 - () because we want the top down y index.

                float2 uvLocal = float2(x, y ) * 0.25;

                float2 uv1 = uvs[base];
                float2 sampleUV1 = uv1 * .25 + uvLocal;
                sampleUV1 = clamp(sampleUV1, uvLocal, uvLocal + .25);

                float2 uv2 = uvs[base + 1];
                float2 sampleUV2 = uv2 * .25 + uvLocal;
                sampleUV2 = clamp(sampleUV2, uvLocal, uvLocal + .25);

                float2 uv3 = uvs[base + 2];
                float2 sampleUV3 = uv3 * .25 + uvLocal;
                sampleUV3 = clamp(sampleUV3, uvLocal, uvLocal + .25);

                float4 samp1 = WaveTexture.SampleLevel(sampler_WaveTexture, sampleUV1, 0);
                float4 samp2 = WaveTexture.SampleLevel(sampler_WaveTexture, sampleUV2, 0);
                float4 samp3 = WaveTexture.SampleLevel(sampler_WaveTexture, sampleUV3, 0);

                float4 avg = (samp1 + samp2 + samp3) / 3.0;
                float avgStep = step(.66, dot(avg.rgb, float3(0.2126, 0.7152, 0.0722)));
                return Displacement * avg * avgStep * zAxis;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                int index = IN.vertexID;

                float3 vertexPos = vertices[index];

                int triIndex = index / 3;
                float3 displacement = GetDisplacement(triIndex);

                // Get the world rotation.
                float3 right = vertexPos.x * xAxis;
                float3 up = vertexPos.y * yAxis;
                float3 forward = vertexPos.z * zAxis;
                
                OUT.pos = TransformObjectToHClip(right + up + forward + world + displacement);

                int colorIndex = colors[IN.vertexID / 3];

                OUT.uv = uvs[index];
                OUT.color = GetColor(colorIndex);

                OUT.bary = baryCoords[index];

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float minBary = min(IN.bary.x, min(IN.bary.y, IN.bary.z));

                int x = cellIndex % 4;
                int y = 3 - (cellIndex / 4); // We do 3 - () because we want the top down y index.

                float2 uvLocal = float2(x * 0.25, y * 0.25);
                float2 clampedUV = clamp(IN.uv * .25 + uvLocal, 0.0, 1.0);

                float4 texColor = WaveTexture.SampleLevel(sampler_WaveTexture, clampedUV , 0);
                float luminance = dot(texColor.rgb, float3(0.2126, 0.7152, 0.0722));
                float value = step(.9, luminance);

                if(value == 1.0) {
                    return minBary < EdgeThreshold ? EdgeColor : IN.color;
                } else {
                    return IN.color;
                }
                
            }

            ENDHLSL
        }
    }
}
