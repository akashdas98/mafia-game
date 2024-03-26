using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInputHandler : InputHandler
{
  private float
    horizontalInput,
    verticalInput,
    actionInput,
    interact,
    drop,
    scroll,
    aim,
    mouseX,
    mouseY;

  private GunInputHandler gunInputHandler;
  private ItemsInputHandler itemsInputHandler;

  public override void SetInputs(InputData input)
  {
    horizontalInput = input.HorizontalAxis;
    verticalInput = input.VerticalAxis;
    actionInput = input.Action;
    interact = input.Interact;
    drop = input.Drop;
    scroll = input.Scroll;
    aim = input.Aim;
    mouseX = input.MouseX;
    mouseY = input.MouseY;
  }

  public override Dictionary<string, float> GetInputs()
  {
    return new Dictionary<string, float> {
      {"horizontalInput", horizontalInput},
      {"verticalInput", verticalInput},
      {"actionInput", actionInput},
      {"interact", interact},
      {"drop", drop},
      {"scroll", scroll},
      {"aim", aim},
      {"mouseX", mouseX},
      {"mouseY", mouseY}
    };
  }

  public override void ResetInputs()
  {
    horizontalInput = 0;
    verticalInput = 0;
    actionInput = 0;
    interact = 0;
    drop = 0;
    scroll = 0;
    aim = 0;
  }

  private void HandleMiscInputs()
  {
    CharacterController controller = (CharacterController)Refs.Controller;

    controller.MoveToward(Utilities.GetDirectionFromInput(horizontalInput, verticalInput));
    if (interact != 0)
    {
      controller.InteractWith();
    }
  }

  void Start()
  {
    gunInputHandler = new GunInputHandler(this);
    itemsInputHandler = new ItemsInputHandler(this);
  }

  void Update()
  {
    HandleMiscInputs();

    gunInputHandler.Update();
    itemsInputHandler.Update();
  }
}