using UnityEngine;
using UnityEditor;

[ExecuteInEditMode] // Ensures script runs in Edit Mode
public class CloudManager : MonoBehaviour
{
    public GameObject cloudPrefab;
    public int cloudCount = 50;
    public Vector3 cloudAreaSize = new Vector3(100, 20, 100);
    public Vector3 cloudMovement = new Vector3(0.1f, 0, 0);
    private bool hasSpawned = false;

    void Start()
    {
        if (!Application.isPlaying && hasSpawned) return;
        SpawnClouds();
        hasSpawned = true;
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            transform.position += cloudMovement * Time.deltaTime;
        }
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            ClearClouds();
            SpawnClouds();
        }
    }

    public void RefreshClouds()
    {
        OnEnable();
    }

    void SpawnClouds()
    {
        hasSpawned = true;

        for (int i = 0; i < cloudCount; i++)
        {
            Vector3 position = new Vector3(
                Random.Range(-cloudAreaSize.x / 2, cloudAreaSize.x / 2),
                Random.Range(-cloudAreaSize.y / 2, cloudAreaSize.y / 2),
                Random.Range(-cloudAreaSize.z / 2, cloudAreaSize.z / 2)
            );

            Instantiate(cloudPrefab, transform.position + position, Quaternion.identity, transform);
        }
    }

    void ClearClouds()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        hasSpawned = false;
    }

    // Draw a bounding box in Scene View
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green; // Set bounding box color
        Gizmos.DrawWireCube(transform.position, cloudAreaSize);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CloudManager))]
public class CloudManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CloudManager cloudManager = (CloudManager)target;
        if (GUILayout.Button("Refresh Clouds"))
        {
            cloudManager.RefreshClouds();
        }
    }
}
#endif
