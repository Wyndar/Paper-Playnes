using UnityEngine;

public class PaperPlaynesManager : MonoBehaviour
{

    public GameObject destBox;
    public GameObject puBox;
    public GameObject birds;
    public GameObject mines;
    public int spawnBoxCount;
    public Renderer spawnRenderer;

    void Start()
    {
        InstantiateBoxes();
        RearrangeBoxes();
    }

    private void InstantiateBoxes()
    {
        for (int i = 0; i < spawnBoxCount; i++)
        {
            Instantiate(puBox).transform.SetParent(transform);
            Instantiate(mines).transform.SetParent(transform);
            Instantiate(destBox).transform.SetParent(transform);
            GameObject bird = Instantiate(birds);
            bird.transform.SetParent(transform);
            bird.GetComponent<BirdAI>().flightArea = spawnRenderer;
        }
    }

    public void RearrangeBoxes()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).SetPositionAndRotation(GetRandomPositionWithinBounds(), transform.rotation);
            transform.GetChild(i).SetPositionAndRotation(new(transform.GetChild(i).position.x, 250f, transform.GetChild(i).position.z), transform.rotation);
        }
    }

    public Vector3 GetRandomPositionWithinBounds()
    {
        Bounds bounds = spawnRenderer.bounds;
        Vector3 boxSize = puBox.GetComponentInChildren<Renderer>().bounds.size;

        float x = Random.Range(bounds.min.x + boxSize.x / 2, bounds.max.x - boxSize.x / 2);
        float y = Random.Range(bounds.min.y + boxSize.y / 2, bounds.max.y - boxSize.y / 2);
        float z = Random.Range(bounds.min.z + boxSize.z / 2, bounds.max.z - boxSize.z / 2);

        return new Vector3(x, y, z);
    }
}
