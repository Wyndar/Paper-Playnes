using UnityEngine;

public class CrashableTriggerColliderHandler : MonoBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
       if(!other.TryGetComponent(out HealthComponent healthComponent))
            return;
        healthComponent.ModifyHealth(HealthModificationType.Damage, 999);
        //999 is guaranteed death
    }
}
