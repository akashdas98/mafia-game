using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Controller : Base
{
  protected List<Interactable> interactables = new List<Interactable>();

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
      Refs.SetCurrentScene(sceneDetails);
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
}