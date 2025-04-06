using UnityEngine;
using UnityEngine.Rendering; // Required for AsyncGPUReadback
using Unity.Collections;    // Required for NativeArray
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class TextureReader : MonoBehaviour
{
    // Assign these in the Unity Inspector
    [Tooltip("The Material using the Mandelbrot shader.")]
    public Material mandelbrotMaterial;

    [Tooltip("The RenderTexture to draw the Mandelbrot set into and read back from.")]
    public RenderTexture sourceRenderTexture;

    private bool requestPending = false;
    Stopwatch requestStopwatch = new Stopwatch();


    // Optional: Control shader parameters from script
    [Header("Mandelbrot Parameters")]
    [Tooltip("Area of the complex plane: (Min Real, Min Imaginary, Width, Height)")]
    public Vector4 complexArea = new Vector4(-2.0f, -1.2f, 3.0f, 2.4f);
    [Tooltip("Maximum iterations for Mandelbrot calculation.")]
    public int maxIterations = 100;

    void Start()
    {
        // Ensure assets are assigned
        if (mandelbrotMaterial == null || sourceRenderTexture == null)
        {
            Debug.LogError("Please assign Mandelbrot Material and Source Render Texture in the Inspector!", this);
            this.enabled = false; // Disable script if setup is incorrect
            return;
        }

        // Optional: Generate the set once automatically when the game starts
        GenerateMandelbrot();
    }

    // Public method to trigger the generation process
    public void GenerateMandelbrot()
    {
        if (!isActiveAndEnabled) return; // Don't run if disabled due to missing assets

        Debug.Log("Generating Mandelbrot set...");

        // Update shader properties from script variables (optional)
        mandelbrotMaterial.SetVector("_Area", complexArea);
        mandelbrotMaterial.SetFloat("_MaxIter", maxIterations); // Use SetFloat as property is Float now

        // --- Render the Mandelbrot set into the RenderTexture ---
        // Graphics.Blit copies a source texture to a destination using a shader.
        // If the source is 'null', Blit draws a fullscreen quad.
        Graphics.Blit(null, sourceRenderTexture, mandelbrotMaterial);

        Debug.Log("Mandelbrot set rendered to RenderTexture.");

        // Optional: Automatically trigger readback right after generating
        // ReadTextureData();
    }

    // Public method to trigger the readback process
    public void ReadTextureData()
    {
        if (!isActiveAndEnabled) return; // Don't run if disabled

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
        }
    }

    // Callback executed when the GPU data is ready
    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (!isActiveAndEnabled) return; // Check again in case object was destroyed

        if (request.hasError)
        {
            Debug.LogError("GPU Readback error detected.");
            return;
        }

        if (request.done) // Double check it finished (usually true inside callback)
        {
            // Get the data. Using 'using' is good practice for safety, although GetData
            // often returns a view that might not strictly *require* disposal if only read immediately.
            using (NativeArray<Color32> buffer = request.GetData<Color32>())
            {
                Debug.Log($"GPU Readback successful. Received {buffer.Length} pixels.");

                // --- Access and Process the Pixel Data ---
                if (buffer.Length > 0)
                {
                    int width = request.width; // Use width/height from the request
                    int height = request.height;
                    int centerX = width / 2;
                    int centerY = height / 2;

                    // Calculate the linear index for the center pixel
                    // Remember texture data often starts at bottom-left or top-left depending on platform/settings
                    // Unity generally treats (0,0) as bottom-left for texture coordinates in shaders,
                    // but raw data access might be top-left row by row. Let's assume linear row-major from top-left for simplicity here.
                    // If colors look flipped, adjust the Y coordinate (e.g., height - 1 - centerY).
                    int centerIndex = centerY * width + centerX;

                    if (centerIndex >= 0 && centerIndex < buffer.Length)
                    {
                        Color32 centerPixelColor = buffer[centerIndex];
                        // Color32 stores RGBA values from 0-255
                        Debug.Log($"Center pixel ({centerX},{centerY}) color (RGBA): {centerPixelColor}");
                    }
                    else {
                        Debug.LogWarning("Center index out of bounds.");
                    }

                    // Example: Count black pixels (roughly inside the set)
                    int blackPixelCount = 0;
                    for(int i = 0; i < buffer.Length; ++i) {
                        // Checking against the exact ColorInside might be strict due to potential format conversions.
                        // A threshold might be better in practice, but let's check R, G, B for 0.
                        if (buffer[i].r == 0 && buffer[i].g == 0 && buffer[i].b == 0) {
                            blackPixelCount++;
                        }
                    }
                    Debug.Log($"Approximate number of black pixels (inside set): {blackPixelCount}");

                } else {
                    Debug.LogWarning("Received an empty buffer from GPU readback.");
                }
            } // NativeArray buffer might be disposed here by 'using' if necessary
        }
    }

    // --- Input Handling ---
    void Update()
    {
        // Press 'G' to re-generate the Mandelbrot set in the RenderTexture
        if (Input.GetKeyDown(KeyCode.G))
        {
            GenerateMandelbrot();
        }

        // Press 'Space' to read the current content of the RenderTexture back to CPU
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ReadTextureData();
        }
    }

    // Optional: Cleanup if RenderTexture was created dynamically
    // void OnDestroy() { if (sourceRenderTexture != null && sourceRenderTexture.IsCreated()) { sourceRenderTexture.Release(); } }
}