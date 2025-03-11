using UnityEngine;

public class PickUpHandler : MonoBehaviour
{
    public void HandlePickUp(PickUpType type)
    {
        if (type == PickUpType.Undefined)
            throw new MissingReferenceException("Set a pickup type on the pickup object");

        if (GameEvent.TryGetEvent(type, out GameEvent gameEvent))
            gameEvent.RaiseEvent();
        else
            throw new MissingReferenceException($"Attempted to use a non-implemented pickup type: {type}");
    }
}

