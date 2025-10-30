Shader "CustomRenderPass/OverrideTextureData"
{
    SubShader
    {
        Pass
        {
            Name "Override Data Pass"
            ZWrite On
            Cull Back
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseColor;

            struct vertexAttributes
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            v2f vert (vertexAttributes v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.position);
                o.normalWS = TransformObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                return o;
            }

            // Struct to define our value at each pixel for our outline data.
            // Stained Glass Color Texture Pass use these value as the render targets.
            struct FragmentOutput
            {
                float4 color  : SV_Target0;
                float3 normal   : SV_Target1;
                float lum : SV_Target2;
            };

            FragmentOutput frag (v2f IN) : SV_Target
            {
                FragmentOutput Out;

                //Get the screen uv.
                // float2 uv = IN.positionCS.xy * rcp(_ScreenParams.xy);
                // #if UNITY_UV_STARTS_AT_TOP
                // uv.y = 1.0 - uv.y;
                // #endif

                // Sample what the light can see.
                float4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                Out.color = color;
                Out.lum = color.r * 0.3 + color.g * 0.59 + color.b * 0.11;

                // Pack the normal into 0-1 for the texture.
                float3 normalVS = mul((float3x3)UNITY_MATRIX_V, IN.normalWS);
                Out.normal = normalVS * 0.5 + 0.5;

                // Return out 3 textures for data usage in the outline.
                return Out;
            }
            ENDHLSL
        }
    }
}
