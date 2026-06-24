using UnityEngine;

public class Interactable : MonoBehaviour
{
  public virtual void Interact(GameObject other) { }

  protected bool TryGetPart<T>(out T part) where T : class
  {
    MonoBehaviour[] behaviours = GetComponentsInParent<MonoBehaviour>(true);
    for (int i = 0; i < behaviours.Length; i++)
    {
      if (behaviours[i] is T typed)
      {
        part = typed;
        return true;
      }
    }

    behaviours = GetComponentsInChildren<MonoBehaviour>(true);
    for (int i = 0; i < behaviours.Length; i++)
    {
      if (behaviours[i] is T typed)
      {
        part = typed;
        return true;
      }
    }

    part = null;
    return false;
  }
}
