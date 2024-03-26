using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ControllerHelper<T> where T : Controller
{
  protected T controller;

  public ControllerHelper(T controller)
  {
    this.controller = controller;
  }

  public virtual void Update()
  {

  }

  public virtual void FixedUpdate()
  {

  }
  public virtual void LateUpdate()
  {

  }
}