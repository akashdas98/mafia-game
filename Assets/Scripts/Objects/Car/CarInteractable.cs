using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarInteractable : Interactable
{
  public override void Interact(GameObject other)
  {
    if (other.CompareTag("Character") && Refs.Controller is CarController carController)
    {
      carController.Enter(other);
    }
  }
}