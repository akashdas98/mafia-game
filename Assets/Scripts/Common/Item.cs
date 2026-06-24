using UnityEngine;

public class Item : Interactable
{
  public override void Interact(GameObject other)
  {
    IItemPickupReceiver pickupReceiver = FindPickupReceiver(other);
    if (pickupReceiver != null)
    {
      PickUp(pickupReceiver);
    }
  }

  protected virtual void PickUp(IItemPickupReceiver pickupReceiver)
  {
    if (pickupReceiver != null)
    {
      pickupReceiver.PickUpItem(this);
    }
  }

  private IItemPickupReceiver FindPickupReceiver(GameObject other)
  {
    if (other == null)
    {
      return null;
    }

    MonoBehaviour[] behaviours = other.GetComponentsInParent<MonoBehaviour>(true);
    for (int i = 0; i < behaviours.Length; i++)
    {
      if (behaviours[i] is IItemPickupReceiver receiver)
      {
        return receiver;
      }
    }

    behaviours = other.GetComponentsInChildren<MonoBehaviour>(true);
    for (int i = 0; i < behaviours.Length; i++)
    {
      if (behaviours[i] is IItemPickupReceiver receiver)
      {
        return receiver;
      }
    }

    return null;
  }
}
