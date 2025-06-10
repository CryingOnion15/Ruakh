Shader "Unlit/StainedBorderShader"
{
    Properties
    {
        _WireframeColor("Wireframe Color", color) = (1.0,1.0,1.0,1.0)
        _Thickness("Thickness", Range(0.01, 10)) = .1
    }
    // TODO BARY coords need to be baked into the mesh.
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geo

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct vert2geo
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            struct g2f {
                float4 pos : SV_POSITION;
                float3 barycentric : TEXCOORD0;
            };

            //sampler2D _MainTex;
            //float4 _MainTex_ST;
            float _Thickness;
            float4 _WireframeColor;

            vert2geo vert (appdata v)
            {
                vert2geo o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                //o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                //UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            // Geometry shader to define the barycentric coords.
            [maxvertexcount(3)]
            void geo(triangle vert2geo IN[3], inout TriangleStream<g2f> triStream) {
                g2f o;
                o.pos = IN[0].vertex;
                o.barycentric = float3(1.0, 0.0, 0.0);
                triStream.Append(o);
                o.pos = IN[1].vertex;
                o.barycentric = float3(0.0, 1.0, 0.0);
                triStream.Append(o);
                o.pos = IN[2].vertex;
                o.barycentric = float3(0.0, 0.0, 1.0);
                triStream.Append(o);
            }

           fixed4 frag(g2f i) : SV_Target
            {
                // Calculate the unit width based on triangle size.
                float3 width = fwidth(i.barycentric);
                
                // Alias the line a bit.
                float3 edgeDistance = i.barycentric / width;

                // Use the coordinate closest to the edge.
                float minEdge = min(edgeDistance.x, min(edgeDistance.y, edgeDistance.z));

                if(minEdge <= _Thickness) {
                    return fixed4(_WireframeColor.r, _WireframeColor.g, _WireframeColor.b, 1.0);
                } else {
                    return fixed4(_WireframeColor.r, _WireframeColor.g, _WireframeColor.b, 0.0);
                }
                // Set to our backwards facing wireframe colour.
                //return fixed4(_WireframeBackColour.r, _WireframeBackColour.g, _WireframeBackColour.b, alpha);
            }
            ENDCG
        }
    }
}
