Shader "Custom/LightParticleVertAndFrag"
{
    Properties{
        color1 ("Color 1", Color) = (1,1,1,1)
        color2 ("Color 2", Color) = (1,1,1,1)
        color3 ("Color 3", Color) = (1,1,1,1)
        color4 ("Color 4", Color) = (1,1,1,1)
        WaveTexture("Wave Texture", 2D) = "white" {}
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
            StructuredBuffer<float2> uvs;
            StructuredBuffer<int> indices;
            StructuredBuffer<int> colors;

            float4 color1;
            float4 color2;
            float4 color3;
            float4 color4;

            sampler2D WaveTexture;

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

                int index = indices[IN.vertexID];

                float3 vertexPos = vertices[index]; 
                OUT.pos = TransformObjectToHClip(vertexPos);

                int colorIndex = colors[IN.vertexID / 3];

                OUT.uv = uvs[index];
                OUT.color = GetColor(colorIndex);

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // IN.color;
                // + tex2D(WaveTexture, IN.uv)
                return IN.color;
            }

            ENDHLSL
        }
    }
}
