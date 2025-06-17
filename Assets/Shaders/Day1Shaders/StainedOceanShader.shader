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
            //StructuredBuffer<int> indices;
            StructuredBuffer<int> colors;

            float4 color1;
            float4 color2;
            float4 color3;
            float4 color4;
            float4 EdgeColor;

            sampler2D WaveTexture;

            float EdgeThreshold;

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

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                int index = IN.vertexID;

                float3 vertexPos = vertices[index]; 
                OUT.pos = TransformObjectToHClip(vertexPos);

                int colorIndex = colors[IN.vertexID / 3];

                OUT.uv = uvs[index];
                OUT.color = GetColor(colorIndex);

                OUT.bary = baryCoords[index];

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float minBary = min(IN.bary.x, min(IN.bary.y, IN.bary.z));

                float4 texColor = tex2D(WaveTexture, IN.uv);
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
