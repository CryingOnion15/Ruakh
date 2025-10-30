Shader "Unlit/StainedUnlitShadowReciever"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _SecondColor("Text Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Pass
        {
            Name "Opaque Pass"
            Tags { "LightMode"="UniversalForward" "RenderType"="Opaque" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/Materials/General/Includes/StainedShadow.hlsl"

            
            float4 _BaseColor;

            struct attributes
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 world : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (attributes v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.world = TransformObjectToWorld(v.pos).xyz;
                return o;
            }

            float4 frag (v2f IN) : SV_Target
            {
                //TODO ADD OUTLINE TO THIS SHADER.
                return _BaseColor;
            }
            ENDHLSL
        }
        // Pass
        // {
        //     Name "Add Shadow Color Pass"
        //     Tags { "LightMode"="StainedShadow" "RenderType"="Transparent" "Queue"="2900"}
        //     // ZWrite Off
        //     // ZTest LEqual
        //     // Blend SrcAlpha OneMinusSrcAlpha

        //     HLSLPROGRAM
        //     #pragma vertex vert
        //     #pragma fragment frag

        //     #include "Assets/Materials/General/Includes/StainedShadow.hlsl"

            
        //     float4 _BaseColor;
        //     float4 _SecondColor;

        //     struct attributes
        //     {
        //         float4 pos : POSITION;
        //         float2 uv : TEXCOORD0;
        //     };

        //     struct v2f
        //     {
        //         float4 pos : SV_POSITION;
        //         float2 uv : TEXCOORD0;
        //         float3 world : TEXCOORD1;
        //     };

        //     sampler2D _MainTex;
        //     float4 _MainTex_ST;

        //     v2f vert (attributes v)
        //     {
        //         v2f o;
        //         o.pos = TransformObjectToHClip(v.pos);
        //         o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        //         o.world = TransformObjectToWorld(v.pos).xyz;
        //         return o;
        //     }

        //     float4 frag (v2f IN) : SV_Target
        //     {

        //         //TODO ADD SHADOW OUTLINE GAPS to this shadow.

        //         //Convert the Position to Light Space Coordinates.
        //         float pixelLightDepth = GetLightSpaceDepth(IN.world);
        //         float3 shadowColor = SampleStainedShadowColor(IN.world);
        //         float shadowDepth = SampleStainedShadowDepth(IN.world);

        //         // Returns 1 if shadow color should be used.
        //         // If pixel depth is greater than the shadow map depth.
        //         float shadowTest = step(0, shadowDepth - pixelLightDepth);
        //         //return float4(lerp(_BaseColor.rgb, shadowColor, shadowTest), 1);
        //         return float4(_SecondColor.rgb, .5);
        //     }
        //     ENDHLSL
        // }
    }
}
