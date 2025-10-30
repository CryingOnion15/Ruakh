Shader "Unlit/BasicUnlit"
{

    SubShader
    {
        Pass
        {
            Name "Stained Glass Post Process"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            //#include "UnityCG.cginc"
            #include "Assets/Materials/General/Includes/StainedOutline.hlsl"

            float4 _EdgeColor;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float outlineTest = SCREEN_OUTLINE_TEST(i.vertex);
                //return float4(outlineTest,0,0,1);
                return lerp(_EdgeColor, float4(1,0,0,1), outlineTest);
            }
            ENDHLSL
        }
    }
}
