using UnityEngine;

public class PickUp : MonoBehaviour
{
    public string pickupName;
    public bool isActive = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerController player))
            Collect(player);
    }

    private void Collect(PlayerController player)
    {
        Debug.Log($"{player.name} collected {pickupName}");
        isActive = false;
        gameObject.SetActive(false);
    }
}
