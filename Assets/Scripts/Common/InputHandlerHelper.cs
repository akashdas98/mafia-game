using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InputHandlerHelper<T> where T : InputHandler
{
  protected T inputHandler;
  protected Dictionary<string, float> inputs;
  public InputHandlerHelper(T inputHandler)
  {
    this.inputHandler = inputHandler;
  }

  protected virtual void SetInputs()
  {
    inputs = inputHandler.GetInputs();
  }

  public virtual void Update()
  {
    SetInputs();
  }

  public virtual void FixedUpdate()
  {

  }
  public virtual void LateUpdate()
  {

  }
}