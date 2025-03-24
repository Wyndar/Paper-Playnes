using UnityEngine;

[CreateAssetMenu(fileName = "Destructible", menuName = "Scriptable/Destructible")]
public class Destructible : ScriptableObject
{
    public int maxHP;
    public GameObject destructionVFX;
    public AudioClip destructionSFX;
    public bool canRespawn = false;
    public bool isDelayedRespawn = false;
    public GameEvent respawnEvent;
    public void Die(GameObject gameObject)
    {
        if (destructionVFX != null)
            _ = Instantiate(destructionVFX, gameObject.transform.position, gameObject.transform.rotation);
        if (destructionSFX != null)
            destructionVFX.GetComponent<AudioSource>().clip = destructionSFX;
        if (canRespawn)
            WaitForRespawn(gameObject);
        else
            Destroy(gameObject);
    }
    private void WaitForRespawn(GameObject gameObject)
    {
        respawnEvent.RaiseEvent(gameObject);
        if (isDelayedRespawn)
            TeamManager.Instance.LocalPlayerDied(gameObject.GetComponent<Controller>());
    }
}
