using UnityEngine;

public class DestructibleComponent : MonoBehaviour
{
    public GameObject destructionVFX;
    public AudioClip destructionSFX;
    public bool shouldRespawn = false;
    public bool isDelayedRespawn = false;
    public GameObject respawnLoadScreen;
    public void Destroy()
    {
        if (destructionVFX == null)
            return;
        _ = Instantiate(destructionVFX, transform.position, transform.rotation);
        if (destructionSFX == null)
            return;
        destructionVFX.GetComponent<AudioSource>().clip = destructionSFX;
        if (shouldRespawn)
            Respawn();
        else
            Destroy(gameObject);
    }
    private void Respawn()
    {
        gameObject.SetActive(false);
        if (!isDelayedRespawn)
            return;
        respawnLoadScreen.SetActive(true);
    }
}
