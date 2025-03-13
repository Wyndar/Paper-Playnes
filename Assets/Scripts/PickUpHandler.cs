using Unity.Netcode;

public class PickUpHandler : NetworkBehaviour
{
    public void HandlePickUp(PickUpType type, NetworkObjectReference pickupRef) => RequestPickUpServerRpc(type, pickupRef, NetworkManager.Singleton.LocalClientId);

    [ServerRpc]
    private void RequestPickUpServerRpc(PickUpType type, NetworkObjectReference pickupRef, ulong requestingClientId)
    {
        if (!pickupRef.TryGet(out NetworkObject pickupObject)) return;
        if (!pickupObject.TryGetComponent(out PickUp pickup)) return;
        if (!pickup.isActive) return;

        pickup.isActive = false;
        pickup.gameObject.SetActive(false);

        NotifyPlayerPickupClientRpc(type, requestingClientId);
        NotifyClientsPickupProcessedClientRpc(pickupRef);
    }

    [ClientRpc]
    private void NotifyPlayerPickupClientRpc(PickUpType type, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        if (GameEvent.TryGetEvent(type, out GameEvent gameEvent))
            gameEvent.RaiseEvent(); 
    }

    [ClientRpc]
    private void NotifyClientsPickupProcessedClientRpc(NetworkObjectReference pickupRef)
    {
        if (pickupRef.TryGet(out NetworkObject pickupObject))
            pickupObject.gameObject.SetActive(false);
    }
}

