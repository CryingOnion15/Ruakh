Shader "Custom/LightParticleVertAndFrag"
{
    Properties
    {
        _MainTex("texture", 2D) = "white" {}
        _Color("color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha One
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct LightParticle
            {
                float3 rotationMatRow1;
                float3 rotationMatRow2;
                float3 rotationMatRow3;
                float3 position;
                float3 startPosition;
                float3 velocity;
                float theta;
                int state;
            };

            // Buffer
            StructuredBuffer<LightParticle> particleBuffer;

            // Floats
            float particleSize;
            float3 cameraRight;
            float3 cameraUp;
            float3 cameraForward;

            // Properties
            sampler2D _MainTex;
            float4 _Color;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float4 color : COLOR;
            };

            static const float2 quadUVs[6] = {
                float2(0, 0),
                float2(1, 1),
                float2(1, 0),
                float2(0, 0),
                float2(0, 1),
                float2(1, 1)
            };

            static const float2 quadOffsets[6] = {
                float2(-1, -1),  // bottom left
                float2(1, 1),    // top right
                float2(1, -1),   // bottom right
                float2(-1, -1),  // bottom left
                float2(-1, 1),   // top left
                float2(1, 1)     // top right
                
            };            

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Get the particle index and vertex ID
                uint particleIndex = IN.vertexID / 6;
                uint vertexInQuad = IN.vertexID % 6;

                LightParticle p = particleBuffer[particleIndex];

                // The particle's world position
                float3 centerWS = p.position;

                // Quad offsets in UV space
                float2 offset = quadOffsets[vertexInQuad] * particleSize;

                // Calculate the rotation to make the quad face the camera
                float3 right = cameraRight * offset.x;
                float3 up = cameraUp * offset.y;
                float3 forward = cameraForward * 0.0f; // No need to offset along forward axis

                // Position of the quad vertices, adjusted to face the camera
                float3 offsetWS = centerWS + right + up + forward;

                OUT.pos = TransformObjectToHClip(float4(offsetWS, 1.0));
                OUT.uv = quadUVs[vertexInQuad];

                if(p.state == 0 || p.state == 5) {
                    _Color.a = 0;
                } else {
                    _Color.a = 255;
                }
                OUT.color = _Color;

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                return tex2D(_MainTex, IN.uv) * IN.color;
            }

            float4 TransformObjectToHClip(float4 posOS)
            {
                return mul(UNITY_MATRIX_MVP, posOS);
            }

            ENDHLSL
        }
    }
}
