using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting : ControllerHelper<CharacterController>
{
  public Shooting(CharacterController controller, Inventory inventory) : base(controller, inventory) { }

  private Gun? gun;
  private float gunDistance = 1f;

  private void SetGun()
  {
    Weapon equippedWeapon = inventory.GetEquippedWeapon();
    gun = equippedWeapon as Gun;
  }

  private void SetGunPosition()
  {
    Dictionary<string, float> inputs = controller.GetInputs();
    int horizontal = inputs["horizontalInput"] > 0 ? 1 : inputs["horizontalInput"] < 0 ? -1 : 0;
    int vertical = inputs["verticalInput"] > 0 ? 1 : inputs["verticalInput"] < 0 ? -1 : 0;
    Vector3 offset = new Vector3(horizontal, vertical, 0).normalized * gunDistance;
    gun.SetPosition(offset); // relative position because weapon is a child object of character / owner
  }

  private void SetAimDirection()
  {
    Dictionary<string, float> inputs = controller.GetInputs();
    gun.SetAimDirection(Utilities.Get8DirectionFromInput(
      inputs["aimHorizontalInput"],
      inputs["aimVerticalInput"]
    ));
  }

  public Vector3 GetAimDirection()
  {
    if (gun)
    {
      return gun.GetAimDirection();
    }
    else
    {
      return new Vector3(0, 0);
    }
  }

  private void HandleGun()
  {
    bool isTriggerPulled = gun.IsTriggerPulled();
    if (gun.IsAiming())
    {
      bool hasActionInput = controller.HasActionInput();

      if (hasActionInput == isTriggerPulled)
      {
        return;
      }
      else
      {
        if (!isTriggerPulled)
        {
          gun.PullTrigger();
        }
        else
        {
          gun.ReleaseTrigger();
        }
      }
    }
    else
    {
      if (isTriggerPulled)
      {
        gun.ReleaseTrigger();
      }
    }
  }

  public override void Update()
  {
    SetGun();
    if (gun)
    {
      SetGunPosition();
      SetAimDirection();
      HandleGun();
    }
  }
}