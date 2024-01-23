using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ControllerHelper<T> where T : Controller
{
  protected T controller;
  protected Inventory inventory;

  public ControllerHelper(T controller, Inventory inventory)
  {
    this.controller = controller;
    this.inventory = inventory;
  }

  public abstract void Update();
}