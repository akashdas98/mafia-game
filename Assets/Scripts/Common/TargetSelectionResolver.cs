using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TargetSelectionResolver
{
  public static GameObject GetChosenTarget(Vector2 clickPosition, Func<ObliqueLoftCollider, Vector2, bool> hasProjectedObliqueFaceAt)
  {
    List<GameObject> objectsUnderTarget = GetViewSortedObjectsUnderPoint(clickPosition, hasProjectedObliqueFaceAt);
    return objectsUnderTarget.Count > 0 ? objectsUnderTarget[0] : null;
  }

  private static List<GameObject> GetViewSortedObjectsUnderPoint(Vector2 clickPosition, Func<ObliqueLoftCollider, Vector2, bool> hasProjectedObliqueFaceAt)
  {
    RaycastHit2D[] hits = Physics2D.RaycastAll(clickPosition, Vector2.zero);
    Collider2D[] pointOverlaps = Physics2D.OverlapPointAll(clickPosition);
    return hits
        .Select(hit => hit.collider)
        .Concat(pointOverlaps)
        .Where(collider => collider != null)
        .Distinct()
        .Select(ResolveTargetFromRaycastHit)
        .Where(go => go != null)
        .Concat(GetProjectedObliqueObjectsUnderPoint(clickPosition, hasProjectedObliqueFaceAt))
        .Distinct()
        .OrderByDescending(GetSortingLayerId)
        .ThenByDescending(GetSortingOrder)
        .ThenBy(go => go.transform.position.y)
        .ToList();
  }

  private static IEnumerable<GameObject> GetProjectedObliqueObjectsUnderPoint(Vector2 clickPosition, Func<ObliqueLoftCollider, Vector2, bool> hasProjectedObliqueFaceAt)
  {
    if (hasProjectedObliqueFaceAt == null)
    {
      yield break;
    }

    foreach (ObliqueLoftCollider loftCollider in UnityEngine.Object.FindObjectsOfType<ObliqueLoftCollider>())
    {
      if (loftCollider == null || !loftCollider.UseInRaycasts)
      {
        continue;
      }

      loftCollider.Rebuild();
      if (loftCollider.IsValid && hasProjectedObliqueFaceAt(loftCollider, clickPosition))
      {
        yield return loftCollider.gameObject;
      }
    }
  }

  private static GameObject ResolveTargetFromRaycastHit(Collider2D hitCollider)
  {
    if (hitCollider == null)
    {
      return null;
    }

    if (hitCollider.gameObject.CompareTag("EnclosureCollider") && hitCollider.transform.parent != null)
    {
      return hitCollider.transform.parent.gameObject;
    }

    SimpleTarget simpleTarget = hitCollider.GetComponent<SimpleTarget>() ?? hitCollider.GetComponentInParent<SimpleTarget>();
    if (simpleTarget != null && simpleTarget.UseInTargeting && simpleTarget.IsValid)
    {
      return simpleTarget.GetTargetObject();
    }

    ObliqueLoftCollider loftCollider = hitCollider.GetComponent<ObliqueLoftCollider>() ?? hitCollider.GetComponentInParent<ObliqueLoftCollider>();
    if (loftCollider != null && loftCollider.UseInRaycasts)
    {
      return loftCollider.gameObject;
    }

    return null;
  }

  private static int GetSortingLayerId(GameObject obj)
  {
    Renderer renderer = ResolveRenderer(obj);
    return renderer != null ? renderer.sortingLayerID : 0;
  }

  private static int GetSortingOrder(GameObject obj)
  {
    Renderer renderer = ResolveRenderer(obj);
    return renderer != null ? renderer.sortingOrder : 0;
  }

  private static Renderer ResolveRenderer(GameObject obj)
  {
    if (obj == null)
    {
      return null;
    }

    Renderer renderer = obj.GetComponent<Renderer>() ?? obj.GetComponentInChildren<Renderer>();
    if (renderer != null)
    {
      return renderer;
    }

    Transform parent = obj.transform.parent;
    while (parent != null)
    {
      renderer = parent.GetComponent<Renderer>() ?? parent.GetComponentInChildren<Renderer>();
      if (renderer != null)
      {
        return renderer;
      }

      parent = parent.parent;
    }

    return null;
  }
}
