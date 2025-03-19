using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameOverManager))]
public class GameOverManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameOverManager gameOverManager = (GameOverManager)target;

        if (GUILayout.Button("Trigger Game Over"))
        {
            if (Application.isPlaying)
            {
                gameOverManager.TriggerGameOver();
                Debug.Log("Game Over triggered from Inspector!");
            }
            else
                Debug.LogWarning("Game Over can only be triggered in Play Mode.");
        }
    }
}
