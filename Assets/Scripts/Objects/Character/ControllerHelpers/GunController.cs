using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : CharacterControllerHelper
{
  private Gun? gun;
  private float gunDistance = 1f;
  private float gunHeight = 1.5f;
  private Target target;

  public GunController(CharacterController controller, Inventory inventory, Target target) : base(controller, inventory)
  {
    this.target = target;
    target.Initialize(this);
  }

  private void SetGun()
  {
    Weapon equippedWeapon = inventory.GetEquippedWeapon();
    if (equippedWeapon is Gun equippedGun)
    {
      gun = equippedGun;
    }
    else
    {
      gun = null;
    }
  }

  public Gun? GetGun()
  {
    return gun;
  }

  public float GetGunDistance()
  {
    return gunDistance;
  }

  private Vector2 GetGunInitPosition()
  {
    return (Vector2)controller.transform.position + new Vector2(0, gunHeight);
  }

  public float GetGunHeight()
  {
    return gunHeight;
  }

  private void SetGunPosition()
  {
    if (IsAiming())
    {
      Vector2 offset = Utilities.FindPointAtDistance(GetGunInitPosition(), target.GetChosenPosition(), gunDistance);
      gun.SetPosition(offset);
    }
    else
    {
      gun.SetPosition(GetGunInitPosition());
    }
  }

  private bool IsAiming()
  {
    return inputs["aim"] > 0 ? true : false;
  }

  private void UpdateTarget()
  {
    if (IsAiming())
    {
      target.AimAt(Camera.main.ScreenToWorldPoint(new Vector2(inputs["mouseX"], inputs["mouseY"])));
    }
    else
    {
      target.Reset();
    }
  }

  private void HandleShooting()
  {
    bool isTriggerPulled = gun.IsTriggerPulled();
    if (IsAiming())
    {
      bool hasActionInput = inputs["actionInput"] > 0 ? true : false;

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

  private void Render()
  {
    //Debug renders
    if (gun && IsAiming())
    {
      Vector2 gunPosition = gun.GetPosition();
      Vector2 targetPosition = target.GetChosenPosition();
      Debug.DrawLine(gunPosition, targetPosition, Color.red, 0);
    }
  }

  private void HandleAiming()
  {
    UpdateTarget();
    SetGunPosition();
  }

  public override void Update()
  {
    base.Update();

    SetGun();

    if (gun != null)
    {
      HandleAiming();
      HandleShooting();
    }
  }

  public override void FixedUpdate()
  {
    Render();
  }
}