using UnityEngine;
using UnityEngine.Rendering; // Required for AsyncGPUReadback
using Unity.Collections;    // Required for NativeArray
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public struct TerrainChunkQueueItem
{
    public int x;
    public int y;
    public int chunkScale;
    public Texture2D texture;
    public TerrainChunkQueueItem(int x, int y, int chunkScale, Texture2D texture)
    {
        this.x = x;
        this.y = y;
        this.chunkScale = chunkScale;
        this.texture = texture;
    }
}

public class TextureReader : MonoBehaviour
{
    // Assign these in the Unity Inspector
    [Tooltip("The Material using the terrain shader.")]
    public Material terrainMaterial;

    [Tooltip("The RenderTexture to draw the terrain set into and read back from.")]
    public RenderTexture sourceRenderTexture;

    private bool requestPending = false;
    Stopwatch requestStopwatch = new Stopwatch();

    //[SerializeField]
    //bool generating = false;


    // Optional: Control shader parameters from script
    [Header("Terrain Parameters")]
    public int pixelFactor = 8;
    public int zoom = 1;

    [SerializeField]
    float oreThreshold1 = 100f;
    [SerializeField]
    float oreThreshold2 = 50f;
    [SerializeField]
    float oreThreshold3 = 20f;
    [SerializeField]
    float oreThreshold4 = 10f;

    int activeX = 0;
    int activeY = 0;
    int activeChunkScale = 512;
    Texture2D activeTargetTexture = null;

    List<TerrainChunkQueueItem> chunkQueue = new List<TerrainChunkQueueItem>();

    public uint seed;

    static TextureReader instance;

    public static TextureReader GetInstance()
    {
        return instance;
    }

    void Start()
    {
        if (instance != null) {
            GameObject.DestroyImmediate(this.gameObject);
        } else {
            instance = this;
        }

        // Ensure assets are assigned
        if (terrainMaterial == null || sourceRenderTexture == null)
        {
            Debug.LogError("Please assign terrain Material and Source Render Texture in the Inspector!", this);
            this.enabled = false; // Disable script if setup is incorrect
            return;
        }

        //if (targetTexture == null) {
        //    targetTexture = new Texture2D((int)chunkScale, (int)chunkScale, TextureFormat.RGBA32, false);
        //}

        //// Optional: Generate the set once automatically when the game starts
        //GenerateTerrainChunk(xOffset, yOffset, chunkScale, textureHandle);
    }

    public void QueueTerrainChunk(int x, int y, int chunkScale, Texture2D handle)
    {
        // remove existing queued requests with this same texture handle to not cause weird trouble from pooling
        for (int i = 0; i < chunkQueue.Count; i++) {
            if (chunkQueue[i].texture == handle) {
                chunkQueue.RemoveAt(i);
                i--;
            }
        }
        Debug.Log("Queue is now " + chunkQueue.Count + " chunks deep with scale " + chunkScale);
        chunkQueue.Add(new TerrainChunkQueueItem(x, y, chunkScale, handle));
    }

    void GenerateTerrainChunk(int x, int y, int chunkScale, Texture2D textureHandle)
    {
        if (!isActiveAndEnabled) return; // Don't run if disabled due to missing assets
        requestPending = true;
        Debug.Log("Generating terrain chunk...");

        // Update shader properties from script variables (optional)
        terrainMaterial.SetFloat("_PixelFactor", pixelFactor);
        terrainMaterial.SetVector("_ViewRect", new Vector4(x, y, chunkScale, chunkScale));
        terrainMaterial.SetVector("_OreThreshold", new Vector4(oreThreshold1, oreThreshold2, oreThreshold3, oreThreshold4));
        terrainMaterial.SetInt("_Seed", (int)seed);

        // --- Render the Terrain chunk into the RenderTexture ---
        // Graphics.Blit copies a source texture to a destination using a shader.
        // If the source is 'null', Blit draws a fullscreen quad.
        Graphics.Blit(null, sourceRenderTexture, terrainMaterial);

        Debug.Log("Terrain chunk rendered to RenderTexture.");

        // Optional: Automatically trigger readback right after generating
        ReadTextureData(x, y, chunkScale, textureHandle);
    }

    // Public method to trigger the readback process
    public void ReadTextureData(int x, int y, int chunkScale, Texture2D textureHandle)
    {
        if (!isActiveAndEnabled) return; // Don't run if disabled

        if (chunkQueue.Count == 0) {
            return;
        }
        TerrainChunkQueueItem queueHead = chunkQueue[0];
        if (queueHead.x != x || queueHead.y != y || queueHead.chunkScale != chunkScale || queueHead.texture != textureHandle) {
            // don't copy the texture if the head request doesn't match anymore, something is fishy
            return;
        }
        activeX = x;
        activeY = y;
        activeChunkScale = chunkScale;
        activeTargetTexture = textureHandle;

        try
        {
            // Attempt to request the readback.
            // This call returns an AsyncGPUReadbackRequest object.
            // We don't need to store it here because the callback receives it.
            AsyncGPUReadback.Request(sourceRenderTexture, 0, TextureFormat.RGBA32, OnCompleteReadback);

            // Log that the request was *initiated*. Success/failure of the actual
            // GPU operation will be determined in the callback.
            Debug.Log("Requested GPU Readback...");
        }
        catch (System.Exception ex)
        {
            // Catch potential immediate exceptions during the Request call
            // (e.g., invalid arguments passed).
            Debug.LogError($"Failed to initiate AsyncGPUReadback request: {ex.Message}");
            // Optionally, re-throw or handle the exception further
            requestPending = false;
        }
    }

    // Callback executed when the GPU data is ready
    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (!isActiveAndEnabled) return; // Check again in case object was destroyed

        if (request.hasError)
        {
            Debug.LogError("GPU Readback error detected.");
            requestPending = false;
            return;
        }

        if (request.done) // Double check it finished (usually true inside callback)
        {
            TerrainChunkQueueItem queueHead = chunkQueue[0];
            if (queueHead.x != activeX || queueHead.y != activeY || queueHead.chunkScale != activeChunkScale || queueHead.texture != activeTargetTexture) {
                // don't copy the texture if the head request doesn't match anymore, something is fishy
                requestPending = false;
                return;
            }

            // Get the data. Using 'using' is good practice for safety, although GetData
            // often returns a view that might not strictly *require* disposal if only read immediately.
            using (NativeArray<Color32> buffer = request.GetData<Color32>())
            {
                Debug.Log($"GPU Readback successful. Received {buffer.Length} pixels.");

                // --- Access and Process the Pixel Data ---
                if (buffer.Length > 0)
                {
                    activeTargetTexture.SetPixels32(buffer.ToArray(), 0);
                    Debug.Log("Texture data copied to CPU storage, safe to reuse texture");
                    // send back to get carve history applied on top by CPU before render with sprite
                    MapGenerator.GetInstance().CarveOnFinishTexture(activeX, activeY, activeChunkScale, activeTargetTexture);
                    chunkQueue.RemoveAt(0);
                } else {
                    Debug.LogWarning("Received an empty buffer from GPU readback.");
                }
            } // NativeArray buffer might be disposed here by 'using' if necessary
            requestPending = false;
        }
    }

    // --- Input Handling ---
    void Update()
    {
        if (!requestPending && chunkQueue.Count > 0) {
            // xOffset += 128;
            GenerateTerrainChunk(chunkQueue[0].x, chunkQueue[0].y, chunkQueue[0].chunkScale, chunkQueue[0].texture);
        }
        //// Press 'G' to re-generate the Mandelbrot set in the RenderTexture
        //if (Input.GetKeyDown(KeyCode.G))
        //{
        //    GenerateTerrainChunk(xOffset, yOffset, chunkScale);
        //}

        //// Press 'Space' to read the current content of the RenderTexture back to CPU
        //if (Input.GetKeyDown(KeyCode.Space)) {
        //    generating = !generating;
        //}
    }

    // Optional: Cleanup if RenderTexture was created dynamically
    // void OnDestroy() { if (sourceRenderTexture != null && sourceRenderTexture.IsCreated()) { sourceRenderTexture.Release(); } }
}