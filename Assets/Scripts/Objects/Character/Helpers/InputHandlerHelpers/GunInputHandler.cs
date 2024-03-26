using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunInputHandler : CharacterInputHandlerHelper
{
  public GunInputHandler(CharacterInputHandler inputHandler) : base(inputHandler) { }
  private void HandleShooting()
  {
    if (inputHandler.Refs.Controller is CharacterController controller)
    {
      GunController gunController = controller.gunController;
      bool hasActionInput = inputs["actionInput"] > 0 ? true : false;
      if (inputs["aim"] != 0)
      {
        if (hasActionInput)
        {
          gunController.PullTrigger();
        }
        else
        {
          gunController.ReleaseTrigger();
        }
      }
      else
      {
        gunController.ReleaseTrigger();
      }
    }
  }

  private void HandleAiming()
  {
    if (inputHandler.Refs.Controller is CharacterController controller)
    {
      GunController gunController = controller.gunController;
      Vector2? aimPosition = inputs["aim"] != 0 ?
      Camera.main.ScreenToWorldPoint(
        new Vector2(inputs["mouseX"], inputs["mouseY"])
      ) :
      null;
      gunController.Aim(aimPosition);
    }
  }

  public override void Update()
  {
    base.Update();

    HandleAiming();
    HandleShooting();
  }

  public override void FixedUpdate()
  {

  }
}