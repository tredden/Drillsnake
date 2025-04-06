// Shader located at "Custom/GradientTest"
Shader "Custom/GradientTest"
{
    Properties
    {
        // Add a dummy texture property; sometimes needed for Blit compatibility, even if unused.
        _MainTex ("Dummy Texture", 2D) = "white" {}
    }
    SubShader
    {
        // Basic pass setup: no culling, depth testing/writing off.
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc" // Include standard Unity shader variables and functions

            // Input structure for the vertex shader
            struct appdata
            {
                float4 vertex : POSITION; // Vertex position
                float2 uv : TEXCOORD0;     // UV coordinate
            };

            // Structure passed from vertex to fragment shader
            struct v2f
            {
                float2 uv : TEXCOORD0;     // Pass UVs
                float4 vertex : SV_POSITION; // Clip space position (required output)
            };

            // Vertex Shader: Simple pass-through
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); // Transform vertex to clip space
                o.uv = v.uv;                               // Pass UVs directly
                return o;
            }

            // Fragment Shader: Output color based on UV coordinates
            fixed4 frag (v2f i) : SV_Target // SV_Target defines this as the output color
            {
                // Create a gradient:
                // - Red channel increases from left (0) to right (1) based on uv.x
                // - Green channel increases from bottom (0) to top (1) based on uv.y
                // - Blue channel is 0
                // - Alpha channel is 1 (fully opaque)
                fixed4 outputColor = fixed4(i.uv.x, i.uv.y, 0.0, 1.0);

                return outputColor;
            }
            ENDCG
        }
    }
}