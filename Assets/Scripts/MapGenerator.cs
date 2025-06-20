using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct CarveResults
{
    public bool didCarve;
    public float totalCarved;
    public float maxThickness;
    public float averageThickness;
    public float totalGold;
    public float totalHazard;
    public float totalThickness;
    public float averageNonzeroThickness;
    public bool hitEdge;

    public CarveResults(float startingThickness = 0f, float startingGold = 0f, float startingHazard = 0f)
    {
        didCarve = false;
        totalCarved = 0f;
        maxThickness = startingThickness;
        averageThickness = startingThickness;
        averageNonzeroThickness = startingThickness;
        totalGold = startingGold;
        totalHazard = startingHazard;
        totalThickness = startingThickness;
        hitEdge = false;
    }
}

public class MapGenerator : MonoBehaviour
{
    [SerializeField]
    GameObject cellPrefab;

    class SpriteCell
    {
        public SpriteRenderer render;
        public int cx = -1;
        public int cy = -1;

        public SpriteCell(SpriteRenderer render)
        {
            this.render = render;
        }

        public void AssignSprite(int cx, int cy, int chunkScale, Texture2D tex)
        {
            render.gameObject.transform.position = new Vector3(cx * chunkScale, cy * chunkScale, 0f);
            render.sprite = Sprite.Create(tex, new Rect(0, 0, chunkScale, chunkScale), Vector2.zero, 1f);
            this.cx = cx;
            this.cy = cy;
        }
    }

    static MapGenerator instance;

    [SerializeField]
    uint seed;

    [SerializeField]
    int pixelWidth;
    [SerializeField]
    int pixelHeight;

    [SerializeField]
    int chunkScale = 1024;
    [SerializeField]
    int worldScale = 16;
    [SerializeField]
    int yOffset;

    BitArray[,] carveChunks;
    Texture2D[,] chunks;
    [SerializeField]
    Stack<Texture2D> texturePool;
    SpriteCell[,] spriteChunks;
    // List<SpriteCell> spriteCells;

    [SerializeField]
    Texture2D activeTexture;
    [SerializeField]
    Vector2Int chunkCoord;
    [SerializeField]
    bool update = true;

    [SerializeField]
    bool useGPU = true;

    // Start is called before the first frame update
    void Start()
    {
        if (instance != null) {
            GameObject.DestroyImmediate(this);
            return;
        } 
        if (seed == 0) {
            GenerateSeed();
            TextureReader.GetInstance().seed = this.seed;
        }
        AllocateTextures();
        // GenerateSpriteCells();
        // GenerateAllTextures();
        if (instance != null) {
            GameObject.DestroyImmediate(this);
            return;
        } else {
            instance = this;
        }
    }

    public static MapGenerator GetInstance()
    {
        return instance;
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
    float SampleDirtThickness(Vector2 uv, float dens)
    {
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
    float SampleOreValue(Vector2 uv, float dens)
    {
        Vector2 c0 = new Vector2(-0.1009521484375f, -0.9563293457031254f);
        Vector2 c = c0 + (new Vector2(valnoise(uv * .005f, 10u), valnoise(uv * .005f, 11u)) * 2f - Vector2.one + (new Vector2(valnoise(uv * .02f, 12u), valnoise(uv * .02f, 13u)) - .5f*Vector2.one)) * .1f;
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
        chunksize *= 400f * .5f * Mathf.Max(0f, 1f- 100f/ dens);

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
    float SampleHazardValue(Vector2 uv, float dens)
    {
        return 0f;// x / (float)chunkScale;
    }

#endregion

    void AllocAndGenerateChunkTexture(int cx, int cy)
    {
        Texture2D texture;
        if (texturePool.Count > 0) {
            texture = texturePool.Pop();
        } else {
            texture = new Texture2D(chunkScale, chunkScale, TextureFormat.RGBA32, false);
        }
        BitArray carveHistory = carveChunks[cx, cy];
        GenerateChunkTexture(cx * chunkScale, cy * chunkScale + yOffset, texture, carveHistory);
        chunks[cx, cy] = texture;
        if (carveHistory == null) {
            carveHistory = new BitArray(chunkScale * chunkScale);
            carveChunks[cx, cy] = carveHistory;
        }
    }

    void GenerateChunkTexture(int x0, int y0, Texture2D texture, BitArray carveHistory)
    {
        if (useGPU) {
            TextureReader.GetInstance().QueueTerrainChunk(x0, y0, chunkScale, texture);
        } else {
            bool hasCarveHistory = carveHistory != null;
            Color[] colors = new Color[chunkScale * chunkScale];
            int i = 0;
            int x;
            int y;
            float r;
            float g;
            float b;
            Color c;
            Vector2 uv;
            bool inBounds;
            for (int yi = 0; yi < chunkScale; yi++) {
                y = yi + y0;
                for (int xi = 0; xi < chunkScale; xi++) {
                    x = xi + x0;
                    inBounds = x >= 0 && x < pixelWidth && y >= 0 + yOffset && y < pixelHeight + yOffset;
                    uv = new Vector2(x, -y) / worldScale;
                    float dens = density(uv);
                    if (hasCarveHistory && carveHistory[i]) {
                        r = 0;
                        g = 0;
                        b = 0;
                    } else {
                        r = SampleHazardValue(uv, dens);
                        g = SampleOreValue(uv, dens);
                        b = SampleDirtThickness(uv, dens);
                    }
                    c = new Color(r, g, b, 1f);
                    colors[i] = c;
                    i++;
                }
            }
            texture.SetPixels(colors, 0);
            texture.Apply(false, false);
        }
    }

    public void CarveOnFinishTexture(int x0, int y0, int chunkScale, Texture2D texture)
    {
        int cx = x0 / chunkScale;
        int cy = (y0 - yOffset) / chunkScale;

        if (cx < 0 || cx > chunks.GetLength(0) - 1 || cy < 0 || cy > chunks.GetLength(1)) {
            return;
        }

        BitArray carveHistory = this.carveChunks[cx, cy];
        bool hasCarveHistory = carveHistory != null;

        Color[] colors = texture.GetPixels();
        int i = 0;
        Color c;
        int y;
        int x;
        bool inBounds;
        for (int yi = 0; yi < chunkScale; yi++) {
            y = yi + y0;
            for (int xi = 0; xi < chunkScale; xi++) {
                x = xi + x0;
                inBounds = x >= 0 && x < pixelWidth && y >= 0 + yOffset && y < pixelHeight + yOffset;
                if (!inBounds) {
                    c = Color.clear;
                    colors[i] = c;
                } else if (hasCarveHistory && carveHistory[i]) {
                    c = colors[i];
                    c.r = 0;
                    c.g = 0;
                    c.b = 0;
                    colors[i] = c;
                } else {
                   // NOP, leave exiting data
                }
                i++;
            }
        }
        texture.SetPixels(colors, 0);
        texture.Apply(false, false);
        Debug.Log("Texture data sent back to GPU for rendering after carve");
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
        texturePool = new Stack<Texture2D>();
        spriteChunks = new SpriteCell[xChunks, yChunks];
        carveChunks = new BitArray[xChunks, yChunks];
        for (int xc = 0; xc < xChunks; xc++) {
            for (int yc = 0; yc < yChunks; yc++) {
                // Texture2D tex = new Texture2D(chunkScale, chunkScale, TextureFormat.RGBA32, false);
                // chunks[xc, yc] = tex;
                // textures.Add(tex);
                GameObject go = GameObject.Instantiate(cellPrefab);
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                SpriteCell sc = new SpriteCell(sr);
                // sc.AssignSprite(xc, yc, chunkScale, tex);
                spriteChunks[xc, yc] = sc;
            }
        }
    }

    void GenerateAllTextures()
    {
        //for (int xc = 0; xc < chunks.GetLength(0); xc++) {
        //    for (int yc = 0; yc < chunks.GetLength(1); yc++) {
        //        GenerateChunkTexture(xc*pixelWidth, yc*pixelHeight, chunks[xc, yc]);
        //    }
        //}
    }

    void GenerateSpriteCells()
    {
        // int numCells = (spriteChunks.GetLength(0) > 0 ? 2 : 1) * (spriteChunks.GetLength(1) > 0 ? 2 : 1);
        // this.spriteCells = new List<SpriteCell>();
        // for (int i = 0; i < numCells; i++) {
        //    GameObject go = GameObject.Instantiate(cellPrefab);
        //    SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        //    SpriteCell sc = new SpriteCell(sr);
        //    this.spriteCells.Add(sc);
        // }
    }

    public CarveResults CarveMap(int x0, int y0, int radius, Collider2D collider, float drillStrength)
    {
        CarveResults output = new CarveResults();
        List<Texture2D> updatedTextures = new List<Texture2D>();
        int totalCells = 0;
        int totalNonzeroCells = 0;
        Vector2 point = Vector2.zero;
        int xMin = Mathf.FloorToInt(collider.bounds.min.x);
        int xMax = Mathf.CeilToInt(collider.bounds.max.x);
        int yMin = Mathf.FloorToInt(collider.bounds.min.y);
        int yMax = Mathf.CeilToInt(collider.bounds.max.y);
        for (int xi = xMin; xi <= xMax; xi++) {
            int x = xi; // x0 + xi;
            int xc = x / chunkScale;
            int xr = x % chunkScale;
            for (int yi = yMin; yi < yMax; yi++) {
                int y = yi; // y0 + yi;
                //if (xi*xi + yi*yi > radius*radius) {
                //    continue;
                //}
                point.Set(x + .5f, y + .5f);
                if (!collider.OverlapPoint(point)) {
                    continue;
                }
                if (x < 0 || x >= pixelWidth || y < 0 || y >= pixelHeight) {
                    output.hitEdge = true;
                    continue;
                }
                int yc = y / chunkScale;
                int yr = y % chunkScale;
                Texture2D texC = chunks[xc, yc];
                BitArray carveHistory = carveChunks[xc, yc];
                if (texC == null || carveHistory == null) {
                    FixSpriteChunk(xc, yc);
                    texC = chunks[xc, yc];
                    carveHistory = carveChunks[xc, yc];
                }
                Color c = texC.GetPixel(xr, yr);
                totalCells++;
                if (c.b > 0) {
                    totalNonzeroCells++;
                }
                if (c.b != 0 || c.g != 0 && c.b <= drillStrength) {
                    output.didCarve = true;
                    output.totalCarved += 1f;
                    output.maxThickness = Mathf.Max(output.maxThickness, c.b);
                    output.totalThickness += c.b;
                    output.totalGold += c.g;
                    output.totalHazard += c.r;
                    c.b = Mathf.Max(0f, c.b - drillStrength); // rock
                    c.r = 0; // coal
                    c.g = 0; // gold
                    carveHistory[yr * chunkScale + xr] = true;
                    texC.SetPixel(xr, yr, c);
                    if (!updatedTextures.Contains(texC)) {
                        updatedTextures.Add(texC);
                    }
                }
            } // end for y
        } // end for x
        output.averageThickness = 0f;
        output.averageNonzeroThickness = 0f;
        if (totalCells > 0) {
			output.averageThickness = output.totalThickness / (float) totalCells;
			output.averageNonzeroThickness = output.totalThickness / (float) totalNonzeroCells;
        }
        foreach (Texture2D tex in updatedTextures) {
            tex.Apply(false, false);
        }
        return output;
    }

    public Vector3 GetTargetSpawnPos()
    {
        return new Vector3(pixelWidth / 2f, pixelHeight - chunkScale / 2f, 0f);
    }

    void FixSpriteChunk(int cx, int cy)
    {
        if (cx < 0 || cx > chunks.GetLength(0) - 1) {
            return;
        }
        if (cy < 0 || cy > chunks.GetLength(1) - 1) {
            return;
        }
        Texture2D tex = chunks[cx, cy];
        if (tex == null) {
            AllocAndGenerateChunkTexture(cx, cy);
        }
        if (spriteChunks[cx, cy].render.sprite == null || tex == null) {
            spriteChunks[cx, cy].AssignSprite(cx, cy, chunkScale, chunks[cx, cy]);
        }
    }

    void UnloadSpriteChunk(int cx, int cy)
    {
        if (cx < 0 || cx > chunks.GetLength(0) - 1) {
            return;
        }
        if (cy < 0 || cy > chunks.GetLength(1) - 1) {
            return;
        }
        Texture2D tex = chunks[cx, cy];
        if (tex != null) {
            texturePool.Push(tex);
            chunks[cx, cy] = null;
        }
        if (spriteChunks[cx, cy] != null && spriteChunks[cx, cy].render.sprite != null) {
            spriteChunks[cx, cy].render.sprite = null;
        }
    }

    public void UpdateCameraBounds(float minX, float maxX, float minY, float maxY)
    {
        int genBuffer = 1;
        int keepBuffer = 3;

        int cx0 = (int)Mathf.Clamp(minX / chunkScale - genBuffer, 0, chunks.GetLength(0) - 1);
        int cx1 = (int)Mathf.Clamp(maxX / chunkScale + genBuffer, 0, chunks.GetLength(0) - 1);
        int cy0 = (int)Mathf.Clamp(minY / chunkScale - genBuffer, 0, chunks.GetLength(1) - 1);
        int cy1 = (int)Mathf.Clamp(maxY / chunkScale + genBuffer, 0, chunks.GetLength(1) - 1);

        //Debug.Log("Cam Bounds -- cx0: " + cx0 + ", cx1: " + cx1 + ", cy0: " + cy0 + " cy1: " + cy1);

        for (int cx = 0; cx < chunks.GetLength(0); cx++) {
            bool xFreeable = cx < cx0 - keepBuffer || cx > cx1 + keepBuffer;
            for (int cy = 0; cy < chunks.GetLength(1); cy++) {
                bool yFreeable = cy < cy0 - keepBuffer || cy > cy1 + keepBuffer; 
                if (xFreeable || yFreeable) {
                    UnloadSpriteChunk(cx, cy);
                }
            }
        }

        for (int cx = cx0; cx <= cx1; cx++) {
            for (int cy = cy0; cy <= cy1; cy++) {
                FixSpriteChunk(cx, cy);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (update) {
            activeTexture = chunks[chunkCoord.x, chunkCoord.y];
            update = false;
        }
    }
}
