using UnityEngine;

public class PickUp : MonoBehaviour
{
    public PickUpType pickupType;
    public bool isActive = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerController player))
            Collect(player);
    }

    private void Collect(PlayerController player)
    {
        Debug.Log($"{player.name} collected {pickupType}");
        isActive = false;
        gameObject.SetActive(false);
    }
}
