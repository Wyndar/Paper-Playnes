using UnityEngine;

public class DestructibleComponent : MonoBehaviour
{
    public GameObject destructionVFX;
    public AudioClip destructionSFX;
    public void Destroy()
    {
        if (destructionVFX == null)
            return;
        _ = Instantiate(destructionVFX, transform.position, transform.rotation);
        if (destructionSFX == null)
            return;
        destructionVFX.GetComponent<AudioSource>().clip = destructionSFX;
        Destroy(gameObject);
    }
}
