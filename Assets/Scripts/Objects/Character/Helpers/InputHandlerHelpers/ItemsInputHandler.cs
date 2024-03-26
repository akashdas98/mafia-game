using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemsInputHandler : CharacterInputHandlerHelper
{
  private ItemsController itemsController;

  public ItemsInputHandler(CharacterInputHandler inputHandler) : base(inputHandler) { }

  private void OnDropWeapon()
  {
    if (inputHandler.Refs.Controller is CharacterController controller)
    {
      ItemsController itemsController = controller.itemsController;
      if (inputs["drop"] != 0)
      {
        itemsController.DropEquippedWeapon();
      }
    }
  }

  private void OnScrollInput()
  {
    if (inputHandler.Refs.Controller is CharacterController controller)
    {
      ItemsController itemsController = controller.itemsController;
      if (inputs["scroll"] != 0)
      {
        itemsController.CycleWeapon(inputs["scroll"] > 0 ? -1 : 1);
      }
    }
  }

  public override void Update()
  {
    base.Update();

    OnScrollInput();
    OnDropWeapon();
  }

  public override void FixedUpdate()
  {

  }
}