using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System;

public class NoiseGenerator : MonoBehaviour
{

    const int computeThreadGroupSize = 8;

    public enum CloudNoiseType { Shape, Detail }
    public enum TextureChannel { R, G, B, A }

    [Header("Editor Settings")]
    public Texture3D fallbackShapeTexture;
    public Texture3D fallbackDetailsTexture;
    public CloudNoiseType activeTextureType;
    public TextureChannel activeChannel;
    public bool autoUpdate;
    public bool logComputeTime;

    [Header("Noise Settings")]
    public int shapeResolution = 132;
    public int detailResolution = 32;

    public WorleyNoiseSettings[] shapeSettings;
    public WorleyNoiseSettings[] detailSettings;
    public ComputeShader noiseCompute;
    public ComputeShader copy;

    [Header("Viewer Settings")]
    public bool viewerEnabled;
    public bool viewerGreyscale = true;
    public bool viewerShowAllChannels;
    [Range(0, 1)]
    public float viewerSliceDepth;
    [Range(1, 5)]
    public float viewerTileAmount = 1;
    [Range(0, 1)]
    public float viewerSize = 1;

    // Internal
    List<ComputeBuffer> buffersToRelease;
    bool updateNoise;

    [HideInInspector]
    public bool showSettingsEditor = true;
    [HideInInspector]
    public RenderTexture shapeTexture;
    [HideInInspector]
    public RenderTexture detailTexture;

    public void UpdateNoise()
    {
        ValidateParamaters();
        CreateTexture(ref shapeTexture, shapeResolution, CloudNoiseType.Shape);
        CreateTexture(ref detailTexture, detailResolution, CloudNoiseType.Detail);

        if (updateNoise && noiseCompute)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();

            updateNoise = false;
            WorleyNoiseSettings activeSettings = ActiveSettings;
            if (activeSettings == null)
                return;

            buffersToRelease = new List<ComputeBuffer>();

            int activeTextureResolution = ActiveTexture.width;

            // Set values:
            noiseCompute.SetFloat("persistence", activeSettings.persistence);
            noiseCompute.SetInt("resolution", activeTextureResolution);
            noiseCompute.SetVector("channelMask", ChannelMask);

            // Set noise gen kernel data:
            noiseCompute.SetTexture(0, "Result", ActiveTexture);
            var minMaxBuffer = CreateBuffer(new int[] { int.MaxValue, 0 }, sizeof(int), "minMax", 0);
            UpdateWorley(ActiveSettings);
            noiseCompute.SetTexture(0, "Result", ActiveTexture);
            //var noiseValuesBuffer = CreateBuffer (activeNoiseValues, sizeof (float) * 4, "values");

            // Dispatch noise gen kernel
            int numThreadGroups = Mathf.CeilToInt(activeTextureResolution / (float)computeThreadGroupSize);
            noiseCompute.Dispatch(0, numThreadGroups, numThreadGroups, numThreadGroups);

            // Set normalization kernel data:
            noiseCompute.SetBuffer(1, "minMax", minMaxBuffer);
            noiseCompute.SetTexture(1, "Result", ActiveTexture);
            // Dispatch normalization kernel
            noiseCompute.Dispatch(1, numThreadGroups, numThreadGroups, numThreadGroups);

            if (logComputeTime)
            {
                // Get minmax data just to force main thread to wait until compute shaders are finished.
                // This allows us to measure the execution time.
                var minMax = new int[2];
                minMaxBuffer.GetData(minMax);

                Debug.Log($"Noise Generation: {timer.ElapsedMilliseconds}ms");
            }
            foreach (ComputeBuffer buffer in buffersToRelease)
                buffer.Release();
        }
    }

    public void Load(CloudNoiseType type, RenderTexture target)
    {
        Texture3D savedTex = type == CloudNoiseType.Shape ? fallbackShapeTexture : fallbackDetailsTexture;
        if (savedTex != null && savedTex.width == target.width)
        {
            copy.SetTexture(0, "tex", savedTex);
            copy.SetTexture(0, "renderTex", target);
            int numThreadGroups = Mathf.CeilToInt(savedTex.width / 8f);
            copy.Dispatch(0, numThreadGroups, numThreadGroups, numThreadGroups);
        }
    }

    public RenderTexture ActiveTexture
    {
        get
        {
            return (activeTextureType == CloudNoiseType.Shape) ? shapeTexture : detailTexture;
        }
    }

    public WorleyNoiseSettings ActiveSettings
    {
        get
        {
            WorleyNoiseSettings[] settings = (activeTextureType == CloudNoiseType.Shape) ? shapeSettings : detailSettings;
            int activeChannelIndex = (int)activeChannel;
            if (activeChannelIndex >= settings.Length)
                return null;
            return settings[activeChannelIndex];
        }
    }

    public Vector4 ChannelMask
    {
        get
        {
            Vector4 channelWeight = new(
                (activeChannel == TextureChannel.R) ? 1 : 0,
                (activeChannel == TextureChannel.G) ? 1 : 0,
                (activeChannel == TextureChannel.B) ? 1 : 0,
                (activeChannel == TextureChannel.A) ? 1 : 0
            );
            return channelWeight;
        }
    }

    void UpdateWorley(WorleyNoiseSettings settings)
    {
        var prng = new System.Random(settings.seed);
        CreateWorleyPointsBuffer(prng, settings.numDivisionsA, "pointsA");
        CreateWorleyPointsBuffer(prng, settings.numDivisionsB, "pointsB");
        CreateWorleyPointsBuffer(prng, settings.numDivisionsC, "pointsC");

        noiseCompute.SetInt("numCellsA", settings.numDivisionsA);
        noiseCompute.SetInt("numCellsB", settings.numDivisionsB);
        noiseCompute.SetInt("numCellsC", settings.numDivisionsC);
        noiseCompute.SetBool("invertNoise", settings.invert);
        noiseCompute.SetInt("tile", settings.tile);
    }

    void CreateWorleyPointsBuffer(System.Random prng, int numCellsPerAxis, string bufferName)
    {
        var points = new Vector3[numCellsPerAxis * numCellsPerAxis * numCellsPerAxis];
        float cellSize = 1f / numCellsPerAxis;

        for (int x = 0; x < numCellsPerAxis; x++)
            for (int y = 0; y < numCellsPerAxis; y++)
                for (int z = 0; z < numCellsPerAxis; z++)
                {
                    float randomX = (float)prng.NextDouble();
                    float randomY = (float)prng.NextDouble();
                    float randomZ = (float)prng.NextDouble();
                    Vector3 randomOffset = new Vector3(randomX, randomY, randomZ) * cellSize;
                    Vector3 cellCorner = new Vector3(x, y, z) * cellSize;

                    int index = x + numCellsPerAxis * (y + z * numCellsPerAxis);
                    points[index] = cellCorner + randomOffset;
                }

        CreateBuffer(points, sizeof(float) * 3, bufferName);
    }

    // Create buffer with some data, and set in shader. Also add to list of buffers to be released
    ComputeBuffer CreateBuffer(Array data, int stride, string bufferName, int kernel = 0)
    {
        ComputeBuffer buffer = new(data.Length, stride, ComputeBufferType.Structured);
        buffersToRelease.Add(buffer);
        buffer.SetData(data);
        noiseCompute.SetBuffer(kernel, bufferName, buffer);
        return buffer;
    }

    void CreateTexture(ref RenderTexture texture, int resolution, CloudNoiseType type)
    {
        if (texture == null || !texture.IsCreated() || texture.width != resolution 
            || texture.height != resolution || texture.volumeDepth != resolution 
            || texture.graphicsFormat != GraphicsFormat.R16G16B16A16_UNorm)
        {
            if (texture != null)
                texture.Release();
            texture = new RenderTexture(resolution, resolution, 0)
            {
                graphicsFormat = GraphicsFormat.R16G16B16A16_UNorm,
                volumeDepth = resolution,
                enableRandomWrite = true,
                dimension = TextureDimension.Tex3D,
                name = type.ToString()
            };
            texture.Create();
            Load(type, texture);
        }
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
    }

    public void ManualUpdate()
    {
        updateNoise = true;
        UpdateNoise();
    }

    public void ActiveNoiseSettingsChanged()
    {
        if (autoUpdate)
            updateNoise = true;
    }

    void ValidateParamaters()
    {
        detailResolution = Mathf.Max(1, detailResolution);
        shapeResolution = Mathf.Max(1, shapeResolution);
    }
}
