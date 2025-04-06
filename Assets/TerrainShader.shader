Shader "Custom/ShadertoyTerrainGenFixedView" // Renamed shader slightly
{
    Properties
    {
        _PixelFactor ("Pixel Scale Factor", Float) = 10.0 // Base scale for the grid
        _ViewOffset ("View Offset XY", Vector) = (0,0,0,0) // Manual pan control (X, Y)
        _Zoom ("Zoom Factor", Float) = 1.0 // Zoom multiplier (1 = default)
        // Add other properties if you want to control parameters from the Inspector
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0 // For uint support

            #include "UnityCG.cginc"

            // Uniforms from Properties block
            float _PixelFactor;
            float4 _ViewOffset; // Using xy for panning
            float _Zoom;

            // Structure to pass data from vertex to fragment shader
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0; // Screen position for fragCoord calculation
                // float2 uv : TEXCOORD1; // Original UVs (still available if needed)
            };

            // Vertex Shader (same as before)
            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                // o.uv = v.texcoord;
                return o;
            }

            // --- Helper Functions (Identical to previous version) ---

            uint hash(uint s) {
                s ^= 2747636419u; s *= 2654435769u;
                s ^= s >> 16;    s *= 2654435769u;
                s ^= s >> 16;    s *= 2654435769u;
                return s;
            }
            float vrand(float2 xy, uint s) {
                return float(hash(hash(hash(s) ^ uint(floor(xy.x))) ^ uint(floor(xy.y)))) / 4294967295.0f;
            }
            float3 fromColor(int c) {
                 uint uc = (uint)c;
                 uint r = (uc >> 16) & 255u; uint g = (uc >> 8) & 255u; uint b = uc & 255u;
                 return float3(r, g, b) / 255.0f;
            }
            float3 qbez(float3 a, float3 b, float3 c, float t) {
                return lerp(lerp(a, b, t), lerp(b, c, t), t);
            }
            float2 vrand22(float2 xy, uint s) {
                return float2(vrand(xy, s), vrand(xy, hash(s)));
            }
            float dot2(float2 a) { return dot(a, a); }
            float2 voronoiNearest(float2 p, uint s) {
                float bestd = 1e10f; float2 best = p; float2 o = floor(p);
                for (float x = -1.0f; x <= 1.0f; x++) { for (float y = -1.0f; y <= 1.0f; y++) {
                    float2 pt = o + float2(x, y); pt += vrand22(pt, s);
                    float d2 = dot2(pt - p);
                    if (d2 < bestd) { bestd = d2; best = pt; }
                }} return best;
            }
            float valnoise(float2 p, uint s) {
                 const float F2 = 0.3660254038; float2 skewed = p + dot(p, float2(F2, F2));
                 float2 cell = floor(skewed); float2 f = frac(skewed);
                 float2 o_st = (f.x > f.y ? float2(1.,0.) : float2(0.,1.));
                 float p1_st = vrand(cell,s); float p2_st = vrand(cell+o_st,s); float p3_st = vrand(cell+1.0f,s);
                 float2 barypos_st = float2(abs(f.x-f.y), dot(f.yx, o_st));
                 return dot(float3(p1_st, p2_st, p3_st), float3(1.0f - barypos_st.x - barypos_st.y, barypos_st.y, barypos_st.x));
            }
             float density(float2 p) {
                 float2 rocks_cell = floor(p * 0.1f); float2 rocks_pos = voronoiNearest(p * 0.1f, 1u);
                 float d = length(rocks_pos - p * 0.1f); float r = vrand(rocks_cell, 3u) * 0.2f;
                 float noise_detail = valnoise(p * 0.1f, 5u) * 7.0f; float noise_broad = valnoise(p * 0.01f, 6u) * 30.0f;
                 return -p.y + (d < r ? (r - d) * 5.0f : 0.0f) + noise_detail + noise_broad;
            }
            float2 cmult(float2 a, float2 b) { return float2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x); }
            float2 mandelhash_smooth(float2 c, int iters) {
                 float s = 2.0f; float2 z = sin(c / s) * s;
                 [unroll] for (int i = 0; i < iters; i++) {
                      float zx_temp = z.x;
                      z.x = sin((zx_temp * zx_temp - z.y * z.y + c.x) / s) * s;
                      z.y = sin((2.0f * zx_temp * z.y + c.y) / s) * s;
                 } return z;
            }
             float ter_fossils(float2 xy) {
                  float result = pow(mandelhash_smooth(xy * 0.05f, 5).y * 0.5f + 0.5f, 17.0f);
                  result *= (pow(mandelhash_smooth(xy * 0.3f, 5).y * 0.5f + 0.5f, 3.0f) +
                             pow(mandelhash_smooth(xy * 0.2121320344f, 5).y * 0.5f + 0.5f, 5.0f) +
                             pow(mandelhash_smooth(xy * 0.1732050808f, 5).y * 0.5f + 0.5f, 7.0f));
                   return result;
            }

            // --- Main Fragment Shader ---
            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate Pixel Coordinate from screenPos (like fragCoord)
                float2 screenCoord = i.screenPos.xy / i.screenPos.w;
                float2 pixelCoord = screenCoord * _ScreenParams.xy;

                // *** MODIFIED UV CALCULATION ***
                // Calculate UV with fixed offset and zoom instead of mouse
                // Apply zoom to the pixel scaling factor
                float effectivePixelFactor = _PixelFactor * _Zoom;
                // Prevent division by zero or very small numbers if zoom is zero or negative
                effectivePixelFactor = max(effectivePixelFactor, 0.001f);
                // Calculate base coordinate using zoom
                float2 baseCoord = pixelCoord / effectivePixelFactor;
                // Apply offset and floor for pixelated grid effect
                float2 uv = floor(baseCoord + _ViewOffset.xy);
                // *** END OF MODIFIED UV CALCULATION ***

                // Calculate density
                float dens = density(uv);

                // Air Color
                if (dens < 0.0f) {
                    float3 airColorBase = float3(0.0, 0.0, 0.0);
                    // float3 airColor = exp(log(airColorBase) * -dens * 0.01f);
                    return fixed4(airColorBase, 0.5);
                }

                // Base terrain noise
                float t = vrand(uv, 0u);

                // Top grass/soil layer
                 /*if (dens < 3.0f) {
                      float3 grass_dark = fromColor(0x213f00); float3 grass_med = fromColor(0x607d2d); float3 grass_light = fromColor(0x92b90f);
                      float3 col = qbez(grass_light, grass_med, grass_dark, t * t);
                      return fixed4(col, 1.0);
                 }*/

                 // Determine material type
                 uint mat = (dens < 8.0f) ? 1u : (dens < 32.0f) ? 2u : (dens < 64.0f) ? 3u : (dens < 128.0f) ? 4u : 5u;

                 // Voronoi noise influence
                 float2 vn_cell = floor(uv * 0.1f); float2 vn_pos = voronoiNearest(uv * 0.1f, 1u);
                 float d_voronoi = length(vn_pos - uv * 0.1f);
                 t = lerp(t, vrand(vn_cell, mat), 0.35f);

                // Select soil colors
                // float3 dsoil, msoil, lsoil;

                float3 dsoil = float3(0, 0, .1f);
                float3 msoil = float3(0, 0, .2f);
                float3 lsoil = float3(0, 0, .3f);

                float3 drock = float3(0, 0, 1.0f);
                float3 mrock = float3(0, 0, .9f);
                float3 lrock = float3(0, 0, .8f);


                // Calculate rock radius and modify color if inside rock
                float r_rock = vrand(vn_cell, 3u) * 0.2f;
                float3 col;
                 if (d_voronoi > r_rock) { col = qbez(dsoil, msoil, lsoil, t); } // Soil
                 else { // Rock
                      float rock_t = 1.0f - (1.0f - t) * (1.0f - t);
                      rock_t += (r_rock - d_voronoi) * 2.0f;
                      col = qbez(drock, mrock, lrock, saturate(rock_t));
                 }

                // --- Ore Vein Calculation (Identical logic to previous version) ---
                float3 darkgold = float3(0, .9f, 0); float3 mediumgold = float3(0, 0.6f, 0); float3 lightgold = float3(0, 0.3f, 0);
                float3 gold = qbez(darkgold, mediumgold, lightgold, t);
                float2 c0 = float2(-0.1009521484375f + sin(_Time.y) * 0.01f, -0.9563293457031254f + cos(_Time.y) * 0.01f);
                float2 noise_offset = (vrand22(uv * 0.005f, 10u) * 2.0f - 1.0f + (vrand22(uv * 0.02f, 12u) * 1.0f - 0.5f)) * 0.1f;
                float2 c = c0 + noise_offset; float2 z = c;
                 [unroll] for(int k=0; k<13; ++k) { z = cmult(z, z) + c; }
                float orevein_fossils = ter_fossils(uv * 0.02f);
                float orevein = orevein_fossils;
                orevein *= vrand(uv * 0.05f, 9u); orevein *= vrand(uv * 0.5f, 8u);
                float2 oreuv_distorted = (uv + sin(uv) + 3.2360679775f * sin(uv * 0.25f));
                float2 orechunk_cell = floor(oreuv_distorted * 0.02f); float2 orechunk_pos = voronoiNearest(oreuv_distorted * 0.02f, 16u);
                float chunksize_base = vrand(orechunk_cell, 17u);
                chunksize_base *= chunksize_base; chunksize_base *= chunksize_base;
                chunksize_base = (chunksize_base - 0.5f) * 200.0f * max(0.0f, 1.0f - 100.0f / dens);
                 if (chunksize_base > 0.0f) {
                      float dist_sq_to_chunk = dot2(orechunk_pos*50.0 - oreuv_distorted);
                      orevein += sqrt(max(0.0f, chunksize_base*chunksize_base - dist_sq_to_chunk));
                 }

                // Final color mixing based on ore presence
                float ore_threshold_gold = 100.0f / max(1.0f, dens);
                float ore_threshold_stone = 10.0f / max(1.0f, dens);
                if (orevein > ore_threshold_gold) { col = gold; }
                else if (orevein * vrand(uv * 0.001f, 32u) > ore_threshold_stone) { col = qbez(drock, mrock, lrock, t); }

                // Final Output
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}