using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : Interactable
{
  public override void Interact(GameObject other)
  {
    Controller otherController = other.GetComponent<Controller>();
    if (otherController != null)
    {
      if (otherController is CharacterController characterController)
      {
        PickUp(characterController);
      }
    }
  }

  protected virtual void PickUp(CharacterController otherController)
  {
    if (otherController != null)
    {
      otherController.itemsController.PickUpItem(this);
    }
  }
}