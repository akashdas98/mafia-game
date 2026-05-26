using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimpleTarget : MonoBehaviour
{
  [SerializeField] private bool useInTargeting = true;
  [SerializeField] private PolygonCollider2D groundCollider;
  [SerializeField] private PolygonCollider2D hitCollider;

  public bool UseInTargeting => useInTargeting;
  public PolygonCollider2D GroundCollider => ResolveGroundCollider();
  public PolygonCollider2D HitCollider => ResolveHitCollider();

  public bool IsValid => GroundCollider != null && HitCollider != null;

  public bool TryGetGroundPointBelowAim(Vector2 aimPoint, out Vector2 groundPoint)
  {
    groundPoint = Vector2.zero;
    PolygonCollider2D collider = GroundCollider;
    if (collider == null)
    {
      return false;
    }

    List<float> intersectionsY = new List<float>();
    Vector2[] points = GetWorldPoints(collider);
    for (int i = 0; i < points.Length; i++)
    {
      Vector2 a = points[i];
      Vector2 b = points[(i + 1) % points.Length];
      if (!IsBetween(aimPoint.x, a.x, b.x) || Mathf.Approximately(a.x, b.x))
      {
        continue;
      }

      float t = (aimPoint.x - a.x) / (b.x - a.x);
      if (t < 0f || t > 1f)
      {
        continue;
      }

      intersectionsY.Add(Mathf.Lerp(a.y, b.y, t));
    }

    if (intersectionsY.Count == 0)
    {
      return false;
    }

    groundPoint = new Vector2(aimPoint.x, intersectionsY.Min());
    return true;
  }

  public bool TryGetFirstGroundIntersection(Vector2 fromGround, Vector2 toGround, out Vector2 intersection)
  {
    intersection = Vector2.zero;
    PolygonCollider2D collider = GroundCollider;
    if (collider == null)
    {
      return false;
    }

    List<Vector2> intersections = new List<Vector2>();
    Vector2[] points = GetWorldPoints(collider);
    for (int i = 0; i < points.Length; i++)
    {
      Vector2 a = points[i];
      Vector2 b = points[(i + 1) % points.Length];
      if (Utilities.LinesIntersect(fromGround, toGround, a, b, out Vector2? hit) && hit != null)
      {
        intersections.Add(hit.Value);
      }
    }

    if (collider.OverlapPoint(fromGround))
    {
      intersections.Add(fromGround);
    }

    if (intersections.Count == 0)
    {
      return false;
    }

    intersection = intersections
      .OrderBy(point => Vector2.Distance(fromGround, point))
      .First();
    return true;
  }

  public bool ContainsHitPoint(Vector2 point)
  {
    PolygonCollider2D collider = HitCollider;
    return collider != null && collider.OverlapPoint(point);
  }

  public bool TryGetFirstHitPolygonIntersection(Vector2 fromVisual, Vector2 toVisual, out Vector2 intersection)
  {
    intersection = Vector2.zero;
    PolygonCollider2D collider = HitCollider;
    if (collider == null)
    {
      return false;
    }

    List<Vector2> intersections = new List<Vector2>();
    Vector2[] points = GetWorldPoints(collider);
    for (int i = 0; i < points.Length; i++)
    {
      Vector2 a = points[i];
      Vector2 b = points[(i + 1) % points.Length];
      if (Utilities.LinesIntersect(fromVisual, toVisual, a, b, out Vector2? hit) && hit != null)
      {
        intersections.Add(hit.Value);
      }
    }

    if (intersections.Count == 0)
    {
      return false;
    }

    intersection = intersections
      .OrderBy(point => Vector2.Distance(fromVisual, point))
      .First();
    return true;
  }

  public GameObject GetTargetObject()
  {
    Highlighter highlighter = GetComponent<Highlighter>() ?? GetComponentInChildren<Highlighter>();
    return highlighter != null ? highlighter.gameObject : gameObject;
  }

  private PolygonCollider2D ResolveGroundCollider()
  {
    if (groundCollider != null)
    {
      return groundCollider;
    }

    PolygonCollider2D taggedCollider = FindChildColliderWithTag("DepthCollider");
    if (taggedCollider != null)
    {
      return taggedCollider;
    }

    return FindColliderFromEntityRefs("DepthCollider");
  }

  private PolygonCollider2D ResolveHitCollider()
  {
    if (hitCollider != null)
    {
      return hitCollider;
    }

    PolygonCollider2D taggedCollider = FindChildColliderWithTag("HitCollider");
    if (taggedCollider != null)
    {
      return taggedCollider;
    }

    return FindColliderFromEntityRefs("HitCollider");
  }

  private PolygonCollider2D FindChildColliderWithTag(string tag)
  {
    foreach (Transform child in transform)
    {
      if (child.CompareTag(tag))
      {
        return child.GetComponent<PolygonCollider2D>();
      }
    }

    return null;
  }

  private PolygonCollider2D FindColliderFromEntityRefs(string tag)
  {
    EntityRefs refs = GetComponentInParent<EntityRefs>();
    if (refs == null)
    {
      return null;
    }

    List<PolygonCollider2D> colliders = refs.GetAll<PolygonCollider2D>();
    for (int i = 0; i < colliders.Count; i++)
    {
      PolygonCollider2D collider = colliders[i];
      if (collider != null && collider.CompareTag(tag))
      {
        return collider;
      }
    }

    return null;
  }

  private Vector2[] GetWorldPoints(PolygonCollider2D collider)
  {
    Vector2[] points = new Vector2[collider.points.Length];
    for (int i = 0; i < collider.points.Length; i++)
    {
      points[i] = collider.transform.TransformPoint(collider.points[i]);
    }

    return points;
  }

  private bool IsBetween(float value, float a, float b)
  {
    return value >= Mathf.Min(a, b) && value <= Mathf.Max(a, b);
  }
}
