Shader "CustomRenderPass/OverrideTextureData"
{
    Properties
    {
    }
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

            struct vertexAttributes
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (vertexAttributes v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.position);
                o.normalWS = TransformObjectToWorldNormal(v.normal);
                return o;
            }

            struct FragmentOutput
            {
                float4 color  : SV_Target0;
                float depth   : SV_Target1;
                float3 normal : SV_Target2;
            };

            FragmentOutput frag (v2f IN) : SV_Target
            {
                FragmentOutput Out;

                //Get the screen color.
                float2 uv = IN.positionCS.xy / IN.positionCS.w;
                uv = uv * 0.5 + 0.5;

                float4 color = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
                Out.color = color.r * 0.3 + color.g * 0.59 + color.b * 0.11;

                float rawDepth = SampleSceneDepth(uv) * 0.5 + 0.5;
                float linearDepth = _ProjectionParams.z / (rawDepth * _ProjectionParams.w - _ProjectionParams.y);
                Out.depth = linearDepth;

                Out.normal = IN.normalWS * 0.5 + 0.5;

                return Out;
            }
            ENDHLSL
        }
    }
}
