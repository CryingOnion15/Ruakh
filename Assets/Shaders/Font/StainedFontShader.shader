Shader "Custom/TMP/DistanceFieldExtended"
{
    Properties
    {
        _MainTex("Font Atlas", 2D) = "white" {}
        _FaceColor("Face Color", Color) = (1,1,1,1)
        _Smoothness("Edge Softness", Range(0,0.2)) = 0.02

        [Toggle] _UseOutline("Use Outline", Float) = 0
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness("Outline Thickness", Range(0,0.2)) = 0.05

        [Toggle] _UseFaceTexture("Use Face Texture", Float) = 0
        _FaceTexture("Face Texture", 2D) = "white" {}
        _MixColor("Mix Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            // TMP texture macros
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _FaceColor;
            float _Smoothness;

            float _UseOutline;
            float4 _OutlineColor;
            float _OutlineThickness;

            float _UseFaceTexture;
            TEXTURE2D(_FaceTexture);
            SAMPLER(sampler_FaceTexture);
            float4 _MixColor;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample the TMP SDF atlas
                half4 sdfTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half sdf = (sdfTex.a > 0) ? sdfTex.a : sdfTex.r;

                // Compute main alpha
                half alpha = smoothstep(0.5 - _Smoothness, 0.5 + _Smoothness, sdf);

                // Face color
                half4 faceColor;
                if (_UseFaceTexture != 0)
                {
                    half4 texCol = SAMPLE_TEXTURE2D(_FaceTexture, sampler_FaceTexture, IN.uv);
                    float theta = saturate(texCol.r);
                    faceColor = lerp(_FaceColor, _MixColor, theta) * IN.color;
                }
                else
                {
                    faceColor = _FaceColor * IN.color;
                }
                faceColor.a *= alpha;

                // Outline
                if (_UseOutline != 0)
                {
                    half outlineAlpha = step(0.5 - _OutlineThickness, sdf) - step(0.5 + _OutlineThickness, sdf);
                    outlineAlpha = saturate(outlineAlpha);

                    half4 outlineCol = _OutlineColor;
                    outlineCol.a *= outlineAlpha;

                    return outlineAlpha > 0 ? outlineCol : faceColor;
                }

                return faceColor;
            }
            ENDHLSL
        }
    }
}
