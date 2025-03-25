using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

public class Save3D : MonoBehaviour {

    const int threadGroupSize = 32;
    public ComputeShader slicer;

    public void Save (RenderTexture volumeTexture, string saveName) {
#if UNITY_EDITOR
        string sceneName = EditorSceneManager.GetActiveScene().name;
        saveName = sceneName + "_" + saveName;
        int resolution = volumeTexture.width;
        Texture2D[] slices = new Texture2D[resolution];

        slicer.SetInt ("resolution", resolution);
        slicer.SetTexture (0, "volumeTexture", volumeTexture);

        for (int layer = 0; layer < resolution; layer++) {
            new RenderTexture(resolution, resolution, 0)
            {
                dimension = TextureDimension.Tex2D,
                enableRandomWrite = true
            }.Create ();

            slicer.SetTexture (0, "slice", new RenderTexture(resolution, resolution, 0)
            {
                dimension = TextureDimension.Tex2D,
                enableRandomWrite = true
            });
            slicer.SetInt ("layer", layer);
            int numThreadGroups = Mathf.CeilToInt (resolution / (float) threadGroupSize);
            slicer.Dispatch (0, numThreadGroups, numThreadGroups, 1);

            slices[layer] = ConvertFromRenderTexture (new RenderTexture(resolution, resolution, 0)
            {
                dimension = TextureDimension.Tex2D,
                enableRandomWrite = true
            });
        }

        var x = Tex3DFromTex2DArray (slices, resolution);
        AssetDatabase.CreateAsset (x, "Assets/Resources/Textures/" + saveName + ".asset");
#endif
    }

    Texture3D Tex3DFromTex2DArray (Texture2D[] slices, int resolution) {
        Texture3D tex3D = new(resolution, resolution, resolution, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Trilinear
        };
        Color32[] outputPixels = tex3D.GetPixels32();

        for (int z = 0; z < resolution; z++) {
            Color c = slices[z].GetPixel (0, 0);
            Color[] layerPixels = slices[z].GetPixels ();
            for (int x = 0; x < resolution; x++)
                for (int y = 0; y < resolution; y++) {
                    outputPixels[x + resolution * (y + z * resolution)] = layerPixels[x + y * resolution];
                }
        }

        tex3D.SetPixels32(outputPixels);
        tex3D.Apply();
        return tex3D;
    }

    Texture2D ConvertFromRenderTexture (RenderTexture rt) {
        Texture2D output = new Texture2D (rt.width, rt.height);
        RenderTexture.active = rt;
        output.ReadPixels (new Rect (0, 0, rt.width, rt.height), 0, 0);
        output.Apply();
        return output;
    }
}