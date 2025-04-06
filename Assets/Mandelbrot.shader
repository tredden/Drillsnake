// Shader located at "Custom/Mandelbrot"
Shader "Custom/Mandelbrot"
{
    Properties
    {
        // _MainTex ("Texture", 2D) = "white" {} // We don't strictly need a texture input for Mandelbrot
        _Area("Area", Vector) = (-2, -1.2, 3, 2.4) // x=minReal, y=minImag, z=widthReal, w=heightImag
        _MaxIter ("Max Iterations", Float) = 100 // Use Float for smoother interpolation maybe
        _ColorInside ("Color Inside", Color) = (0,0,0,1) // Black for inside set
        _ColorOutsideA ("Color Outside A", Color) = (0,0,1,1) // e.g., Blue
        _ColorOutsideB ("Color Outside B", Color) = (1,1,1,1) // e.g., White
    }
    SubShader
    {
        // No culling or depth writes needed for a fullscreen effect
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc" // Includes necessary helper functions and variables

            struct appdata
            {
                float4 vertex : POSITION; // Vertex position in object space
                float2 uv : TEXCOORD0;     // UV coordinates
            };

            struct v2f // Vertex to Fragment structure
            {
                float2 uv : TEXCOORD0;     // Pass UVs to the fragment shader
                float4 vertex : SV_POSITION; // Clip space position (Required)
            };

            // Shader Properties
            float4 _Area;
            float _MaxIter;
            fixed4 _ColorInside;
            fixed4 _ColorOutsideA;
            fixed4 _ColorOutsideB;

            // Vertex Shader: Very simple, just transforms vertex position and passes UVs
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); // Standard vertex transformation
                o.uv = v.uv;                               // Pass UV coordinates through
                return o;
            }

            // Fragment Shader: Calculates Mandelbrot for the pixel
            fixed4 frag (v2f i) : SV_Target // SV_Target specifies this is the render target output
            {
                // Map UV coordinates [0,1]x[0,1] to the complex plane based on _Area
                float real = _Area.x + i.uv.x * _Area.z; // Map uv.x to real component
                float imag = _Area.y + i.uv.y * _Area.w; // Map uv.y to imaginary component

                float c_real = real; // Constant C real part
                float c_imag = imag; // Constant C imaginary part

                float z_real = 0.0; // Z starts at 0
                float z_imag = 0.0; // Z starts at 0

                int iter = 0;
                for(iter = 0; iter < (int)_MaxIter; iter++)
                {
                    // Z = Z^2 + C
                    // Z^2 = (z_real + i*z_imag)^2 = (z_real^2 - z_imag^2) + 2*z_real*z_imag*i
                    float zr_temp = z_real * z_real - z_imag * z_imag + c_real;
                    float zi_temp = 2.0 * z_real * z_imag + c_imag;

                    z_real = zr_temp;
                    z_imag = zi_temp;

                    // Check if escaped (magnitude squared > 4, equivalent to magnitude > 2)
                    if ((z_real * z_real + z_imag * z_imag) > 4.0)
                    {
                        break; // Escaped
                    }
                }

                // Color the pixel
                if (iter == (int)_MaxIter)
                {
                    return _ColorInside; // Point is likely inside the set
                }
                else
                {
                    // Point escaped, color based on iteration count (normalized)
                    float t = (float)iter / _MaxIter;
                    // Smooth interpolation between two colors
                    return lerp(_ColorOutsideA, _ColorOutsideB, t*t); // Using t*t for faster transition near set boundary
                }
            }
            ENDCG
        }
    }
}