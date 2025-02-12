using UnityEngine;
public enum BoxLocation { ForwardLeft, Forward, ForwardRight, Left, Centre, Right };
public class Spawner : MonoBehaviour
{
    public GameObject spawnBox;
    public BoxLocation boxLocation;
    public float spawnRange;
    public int boxCount;
    void Start() => Respawn();
    public void Respawn()
    {
        if(transform.childCount>0) 
            Despawn();
        while (boxCount > 0)
        {
            GameObject go = Instantiate(spawnBox, GetRandomPositionWithinBounds(gameObject), transform.rotation);
            go.transform.SetParent(transform);
            boxCount--;
        }
    }
    public Vector3 GetRandomPositionWithinBounds(GameObject obj)
    {
        if (obj.TryGetComponent<Renderer>(out var renderer))
        {
            Bounds bounds = renderer.bounds;
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            float z = Random.Range(bounds.min.z, bounds.max.z);
            return new Vector3(x, y, z);
        }
        return transform.position;
    }
    public void Despawn()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }
}
