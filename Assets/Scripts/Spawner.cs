using UnityEngine;
public class Spawner : MonoBehaviour
{
    public GameObject spawnBox;
    public BoxLocation boxLocation;
    public float spawnRange;
    public int spawnBoxCount;
    public Renderer spawnRenderer;
    public Vector3 boxLocationVector;

    void Start()
    {
        spawnRenderer = GetComponent<Renderer>();
        InstantiateBoxes();
        RearrangeBoxes();
    }

    private void InstantiateBoxes()
    {
        for (int i = 0; i < spawnBoxCount; i++)
            Instantiate(spawnBox).transform.SetParent(transform);
    }

    public void RearrangeBoxes()
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).SetPositionAndRotation(GetRandomPositionWithinBounds(), transform.rotation);
    }

    public Vector3 GetRandomPositionWithinBounds()
    {
        Bounds bounds = spawnRenderer.bounds;
        Vector3 boxSize = spawnBox.GetComponent<Renderer>().bounds.size;

        float x = Random.Range(bounds.min.x + boxSize.x / 2, bounds.max.x - boxSize.x / 2);
        float y = Random.Range(bounds.min.y + boxSize.y / 2, bounds.max.y - boxSize.y / 2);
        float z = Random.Range(bounds.min.z + boxSize.z / 2, bounds.max.z - boxSize.z / 2);

        return new Vector3(x, y, z);
    }
}
