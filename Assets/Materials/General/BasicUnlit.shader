Shader "Unlit/BasicUnlit"
{
    Properties
    {
        _BaseColor ("BaseColor", Color) = (1,1,1,1)
        _BaseMap ("BaseMap", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "LightMode"="UniversalForward" }
        ZWrite On
        ZTest LEqual
        Blend Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            //#include "UnityCG.cginc"
            #include "Assets/Materials/General/Includes/StainedOutline.hlsl"
            #include "Assets/Materials/General/Includes/StainedShadow.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1; 
                float4 posWS : TEXCOORD2;
                float cascadeIndex : TEXCOORD3;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.normalWS = TransformObjectToWorldNormal(v.normal);
                o.posWS = float4(TransformObjectToWorld(v.vertex), 1.0);
                o.cascadeIndex = GetCascadeIndex(o.posWS.xyz);
                o.uv = v.uv;
                return o;
            }

            // Struct to define our value at each pixel for our outline data.
            // Stained Glass Color Texture Pass use these value as the render targets.
            struct FragmentOutput
            {
                float4 color  : SV_Target0;
                float lightDepth : SV_Target1;
                //float depth : SV_Target1;
                //float3 normal   : SV_Target1;
                //float lum : SV_Target2;
            };

            FragmentOutput frag(v2f i)
            {
                FragmentOutput OUT;
                OUT.color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;

                OUT.lightDepth = GetLightSpaceDepthBlend(i.posWS.xyz, i.cascadeIndex);

                return OUT;
            }
            ENDHLSL
        }
    }
}
