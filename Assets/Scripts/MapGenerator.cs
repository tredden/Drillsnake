using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField]
    uint seed;

    [SerializeField]
    int pixelWidth;
    [SerializeField]
    int pixelHeight;

    [SerializeField]
    int chunkScale = 1024;

    Texture2D[,] chunks;

    [SerializeField]
    Texture2D activeTexture;
    [SerializeField]
    Vector2Int chunkCoord;
    [SerializeField]
    bool update = true;

    // Start is called before the first frame update
    void Start()
    {
        if (seed == 0) {
            GenerateSeed();
        }
        AllocateTextures();
        GenerateAllTextures();
    }

    #region GENERATION_LOGIC

    uint hash(uint s)
    {
        //hash from https://www.cs.ubc.ca/~rbridson/docs/schechter-sca08-turbulence.pdf
        // second magic number is floor(2^32/phi), idk what first magic number is
        s ^= 2747636419u;
        s *= 2654435769u;
        s ^= s >> 16;
        s *= 2654435769u;
        s ^= s >> 16;
        s *= 2654435769u;
        return s;
    }

    float vrand(Vector2 xy, uint s)
    {
        return (hash(hash(hash(s) ^ (uint)xy.x) ^ (uint)xy.y)) / 4294967295f;
    }
    Vector2 vrand22(Vector2 xy, uint s)
    {
        return new Vector2(vrand(xy, s), vrand(xy, hash(s)));
    }

    Vector2 voronoiNearest(Vector2 p, uint s)
    {
        float bestd = 1e10f;
        Vector2 best = p;
        Vector2 o = new Vector2(Mathf.Floor(p.x), Mathf.Floor(p.y));
        Vector2 pt = Vector2.zero;
        for (float x = -1f; x <= 1f; x++) {
            for (float y = -1f; y <= 1f; y++) {
                pt = o + new Vector2(x, y);
                pt += vrand22(pt, s);
                float d2 = (pt - p).SqrMagnitude();
                if (d2 < bestd) {
                    bestd = d2;
                    best = pt;
                }
            }
        }
        return best;
    }


    float valnoise(Vector2 p, uint s)
    {
        Vector2 c = p + (p.x + p.y) * 0.3660254038f * Vector2.one;
        float p1 = vrand(c, s);
        Vector2 f = new Vector2(c.x % 1f, c.y % 1f);
        Vector2 o = (f.x > f.y ? Vector2.up : Vector2.right);
        float p2 = vrand(c + o, s);
        float p3 = vrand(c + Vector2.one, s);

        Vector2 barypos = new Vector2(Mathf.Abs(f.x - f.y), (f.y*o.x + f.x*o.y));

        return Vector3.Dot(new Vector3(p1, p2, p3), new Vector3(1f - barypos.x - barypos.y, barypos.x, barypos.y));
    }

    float density(Vector2 p)
    {
        Vector2 rocks = voronoiNearest(p * .1f, 1u);
        float d = (rocks - p * .1f).magnitude;
        float r = vrand(rocks, 3u) * .2f;


        return -p.y + (d < r ? r - d : 0f) + valnoise(p * .1f, 5u) * 7f + valnoise(p * .01f, 6u) * 30f;
    }

    Vector2 cmult(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x - a.y * b.y, a.x*b.y + a.y*b.x); }

    // provides values for rock and dirt
    // >=.5 for rock
    float SampleDirtThickness(int x, int y)
    {
        Vector2 uv = new Vector2(x, -y);

        float dens = density(uv);

        if (dens < 0) {
            return 0f;
        } else {
            Vector2 vn = voronoiNearest(uv * .1f, 1u);
            float d = (vn - uv * .1f).magnitude;
            float r = vrand(vn, 3u) * .2f;
            return d > r ? .25f : .75f;
        }
    }

    // provides values for gold
    float SampleOreValue(int x, int y)
    {
        Vector2 uv = new Vector2(x, -y);
        float dens = density(uv);

        Vector2 c0 = new Vector2(-0.1009521484375f, -0.9563293457031254f);
        Vector2 c = c0 + (new Vector2(valnoise(uv * .005f, 10u), valnoise(uv * .005f, 11u)) * 2f- Vector2.one + (new Vector2(valnoise(uv * .02f, 12u), valnoise(uv * .02f, 13u)) - .5f*Vector2.one)) * .1f;
        Vector2 z = c;
        for (int i = 0; i < 13; i++) {
            z = cmult(z, z) + c;
        }

        float orevein = 1f/ (1f+ z.sqrMagnitude);

        orevein *= valnoise(uv * .05f, 9u);
        orevein *= valnoise(uv * .5f, 8u);

        Vector2 orechunk = voronoiNearest(uv * .02f, 16u) * 50f;
        float chunksize = vrand(orechunk, 17u);
        chunksize *= chunksize;
        chunksize *= chunksize;
        chunksize -= .5f;
        chunksize *= 400f* .5f * Mathf.Max(0f, 1f- 100f/ dens);

        if (chunksize > 0f) {
            orevein += Mathf.Sqrt(chunksize - Mathf.Max((orechunk - uv).sqrMagnitude - chunksize, 0f));
        }

        if (orevein > 100f / dens) {
            return 1f;
        } else {
            return 0f;
        }
    }

    // provides values for lava
    float SampleHazardValue(int x, int y)
    {
        return 0f;// x / (float)chunkScale;
    }

#endregion

    void GenerateChunkTexture(int x0, int y0, Texture2D texture)
    {
        Color32[] colors = new Color32[chunkScale * chunkScale];
        int i = 0;
        for (int yi = 0; yi < chunkScale; yi++) {
            int y = yi + y0;
            for (int xi = 0; xi < chunkScale; xi++) {
                int x = xi + x0;
                bool inBounds = x >= 0 && x < pixelWidth && y >= 0 && y < pixelHeight;
                colors[i].a = (byte)(inBounds ? 255 : 0);
                colors[i].r = (byte)(inBounds ? SampleHazardValue(x, y) * 255: 0);
                colors[i].g = (byte)(inBounds ? SampleOreValue(x, y) * 255 : 0);
                colors[i].b = (byte)(inBounds ? SampleDirtThickness(x, y) * 255: 0);
                i++;
            }
        }
        texture.SetPixels32(colors, 0);
        texture.Apply(false, false);
    }

    void GenerateSeed()
    {
        seed = (uint)Random.Range(1, 1000000);
    }

    void AllocateTextures()
    {
        int xChunks = (pixelWidth + (chunkScale - 1)) / chunkScale;
        int yChunks = (pixelHeight + (chunkScale - 1)) / chunkScale;
        chunks = new Texture2D[xChunks, yChunks];
        for (int xc = 0; xc < xChunks; xc++) {
            for (int yc = 0; yc < yChunks; yc++) {
                chunks[xc, yc] = new Texture2D(chunkScale, chunkScale, TextureFormat.RGBA32, false);
            }
        }
    }

    void GenerateAllTextures()
    {
        for (int xc = 0; xc < chunks.GetLength(0); xc++) {
            for (int yc = 0; yc < chunks.GetLength(1); yc++) {
                GenerateChunkTexture(xc*pixelWidth, yc*pixelHeight, chunks[xc, yc]);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (update) {
            activeTexture = chunks[chunkCoord.x, chunkCoord.y];
            SpriteRenderer render = GetComponent<SpriteRenderer>();
            render.sprite = Sprite.Create(activeTexture, new Rect(0, 0, chunkScale, chunkScale), Vector2.one * .5f, 1f);
            update = false;
        }
    }
}
