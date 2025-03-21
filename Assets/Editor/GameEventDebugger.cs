using UnityEngine;
using UnityEditor;
using System.Reflection;

[CustomEditor(typeof(GameEvent))]
public class GameEventDebugger : Editor
{
    private GameEvent gameEvent;
    private GameObject gameObjectParam;
    private bool toggle;
    private int intParam1;
    private int intParam2;
    private Team teamParam;
    private HealthComponent healthComponent;
    private HealthModificationType healthModificationType;

    private void OnEnable() => gameEvent = (GameEvent)target;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Game Event Debugger", EditorStyles.boldLabel);

        if (gameEvent == null)
        {
            EditorGUILayout.HelpBox("GameEvent reference is missing!", MessageType.Error);
            return;
        }

        // Dropdown for selecting event type
        GameEventType selectedEventType = gameEvent.selectedEventType;

        // Show relevant input fields based on selection
        switch (selectedEventType)
        {
            case GameEventType.NoParams:
                if (gameEvent.HasSubscribers() && GUILayout.Button("Raise Event (No Params)"))
                    gameEvent.RaiseEvent();
                break;
            case GameEventType.Toggle:
                toggle = EditorGUILayout.Toggle("Is On", toggle);
                if (gameEvent.HasToggleSubscribers() && GUILayout.Button("Raise Event (Toggle)"))
                        gameEvent.RaiseEvent(gameObjectParam);
                break;
            case GameEventType.GameObject:
                gameObjectParam = (GameObject)EditorGUILayout.ObjectField("GameObject Param", gameObjectParam, typeof(GameObject), true);
                if (gameEvent.HasGameObjectSubscribers() && GUILayout.Button("Raise Event (GameObject)"))
                    if (gameObjectParam == null)
                        Debug.LogWarning("Cannot raise event: GameObject parameter is null.");
                    else
                        gameEvent.RaiseEvent(gameObjectParam);
                break;

            case GameEventType.StatUpdate:
                intParam1 = EditorGUILayout.IntField("Stat Value 1", intParam1);
                intParam2 = EditorGUILayout.IntField("Stat Value 2", intParam2);
                if (gameEvent.HasStatSubscribers() && GUILayout.Button("Raise Event (Stat Update)"))
                    gameEvent.RaiseEvent(intParam1, intParam2);
                break;

            case GameEventType.TeamStatUpdate:
                teamParam = (Team)EditorGUILayout.EnumPopup("Team Param", teamParam);
                intParam1 = EditorGUILayout.IntField("Current Stat", intParam1);
                intParam2 = EditorGUILayout.IntField("Max/Previous Stat", intParam2);
                if (gameEvent.HasTeamSubscribers() && GUILayout.Button("Raise Event (Team Stat Update)"))
                    if (teamParam == Team.Undefined)
                        Debug.LogWarning("Cannot raise event: Team parameter is undefined.");
                    else
                        gameEvent.RaiseEvent(teamParam, intParam1, intParam2);
                break;

            case GameEventType.HealthModified:
                healthComponent = (HealthComponent)EditorGUILayout.ObjectField("healthComponent Component", healthComponent, typeof(HealthComponent), true);
                healthModificationType = (HealthModificationType)EditorGUILayout.EnumPopup("healthComponent Mod Type", healthModificationType);
                intParam1 = EditorGUILayout.IntField("Amount", intParam1);
                intParam2 = EditorGUILayout.IntField("Previous HP", intParam2);
                if (gameEvent.HasHealthModifiedSubscribers() && GUILayout.Button("Raise Event (healthComponent Modified)"))
                    if (healthComponent == null)
                        Debug.LogWarning("Cannot raise event: healthComponent Component parameter is null.");
                    else
                        gameEvent.RaiseEvent(healthComponent, healthModificationType, intParam1, intParam2);
                break;
        }
    }
}

