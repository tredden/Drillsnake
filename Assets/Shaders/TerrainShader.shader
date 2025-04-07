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
            float4 _ViewRect; // Using xy for panning
            int _Seed;
            float4 _OreThresholds;

            // Structure to pass data from vertex to fragment shader
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0; // Screen position for fragCoord calculation
                float2 uv : TEXCOORD1; // Original UVs (still available if needed)
            };

            // Vertex Shader (same as before)
            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.uv = v.texcoord;
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
                return float(hash(hash(hash(s) ^ uint(int(floor(xy.x)))) ^ uint(int(floor(xy.y))))) / 4294967295.0f;
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
            float simplex_kernel(float d2) {
                d2 = 0.5f - d2;
                return d2 < 0.0f ? 0.0f : d2 * d2 * d2 * 64.0f; 
            }

            float simplex_noise(float2 p, uint s) {
                const float F2 = 0.3660254038f; // (sqrt(3.0)-1.0)/2.0;
                const float G2 = 0.2113248654f; // (3.0-sqrt(3.0))/6.0;

                float2 c = p + (p.x + p.y) * F2;
                float2 fl = floor(c);
                float2 fr = c - fl; // Use c-fl for fractional part
                float2 o = (fr.x > fr.y) ? float2(1.0f, 0.0f) : float2(0.0f, 1.0f);

                // Get gradients for the three corners of the simplex cell
                float2 g1 = vrand22(fl, s) * 2.0f - 1.0f; // Centered gradients
                float2 g2 = vrand22(fl + o, s) * 2.0f - 1.0f;
                float2 g3 = vrand22(fl + float2(1.0f, 1.0f), s) * 2.0f - 1.0f;

                // Unskew the integer grid coordinates to find the simplex corner points in input space
                float2 p1 = fl - (fl.x + fl.y) * G2;
                float2 p2 = fl + o - (fl.x + fl.y + 1.0f) * G2;
                float2 p3 = fl + float2(1.0f, 1.0f) - (fl.x + fl.y + 2.0f) * G2;

                // Calculate vectors from input point p to each corner
                float2 d1 = p - p1;
                float2 d2 = p - p2;
                float2 d3 = p - p3;

                // Calculate contribution from each corner
                float n = 0.0f;
                n += simplex_kernel(dot2(d1)) * dot(d1, g1);
                n += simplex_kernel(dot2(d2)) * dot(d2, g2);
                n += simplex_kernel(dot2(d3)) * dot(d3, g3);

                return n; // Adjust scaling factor if needed
            }

            float dsimplex_kernel(float d2) {
                d2 = 0.5f - d2;
                // return d2 < 0.0f ? 0.0f : -d2 * d2 * 3.0f * 8.0f; // Original
                return d2 < 0.0f ? 0.0f : -d2 * d2 * 3.0f * 64.0f; // Matching kernel factor
            }

            float2 simplex_gradient(float2 p, uint s) {
                const float F2 = 0.3660254038f; // (sqrt(3.0)-1.0)/2.0;
                const float G2 = 0.2113248654f; // (3.0-sqrt(3.0))/6.0;

                float2 c = p + (p.x + p.y) * F2;
                float2 fl = floor(c);
                float2 fr = c - fl;
                float2 o = (fr.x > fr.y) ? float2(1.0f, 0.0f) : float2(0.0f, 1.0f);

                float2 g1 = vrand22(fl, s) -.5f;
                float2 g2 = vrand22(fl + o, s) -.5f;
                float2 g3 = vrand22(fl + float2(1.0f, 1.0f), s) *-.5f;

                float2 p1 = fl - (fl.x + fl.y) * G2;
                float2 p2 = fl + o - (fl.x + fl.y + 1.0f) * G2;
                float2 p3 = fl + float2(1.0f, 1.0f) - (fl.x + fl.y + 2.0f) * G2;

                float2 d1 = p - p1;
                float2 d2 = p - p2;
                float2 d3 = p - p3;

                float sk1 = simplex_kernel(dot2(d1));
                float sk2 = simplex_kernel(dot2(d2));
                float sk3 = simplex_kernel(dot2(d3));

                float dsk1 = dsimplex_kernel(dot2(d1));
                float dsk2 = dsimplex_kernel(dot2(d2));
                float dsk3 = dsimplex_kernel(dot2(d3));

                // Derivative calculation using chain rule: d/dx [k(d(x)^2) * dot(d(x), g)]
                // = d/dx [k(d(x)^2)] * dot(d(x), g) + k(d(x)^2) * d/dx [dot(d(x), g)]
                // = k'(d(x)^2) * 2 * d(x) * dot(d(x), g) + k(d(x)^2) * g  (since d/dx[d(x)] = Identity matrix/vector)

                // d/dx f(g(x)) = f'(g(x))g'(x)    
                return dsimplex_kernel(dot2(d1)) * 2. * d1 * dot(d1, g1) + simplex_kernel(dot2(d1)) * g1 +
                    dsimplex_kernel(dot2(d2)) * 2. * d2 * dot(d2, g2) + simplex_kernel(dot2(d2)) * g2 +
                    dsimplex_kernel(dot2(d3)) * 2. * d3 * dot(d3, g3) + simplex_kernel(dot2(d3)) * g3;
            }
            float density(float2 p) {
                 float2 rocks_cell = floor(p * 0.1f); float2 rocks_pos = voronoiNearest(p * 0.1f, 1u);
                 float d = length(rocks_pos - p * 0.1f); float r = vrand(rocks_cell, _Seed ^ 3u) * 0.2f;
                 float noise_detail = valnoise(p * 0.1f, _Seed ^ 5u) * 7.0f; float noise_broad = valnoise(p * 0.01f, _Seed ^ 6u) * 30.0f;
                 return p.y + (d < r ? (r - d) * 5.0f : 0.0f) + noise_detail + noise_broad;
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

            float3 tsample_gold(float t) {
                float3 dgold = float3(0, .9f, 0);
                float3 mgold = float3(0, 0.8f, 0);
                float3 lgold = float3(0, 0.7f, 0);
                return qbez(dgold, mgold, lgold, t);
            }
            float3 tsample_soil(float t) {
                float3 dsoil = float3(0, 0, .2f);
                float3 msoil = float3(0, 0, .15f);
                float3 lsoil = float3(0, 0, .1f);
                return qbez(dsoil, msoil, lsoil, t);
            }
            float3 tsample_rock(float t) {
                float3 drock = float3(0, 0, 1.0f);
                float3 mrock = float3(0, 0, .9f);
                float3 lrock = float3(0, 0, .8f);
                return qbez(drock, mrock, lrock, t);
            }
            float3 tsample_grass(float t) {
                float3 dgrass = float3(0, 0, 0.2f);
                float3 mgrass = float3(0, 0, 0.15f);
                float3 lgrass = float3(0, 0, 0.1f);
                return qbez(dgrass, mgrass, lgrass, t);
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
                float effectivePixelFactor = _PixelFactor;
                // Prevent division by zero or very small numbers if zoom is zero or negative
                effectivePixelFactor = max(effectivePixelFactor, 0.001f);
                // Apply offset and floor for pixelated grid effect
                float2 uv = floor(i.uv * _ViewRect.zw + _ViewRect.xy) / effectivePixelFactor;
                // *** END OF MODIFIED UV CALCULATION ***
                
                  // Select soil colors
                // float3 dsoil, msoil, lsoil;

                

                uv = float2(uv.x, -uv.y);

                // Calculate density
                float dens = density(uv);

                // Air Color
                if (dens < 0.0f) {
                    float3 airColorBase = float3(0.0, 0.0, 0.0);
                    // float3 airColor = exp(log(airColorBase) * -dens * 0.01f);
                    return fixed4(airColorBase, 0.5);
                }

                // Base terrain noise
                float t = vrand(uv, _Seed ^ 0u);

                // Top grass/soil layer
                 if (dens < 3.0f) {
                      float3 col = tsample_grass(t * t);
                      return fixed4(col, 0.75);
                 }

                 // Determine material type
                 uint mat = (dens < 8.0f) ? 1u : (dens < 32.0f) ? 2u : (dens < 64.0f) ? 3u : (dens < 128.0f) ? 4u : 5u;

                 // Voronoi noise influence
                 float2 vn_cell = floor(uv * 0.1f); float2 vn_pos = voronoiNearest(uv * 0.1f, 1u);
                 float d_voronoi = length(vn_pos - uv * 0.1f);
                 t = lerp(t, vrand(vn_cell, _Seed ^ mat), 0.35f);

                // Calculate rock radius and modify color if inside rock
                float r_rock = vrand(vn_cell, _Seed ^ 3u) * 0.2f;
                float3 col;
                 if (d_voronoi > r_rock) { col = tsample_soil(t); } // Soil
                 else { // Rock
                      float rock_t = 1.0f - (1.0f - t) * (1.0f - t);
                      rock_t += (r_rock - d_voronoi) * 2.0f;
                      if (vrand(vn_pos, _Seed ^ 4u) < .1) { // threshold for randomly making a rock into gold
                          col = tsample_gold(rock_t);
                      }
                      else {
                          col = tsample_rock(rock_t);
                      }
                 }

                 // --- Ore Vein Calculations ---                 
                 float2 c0 = float2(-0.1009521484375f, -0.9563293457031254f);
                 float2 c = c0 + (float2(valnoise(uv * .005, _Seed ^ 10u), valnoise(uv * .005, _Seed ^ 11u)) * 2. - 1. + (float2(valnoise(uv * .02, _Seed ^ 12u), valnoise(uv * .02, _Seed ^ 13u)) * 1. - .5)) * .1;
                 float2 z = c;
                 for(int k=0; k<13; ++k) z = cmult(z, z) + c; // 13 iterations
                 float orevein_julia = 1.0f / (1.0f + dot(z, z)); // Potential for high values if z near 0

                 // Fossil/Noise based ore vein
                 float orevein_fossil = ter_fossils(uv * 0.02f);
                 orevein_fossil *= valnoise(uv * 0.05f, _Seed ^ 9u);
                 orevein_fossil *= valnoise(uv * 0.5f, _Seed ^ 8u);

                 // Chunk based ore
                 float2 oreuv = (uv + sin(uv) + 3.2360679775f * sin(uv * 0.25f)); // Distorted UV for chunks
                 float2 orechunk_center = voronoiNearest(oreuv * 0.02f, _Seed ^ 16u); // Find chunk center
                 float chunksize_base = vrand(orechunk_center, _Seed ^ 17u);
                 chunksize_base *= chunksize_base; chunksize_base *= chunksize_base; // Amplify effect
                 chunksize_base -= 0.5f;
                 float chunk_density_factor = max(0.0f, 1.0f - 100.0f / max(dens, 1.0f)); // Avoid division by zero
                 float chunksize = chunksize_base * 400.0f * 0.5f * chunk_density_factor;

                 float orevein_chunk = 0.0f;
                 if (chunksize > 0.0f) {
                     orevein_chunk += sqrt(chunksize - max(dot2(orechunk_center - oreuv) - chunksize, 0.));
                 }

                 // Combine ore factors (adjust blending as needed)
                 // float orevein = orevein_julia * 0.1f + orevein_fossil + orevein_chunk; // Example combination
                 float orevein = 0;
                 orevein += max(orevein_julia, 0);
                 orevein += max(orevein_fossil, 0);
                 orevein += max(orevein_chunk, 0); // Simplified combination

                 // Apply ore color based on threshold and density
                 // Original thresholds seemed complex, simplifying here
                 if (orevein > 100.f / dens) col = tsample_gold(t); // Strong ore = gold
                 else if (orevein * valnoise(uv * .001, _Seed ^ 32u) > 10.f / dens) col = tsample_soil(t); // Weaker ore = blue/grey mineral

                // --- Simplex based ore layers ---
                 float2 puv = uv * 0.005f;
                 float2 perturb = mandelhash_smooth(puv, 2); // Use the mandelhash as perturbation
                 float pv = simplex_noise(perturb + puv, _Seed ^ 0u); // Simplex noise value at perturbed coordinate
                 float2 grad = simplex_gradient(perturb + puv, _Seed ^ 0u);
                 // Measure gradient magnitude and perturbation magnitude - original logic seemed complex
                 float pd = (1.0f - dot2(grad)) * dot2(perturb) / 3.0f; // Original pd calculation
                 // Let's use gradient magnitude as an indicator of detail/change
                 // float pd = length(grad); // Use gradient magnitude

                 float3 ore1 = tsample_gold(t);
                 float3 ore2 = tsample_gold(t);
                 float3 ore3 = tsample_gold(t);
                 float3 ore4 = tsample_gold(t);

                ////// Ore Layer 1 (Red)
                // float ore_sparseness = exp(uv.y * 0.0002f) * 10.0f;
                // float ore_center = exp(uv.y * 0.001f); // Center varies with depth
                // float ore_x = (pv - ore_center) * ore_sparseness; // How far from the center value
                // float ore = max(pd * 0.1f, 0.0f) * exp(-ore_x * ore_x); // Gaussian falloff around center, scaled by gradient magnitude
                // if (ore > .3f) col = ore1;

                ////// Ore Layer 2 (Orange)
                // ore_sparseness = exp(uv.y * 0.0002f) * 10.0f;
                // ore_center = exp(uv.y * 0.001f) * 2.0f; // Different depth center
                // ore_x = (pv - ore_center) * ore_sparseness;
                // ore = max(pd * 0.1f, 0.0f) * exp(-ore_x * ore_x);
                // if (ore > .3f) col = ore2;

                //// // Ore Layer 3 (Cyan/Teal)
                // ore_sparseness = exp(uv.y * 0.0002f) * 15.0f;
                // ore_center = sin(uv.y * 0.001f) * 3.0f; // Sinusoidal center variation
                // ore_x = (pv - ore_center) * ore_sparseness;
                // ore = max(pd * 0.1f, 0.0f) * exp(-ore_x * ore_x);
                // if (ore > .3f) col = ore3; // Blend cyan

                //// // Ore Layer 4 (Blue)
                // ore_sparseness = exp(uv.y * 0.0002f) * 40.0f; // More sparse
                // ore_center = vrand(floor(uv * 0.003f), _Seed ^ 3u) * 2.0f - 1.0f; // Random center using vrand
                // ore_x = (pv - ore_center) * ore_sparseness;
                // ore = max(pd * 0.1f, 0.0f) * exp(-ore_x * ore_x);
                // if (ore > .3f) col = ore3; // Blend blue


                //// Final output
                // return float4(col, 1.0f);

                // Final Output
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}