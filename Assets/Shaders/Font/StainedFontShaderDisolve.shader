Shader "Custom/StainedFontShaderDisolve"
{
    Properties
    {
        // Base Font Face Properties
        _MainTex("Font Atlas", 2D) = "white" {}
        _FaceColor("Face Color", Color) = (1,1,1,1)
        _Smoothness("Edge Softness", Range(0,0.2)) = 0.02

        // Text Outline Properties
        [Toggle] _UseOutline("Use Outline", Float) = 0
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness("Outline Thickness", Range(0,0.2)) = 0.05

        // Face Texture Properties
        [Toggle] _UseFaceTexture("Use Face Texture", Float) = 0
        _FaceTexture("Face Texture", 2D) = "white" {}
        _MixColor("Mix Color", Color) = (1,1,1,1)

        // Dissolve properties
        [Toggle] _UseDissolve("Use Face Texture", Float) = 0
        _Dissolve("Dissolve", Range(0,1)) = 0
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _NoiseScale("Noise Scale", Float) = 5
        _NoiseOffset("Noise Offset", Vector) = (0, 0.1, 0, 0)
        _NoiseScroll("Noise Scroll", Vector) = (0, 0, 0, 0)
        _EdgeWidth("Edge Width", Range(.01,0.2)) = 0.05
        _GlowColor("Glow Color", Color) = (1,0.5,0,1)
        _GlowIntensity("Glow Intensity", Float) = 2
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

            // Base Font Face Variables
            float4 _FaceColor;
            float _Smoothness;

            // Outline variables
            float _UseOutline;
            float4 _OutlineColor;
            float _OutlineThickness;

            // Face Texture Variables
            float _UseFaceTexture;
            TEXTURE2D(_FaceTexture);
            SAMPLER(sampler_FaceTexture);
            float4 _MixColor;

            // Dissolve Variables
            float _UseDissolve;
            float _Dissolve;
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            float _NoiseScale;
            float4 _NoiseOffset;
            float4 _NoiseScroll;
            float _EdgeWidth;
            float4 _GlowColor;
            float _GlowIntensity;

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
                // Get Base Face Color
                half4 sdfTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half sdf = (sdfTex.a > 0) ? sdfTex.a : sdfTex.r;

                half alpha = smoothstep(0.5 - _Smoothness, 0.5 + _Smoothness, sdf);

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

                // Set Outline if using
                if (_UseOutline != 0)
                {
                    half outlineAlpha = step(0.5 - _OutlineThickness, sdf) - step(0.5 + _OutlineThickness, sdf);
                    outlineAlpha = saturate(outlineAlpha);

                    half4 outlineCol = _OutlineColor;
                    outlineCol.a *= outlineAlpha;

                    faceColor = outlineAlpha > 0 ? outlineCol : faceColor;
                }

                // Add Disolve
                if(_UseDissolve != 0) {
                    float2 screenUV = IN.positionHCS.xy / IN.positionHCS.w;
                    screenUV = screenUV * .5 + 0.5;
                    float2 noiseUV = screenUV * _NoiseScale + (_NoiseOffset.xy) + (_Time.y * _NoiseScroll.xy);
                    float noiseVal = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                    // mask: dissolve threshold
                    float dissolveMask = noiseVal - _Dissolve;

                    // if below threshold → transparent
                    if (dissolveMask < 0)
                    {
                        // edge glow (soft band)
                        float edge = 1 - saturate(dissolveMask / -_EdgeWidth);
                        float4 glow = _GlowColor * (edge * _GlowIntensity);
                        glow.a = edge * faceColor.a; // alpha for blending
                        return glow;
                    }
                }

                return faceColor;
            }
            ENDHLSL
        }
    }
}
