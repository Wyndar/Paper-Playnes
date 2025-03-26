using Unity.Netcode;
using UnityEngine;

public class CrashableTriggerColliderHandler : MonoBehaviour
{

    public void OnTriggerEnter(Collider other)
    {
        if (!NetworkManager.Singleton.IsServer || !other.TryGetComponent(out HealthComponent healthComponent))
            return;
        healthComponent.ModifyHealth(HealthModificationType.Damage, 999, null);
        //999 is guaranteed death
    }
}
