Shader "Custom/LightParticleVertAndFrag"
{
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
            StructuredBuffer<int> indices;
            StructuredBuffer<float4> colors;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };     

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                int index = indices[IN.vertexID];

                float3 vertexPos = vertices[index]; 
                OUT.pos = TransformObjectToHClip(vertexPos);

                int triangleID = IN.vertexID / 3;
                OUT.color = colors[triangleID];

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                return IN.color;
            }

            ENDHLSL
        }
    }
}
