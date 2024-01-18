using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting : MonoBehaviour
{
  [SerializeField]
  private CharacterController controller;

  [SerializeField]
  private Gun gun;

  private float gunDistance = 1f;
  private Vector3 aiming, gunPosition;

  public void SetGunPosition()
  {
    Dictionary<string, float> inputs = controller.GetInputs();
    int horizontal = inputs["horizontalInput"] > 0 ? 1 : inputs["horizontalInput"] < 0 ? -1 : 0;
    int vertical = inputs["verticalInput"] > 0 ? 1 : inputs["verticalInput"] < 0 ? -1 : 0;
    Vector3 offset = new Vector3(horizontal, vertical, 0).normalized * gunDistance;
    gunPosition = transform.position + offset;
  }

  private void SetAiming()
  {
    Dictionary<string, float> inputs = controller.GetInputs();
    aiming = Utilities.Get8DirectionFromInput(
      inputs["aimHorizontalInput"],
      inputs["aimVerticalInput"]
    );
  }

  public bool IsAiming()
  {
    return aiming.x != 0 || aiming.y != 0;
  }

  public Vector3 GetAiming()
  {
    return aiming;
  }

  private void HandleGun()
  {
    bool isTriggerPulled = gun.IsTriggerPulled();
    if (IsAiming())
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

  void Update()
  {
    SetAiming();
  }
}