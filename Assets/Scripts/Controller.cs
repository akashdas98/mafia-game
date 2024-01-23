using System;
using UnityEngine;

public abstract class Controller : MonoBehaviour
{
  protected ControlManager controlManager;

  [SerializeField]
  protected Animator animator;

  public abstract void SetInputs(InputData input);

  public abstract void ResetInputs();

  public abstract void Interact(GameObject other, ControlManager cm);

  public void AssignControlManager(ControlManager cm)
  {
    controlManager = cm;
  }

  private Action enableAction;
  private Action disableAction;

  public void SetEnableDisableActions(Action enable, Action disable)
  {
    enableAction = enable;
    disableAction = disable;
  }

  public void EnableControls()
  {
    enableAction?.Invoke();
  }

  public void DisableControls()
  {
    disableAction?.Invoke();
  }
}