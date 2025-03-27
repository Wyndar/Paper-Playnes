using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Destructible", menuName = "Scriptable/Destructible")]
public class Destructible : ScriptableObject
{
    public int maxHP;
    public GameObject destructionVFX;
    public AudioClip destructionSFX;
    public bool isController = false;
    public GameEvent respawnEvent;
    public void Die(GameObject gameObject, List<Controller> damageSources)
    {
        if (destructionVFX != null)
            _ = Instantiate(destructionVFX, gameObject.transform.position, gameObject.transform.rotation);
        if (destructionSFX != null)
            destructionVFX.GetComponent<AudioSource>().clip = destructionSFX; 
        if (isController)
            DestructibleNetworkManager.Instance.LocalPlayerDied(gameObject.GetComponent<Controller>(), damageSources);
        //else 
            //need new logic here to handle disabling pick up pool
    }
}
