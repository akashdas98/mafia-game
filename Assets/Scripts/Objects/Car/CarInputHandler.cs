using System;
using System.Collections.Generic;
using UnityEngine;

public class CarInputHandler : InputHandler
{
  private float horizontalInput, verticalInput, interact, brakeInput;
  public override void SetInputs(InputData input)
  {
    brakeInput = input.Brake > 0 ? 1 : 0;
    horizontalInput = input.HorizontalAxis;
    verticalInput = input.VerticalAxis;
    interact = input.Interact;
  }

  public override Dictionary<string, float> GetInputs()
  {
    return new Dictionary<string, float> {
      {"brakeInput", brakeInput},
      {"horizontalInput", horizontalInput},
      {"verticalInput", verticalInput},
      {"interact", interact},
    };
  }

  public override void ResetInputs()
  {
    brakeInput = 0;
    horizontalInput = 0;
    verticalInput = 0;
    interact = 0;
  }

  private bool HasInput()
  {
    return horizontalInput != 0 || verticalInput != 0;
  }

  private void ExitPlayerOnInput()
  {
    if (interact != 0 && TryGetPart(out CarController controller))
    {
      controller.Exit();
    }
  }

  void Update()
  {
    if (TryGetPart(out CarController controller))
    {
      controller.SetDirection(new Vector2(horizontalInput, verticalInput));
    }
    ExitPlayerOnInput();
  }

  void FixedUpdate()
  {
    if (TryGetPart(out CarController controller))
    {
      if (brakeInput == 0)
      {
        if (HasInput())
        {
          controller.Forward();
        }
        else
        {
          controller.GradualStop();
        }
      }
      else
      {
        if (controller.Moving())
        {
          controller.Brake();
        }
        else if (HasInput())
        {
          controller.Reverse();
        }
        else
        {
          controller.GradualStop();
        }
      }
    }
  }
}
