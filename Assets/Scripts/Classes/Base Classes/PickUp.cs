using Unity.Netcode;
using UnityEngine;

public class PickUp : NetworkBehaviour
{
    public PickUpType pickupType;
    public bool isActive = true;
    public override void OnNetworkSpawn()
    {
        gameObject.name = pickupType.ToString();
        if (!IsServer) return;
        isActive = true;
        gameObject.SetActive(true);
    }
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
        player.HandlePickUp(pickupType, NetworkObject);
    }
}
