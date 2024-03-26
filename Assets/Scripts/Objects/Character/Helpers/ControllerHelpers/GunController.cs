using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : CharacterControllerHelper
{
  private Gun gun;
  private float gunDistance = 1f;
  private float gunHeight = 1.5f;
  private Target target;

  public GunController(CharacterController controller) : base(controller)
  {
    this.target = controller.Refs.AimTarget;
    target.Initialize(this);
  }

  private void SetGun()
  {
    Weapon equippedWeapon = controller.Refs.Inventory.GetEquippedWeapon();
    if (equippedWeapon is Gun equippedGun)
    {
      gun = equippedGun;
    }
    else
    {
      gun = null;
    }
  }

  public Gun GetGun()
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
    Vector2 offset = Utilities.FindPointAtDistance(GetGunInitPosition(), target.GetChosenPosition(), gunDistance);
    gun?.SetPosition(offset);
  }

  private void ResetGunPosition()
  {
    gun?.SetPosition(GetGunInitPosition());
  }

  public void PullTrigger()
  {
    gun?.PullTrigger();
  }

  public void ReleaseTrigger()
  {
    gun?.ReleaseTrigger();
  }

  private void Render()
  {
    //Debug renders
    if (gun is Gun g)
    {
      Vector2 gunPosition = g.GetPosition();
      Vector2 targetPosition = target.GetChosenPosition();
      Debug.DrawLine(gunPosition, targetPosition, Color.red, 0);
    }
  }

  public void Aim(Vector2? position)
  {
    if (gun != null)
    {
      if (position is Vector2 pos)
      {
        target.AimAt(pos);
        SetGunPosition();

        Render();
      }
      else
      {
        target.Reset();
        ResetGunPosition();
      }
    }
  }

  public override void Update()
  {
    base.Update();

    SetGun();
  }

  public override void FixedUpdate()
  {
  }
}