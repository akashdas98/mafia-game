using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Controller : Interactable
{
  protected ControlManager controlManager;
  protected SceneDetails currentSceneDetails;

  [SerializeField]
  protected Animator animator;

  public abstract void SetInputs(InputData input);

  public abstract Dictionary<string, float> GetInputs();

  public abstract void ResetInputs();

  public override sealed void Interact(GameObject other) { }
  public abstract void Interact(GameObject other, ControlManager cm);

  public void AssignControlManager(ControlManager cm)
  {
    controlManager = cm;
  }

  protected Interactable currentInteractable = null;
  protected List<Interactable> interactables = new List<Interactable>();

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

  public SceneDetails GetCurrentSceneDetails()
  {
    return currentSceneDetails;
  }

  private void SetCurrentScene(Collider2D other)
  {
    if (other.CompareTag("SceneManager"))
    {
      SceneDetails sceneDetails = other.GetComponent<SceneDetails>();

      if (sceneDetails != null)
      {
        currentSceneDetails = sceneDetails;
        Debug.Log("Current Scene: " + currentSceneDetails.name);
      }
    }
  }

  private void AddInteractable(Collider2D other)
  {
    Interactable interactable = other.GetComponent<Interactable>();
    if (interactable != null)
    {
      interactables.Add(interactable);
      UpdateCurrentInteractable();
    }
  }

  private void RemoveInteractable(Collider2D other)
  {
    Interactable interactable = other.GetComponent<Interactable>();
    if (interactables.Contains(interactable))
    {
      interactables.Remove(interactable);
      UpdateCurrentInteractable();
    }
  }

  private void UpdateCurrentInteractable()
  {
    if (interactables.Count == 0)
    {
      currentInteractable = null;
    }
    else
    {
      currentInteractable = interactables[interactables.Count - 1];
    }
  }

  protected void OnTriggerEnter2D(Collider2D other)
  {
    SetCurrentScene(other);
    AddInteractable(other);
  }

  protected void OnTriggerExit2D(Collider2D other)
  {
    RemoveInteractable(other);
  }
}