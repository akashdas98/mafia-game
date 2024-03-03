using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ControllerHelper<T> where T : Controller
{
  protected T controller;
  protected Inventory inventory;

  protected Dictionary<string, float> inputs;

  public ControllerHelper(T controller, Inventory inventory)
  {
    this.controller = controller;
    this.inventory = inventory;
  }

  protected virtual void SetInputs()
  {
    inputs = controller.GetInputs();
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