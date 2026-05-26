using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class InputHandler : Base
{
  public InputManager InputManager { get; private set; }

  public void AssignInputManager(InputManager inputManager)
  {
    InputManager = inputManager;
  }

  public abstract void SetInputs(InputData input);

  public abstract Dictionary<string, float> GetInputs();

  public abstract void ResetInputs();
}
