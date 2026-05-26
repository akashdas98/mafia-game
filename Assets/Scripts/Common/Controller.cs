using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Controller : Base
{
  protected List<Interactable> interactables = new List<Interactable>();
  public SceneDetails CurrentSceneDetails { get; private set; }

  private void AddInteractable(Interactable interactable)
  {
    if (interactable != null)
    {
      interactables.Add(interactable);
    }
  }

  private void RemoveInteractable(Interactable interactable)
  {
    if (interactables.Contains(interactable))
    {
      interactables.Remove(interactable);
    }
  }

  public Interactable? GetCurrentInteractable()
  {
    if (interactables.Count == 0) return null;
    return interactables[interactables.Count - 1];
  }

  protected void OnTriggerEnter2D(Collider2D other)
  {
    if (other.CompareTag("SceneManager"))
    {
      SceneDetails sceneDetails = other.GetComponent<SceneDetails>();
      SetCurrentScene(sceneDetails);
    }
    else if (other.gameObject.CompareTag("Interactable"))
    {
      Interactable interactable = other.GetComponent<Interactable>();
      AddInteractable(interactable);
    }
  }

  protected void OnTriggerExit2D(Collider2D other)
  {
    if (other.gameObject.CompareTag("Interactable"))
    {
      Interactable interactable = other.GetComponent<Interactable>();
      RemoveInteractable(interactable);
    }
  }

  private void SetCurrentScene(SceneDetails sceneDetails)
  {
    if (sceneDetails != null)
    {
      CurrentSceneDetails = sceneDetails;
      Debug.Log("Current Scene: " + CurrentSceneDetails.name);
    }
  }
}
