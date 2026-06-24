using System.Collections.Generic;
using UnityEngine;

public class CharacterInteractor : MonoBehaviour, IInteractInputReceiver
{
  private readonly List<Interactable> interactables = new List<Interactable>();

  public Interactable CurrentInteractable => interactables.Count == 0
    ? null
    : interactables[interactables.Count - 1];

  public void Interact(GameObject actor)
  {
    Interactable currentInteractable = CurrentInteractable;
    if (currentInteractable != null)
    {
      currentInteractable.Interact(actor);
    }
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (!other.gameObject.CompareTag("Interactable"))
    {
      return;
    }

    Interactable interactable = other.GetComponent<Interactable>();
    if (interactable != null && !interactables.Contains(interactable))
    {
      interactables.Add(interactable);
    }
  }

  private void OnTriggerExit2D(Collider2D other)
  {
    if (!other.gameObject.CompareTag("Interactable"))
    {
      return;
    }

    Interactable interactable = other.GetComponent<Interactable>();
    if (interactable != null)
    {
      interactables.Remove(interactable);
    }
  }
}
