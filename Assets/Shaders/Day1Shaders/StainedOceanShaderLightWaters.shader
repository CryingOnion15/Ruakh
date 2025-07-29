Shader "Custom/StainedOceanLightWaters"
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
        ZTest Always

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
            StructuredBuffer<uint> lightWatersColors;

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
            
            float4 GetColor(uint index) {
                if(index == 0) return float4(0,0,0,0);
                if(index == 1) return color1;
                if(index == 2) return color2;
                if(index == 3) return color3;
                return color4;
            }

             float3 GetDisplacement(uint index) {
                float2 uv1 = uvs[index];
                uv1 = clamp(uv1, 0, 1);

                float4 samp1 = WaveTexture.SampleLevel(sampler_WaveTexture, uv1, 0);

                float avgStep = dot(samp1.rgb, float3(0.2126, 0.7152, 0.0722));
                return Displacement * avgStep * -zAxis;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                uint index = IN.vertexID;

                float3 vertexPos = vertices[index];
                float3 displacement = GetDisplacement(index);

                // Get the world rotation.
                float3 right = vertexPos.x * xAxis;
                float3 up = vertexPos.y * yAxis;
                float3 forward = vertexPos.z * zAxis;
                
                OUT.pos = TransformObjectToHClip(right + up + forward + world + displacement);

                uint colorIndex = lightWatersColors[IN.vertexID / 3];

                OUT.uv = uvs[index];
                OUT.color = GetColor(colorIndex);

                OUT.bary = baryCoords[index];

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {

                if(IN.uv.y > 0.5f) {
                    float minBary = min(IN.bary.x, min(IN.bary.y, IN.bary.z));

                    float2 clampedUV = clamp(IN.uv, 0.0, 1.0);

                    float4 texColor = WaveTexture.SampleLevel(sampler_WaveTexture, clampedUV , 0);
                    float luminance = dot(texColor.rgb, float3(0.2126, 0.7152, 0.0722));
                    float value = step(.9, luminance);

                    if(value == 1.0) {
                        float4 baryColor = minBary < EdgeThreshold ? float4(EdgeColor.rgb, IN.color.a) : IN.color;// - float4(0.15,0.15,0.15,0);
                        baryColor.a = IN.color.a;
                        return baryColor;
                    } else {
                        return IN.color;
                    }
                }
                // Make the bottom half transparent.
                return float4(0,0,0,0);
            }

            ENDHLSL
        }
    }
}
