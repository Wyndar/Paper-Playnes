using UnityEditor;
using UnityEngine;

[CustomEditor (typeof (NoiseGenerator))]
public class NoiseGenEditor : Editor {

    NoiseGenerator noise;
    Editor noiseSettingsEditor;

    public override void OnInspectorGUI () {
        DrawDefaultInspector ();

        if (GUILayout.Button ("Update")) {
            noise.ManualUpdate ();
            EditorApplication.QueuePlayerLoopUpdate ();
        }

        if (GUILayout.Button("Save"))
            Save();
        if (GUILayout.Button("Load"))
            Load();
        if (noise.ActiveSettings != null)
            DrawSettingsEditor (noise.ActiveSettings, ref noise.showSettingsEditor, ref noiseSettingsEditor);
    }

    void Save () {
        FindFirstObjectByType<Save3D> ().Save (noise.shapeTexture, NoiseGenerator.CloudNoiseType.Shape.ToString());
        FindFirstObjectByType<Save3D> ().Save (noise.detailTexture, NoiseGenerator.CloudNoiseType.Shape.ToString());
    }

    void Load () {
        noise.Load (NoiseGenerator.CloudNoiseType.Shape, noise.shapeTexture);
        noise.Load (NoiseGenerator.CloudNoiseType.Detail, noise.detailTexture);
        EditorApplication.QueuePlayerLoopUpdate ();
    }

    void DrawSettingsEditor (Object settings, ref bool foldout, ref Editor editor) {
        if (settings != null) {
            foldout = EditorGUILayout.InspectorTitlebar (foldout, settings);
            using var check = new EditorGUI.ChangeCheckScope();
            if (foldout)
            {
                CreateCachedEditor(settings, null, ref editor);
                editor.OnInspectorGUI();
            }
            if (check.changed)
                noise.ActiveNoiseSettingsChanged();
        }
    }

    void OnEnable() => noise = (NoiseGenerator)target;

}