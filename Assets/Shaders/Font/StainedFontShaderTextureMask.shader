Shader "Custom/StainedFontShaderTextureMask"
{
    Properties
    {
        // Base Font Face Properties
        _MainTex("Font Atlas", 2D) = "white" {}
        _FaceColor("Face Color", Color) = (1,1,1,1)
        _Smoothness("Edge Softness", Range(0,0.2)) = 0.02

        // Outline Properties
        [Toggle] _UseOutline("Use Outline", Float) = 0
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness("Outline Thickness", Range(0,0.2)) = 0.05

        // Face Texture Properties
        [Toggle] _UseFaceTexture("Use Face Texture", Float) = 0
        _FaceTexture("Face Texture", 2D) = "white" {}
        _MixColor("Mix Color", Color) = (1,1,1,1)

        [Toggle] _UseTextureMask("Use Texture Mask", Float) = 0
        _TextureMask("Texture Mask", 2D) = "white" {}
        _MaskScale("Mask Scale", Float) = 1
        _TextureOffset("Texture Offset", Vector) = (0,0,0,0)
        _Rotation("Rotation", Float) = 0

        [Toggle] _UseCircleMask("Use Circle Mask", Float) = 0
        _CircleCenter("Circle Center", Vector) = (.5,.5,0,0)
        _CircleSize("Circle Size", Range(0,1)) = 0
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
                float4 positionOS : TEXCOORD1;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            // Font Face Variables and Macros
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _FaceColor;
            float _Smoothness;

            // Outline Variables
            float _UseOutline;
            float4 _OutlineColor;
            float _OutlineThickness;

            // Face Texture Variables
            float _UseFaceTexture;
            TEXTURE2D(_FaceTexture);
            SAMPLER(sampler_FaceTexture);
            float4 _MixColor;

            // Texture Mask Variables
            float _UseTextureMask;
            TEXTURE2D(_TextureMask);
            SAMPLER(sampler_TextureMask);
            float _MaskScale;
            float4 _TextureOffset;
            float _Rotation;
            float3 _BoundsMin;
            float3 _BoundsMax;

            // Circle Mash Variables
            float _UseCircleMask;
            float4 _CircleCenter;
            float _CircleSize;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS = IN.positionOS;
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // Sample the TMP SDF atlas
                half4 sdfTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half sdf = (sdfTex.a > 0) ? sdfTex.a : sdfTex.r;
                float2 boundsUV = float2(0,0);

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

               // Set Outline if using
                if (_UseOutline != 0)
                {
                    half outlineAlpha = step(0.5 - _OutlineThickness, sdf) - step(0.5 + _OutlineThickness, sdf);
                    outlineAlpha = saturate(outlineAlpha);

                    half4 outlineCol = _OutlineColor;
                    outlineCol.a *= outlineAlpha;

                    faceColor = outlineAlpha > 0 ? outlineCol : faceColor;
                }

                if(_UseTextureMask != 0 || _UseCircleMask != 0) {
                    boundsUV = (IN.positionOS.xy - _BoundsMin.xy) / (_BoundsMax.xy - _BoundsMin.xy);
                }

                if(_UseTextureMask != 0) {
                    float2 maskUV = boundsUV;
                    maskUV += _TextureOffset;

                    // Rotate the maskUV.
                    maskUV -= float2(.5,.5);
                    maskUV /= _MaskScale;

                    float c = cos(_Time.y * _Rotation);
                    float s = sin(_Time.y * _Rotation);

                    maskUV = float2(
                        maskUV.x * c - maskUV.y * s,
                        maskUV.x * s + maskUV.y * c
                    );

                    maskUV += float2(.5,.5);

                    float maskVal = SAMPLE_TEXTURE2D(_TextureMask, sampler_TextureMask, maskUV).a;

                    faceColor.a *= maskVal;
                }

                if(_UseCircleMask != 0) {
                    float2 circUV = boundsUV;
                    circUV -= _CircleCenter.xy;
                    float distSqr = dot(circUV, circUV);
                    float radius = _CircleSize * _CircleSize;

                    if(distSqr > radius) {
                        faceColor.a = 0;
                    }
                }

                return faceColor;
            }
            ENDHLSL
        }
    }
}
