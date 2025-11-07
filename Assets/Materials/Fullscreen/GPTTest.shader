Shader "Hidden/URP6/FullscreenDepth_Raw"
{
    SubShader
    {
        Pass
        {
            Name "FullscreenDepthRaw"
            Tags { "RenderPipeline"="UniversalRenderPipeline" }

            ZWrite Off
            ZTest Always
            Cull Off
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            //TEXTURE2D_X(_CameraDepthTexture);
            //SAMPLER(sampler_CameraDepthTexture);

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(uint vid : SV_VertexID)
            {
                Varyings o;

                float2 uv = float2((vid << 1) & 2, vid & 2);

                o.uv = uv;
                o.position = float4(uv * 2 - 1, 0, 1);

                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;

                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1 - uv.y;
                #endif

                float d = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;

                return float4(d, d, d, 1);
            }

            ENDHLSL
        }
    }
}
