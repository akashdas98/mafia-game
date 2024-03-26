using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class InputHandler : Base
{
  public abstract void SetInputs(InputData input);

  public abstract Dictionary<string, float> GetInputs();

  public abstract void ResetInputs();
}