Shader "Hidden/StainedShadowMaskOverride"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "LightMode"="UniversalForward"
        }

        Pass
        {
            Name "Shadow Mask Override Shader"

            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Materials/General/Includes/StainedShadow.hlsl"


            struct Attributes
            {
                float4 objectPosition : POSITION;
                float3 objectNormal : NORMAL;
            };

            struct Varyings
            {
                float4 clipPos : SV_POSITION;
                float3 worldPosition : TEXCOORD0;
                float3 worldNormal   : TEXCOORD1;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;

                o.worldPosition = TransformObjectToWorld(v.objectPosition.xyz);
                o.clipPos = TransformWorldToHClip(o.worldPosition);
                o.worldNormal = TransformObjectToWorldNormal(v.objectNormal);

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float shadow = SHADOW_TEST(i.worldPosition, i.worldNormal);
                float4 shadowColor = SampleStainedShadowColor(i.worldPosition);

                return shadowColor * shadow;
            }

            ENDHLSL
        }
    }
}
