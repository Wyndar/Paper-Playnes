using UnityEngine;

public class PickUp : MonoBehaviour
{
    public PickUpType pickupType;
    public bool isActive = true;

    public void OnEnable() => gameObject.name = pickupType.ToString();

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PickUpHandler player))
            Collect(player);
    }

    private void Collect(PickUpHandler player)
    {
        Debug.Log($"{player.name} collected {pickupType}");
        isActive = false;
        gameObject.SetActive(false);
        player.HandlePickUp(pickupType);
    }
}
