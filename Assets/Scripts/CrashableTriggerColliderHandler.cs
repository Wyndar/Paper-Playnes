using UnityEngine;

public class CrashableTriggerColliderHandler : MonoBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
       if(!other.TryGetComponent(out HealthComponent healthComponent))
            return;
        Debug.Log(gameObject.name);
        healthComponent.ModifyHealth(HealthModificationType.Damage, 999);
        //999 is guaranteed death
    }
}
