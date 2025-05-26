Shader "Custom/LightParticleVertAndFrag"
{
    // Properties
    // {

    // }

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
            StructuredBuffer<float4> colors;

            struct Attributes
            {
                float3 position : POSITION;
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 pos : SV_POSITION;
                //float2 uv  : TEXCOORD0;
                float4 color : COLOR;
            };     

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // float3 vertexPos = vertices[IN.vertexID]; 
                OUT.pos = TransformObjectToHClip(IN.position);
                // Get the color for the vertex id.
                OUT.color = colors[IN.vertexID];

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
