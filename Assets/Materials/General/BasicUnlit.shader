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
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            //#include "UnityCG.cginc"
            #include "Assets/Materials/General/Includes/StainedOutline.hlsl"

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
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
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

            FragmentOutput frag (v2f i) : SV_Target
            {
                FragmentOutput OUT;

                float4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                OUT.color = baseTex * _BaseColor;

                float3 normalVS = mul((float3x3)UNITY_MATRIX_V, i.normalWS);
                OUT.normal = normalVS * 0.5 + 0.5;

                OUT.lum = OUT.color.r * 0.3 + OUT.color.g * 0.59 + OUT.color.b * 0.11;

                return OUT;
            }
            ENDHLSL
        }
    }
}
