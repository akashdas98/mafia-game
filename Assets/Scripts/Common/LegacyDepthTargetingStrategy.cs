using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public delegate bool LegacyDepthTargetPathResolver(
  Vector2 gunPosition,
  float gunHeight,
  Vector2 targetGround,
  float targetHeight,
  out GameObject resultObject,
  out Vector2 resultPosition);

public class LegacyDepthTargetingStrategyResult
{
  public GameObject ResultObject;
  public Vector2 ResultPosition;
}

public static class LegacyDepthTargetingStrategy
{
  public static bool TryResolve(
    GameObject chosenTarget,
    Vector2 aimPosition,
    Vector2 gunPosition,
    float gunHeight,
    LegacyDepthTargetPathResolver tryResolveSimpleTarget,
    LegacyDepthTargetPathResolver tryResolveObliqueTarget,
    System.Func<Vector2, bool> isInsideMinimumTargetRadius,
    out LegacyDepthTargetingStrategyResult result)
  {
    result = null;

    GameObject depthColliderObject = FindChildWithTag(chosenTarget, "DepthCollider");
    PolygonCollider2D depthCollider = depthColliderObject != null ? depthColliderObject.GetComponent<PolygonCollider2D>() : null;
    if (depthCollider == null)
    {
      return false;
    }

    if (isInsideMinimumTargetRadius != null && isInsideMinimumTargetRadius(aimPosition))
    {
      result = NewResult(chosenTarget, aimPosition);
      return true;
    }

    Vector2 targetBottomIntersection = FindVerticalIntersectionPoint(depthCollider, aimPosition, false) ?? Vector2.zero;
    Vector2 gunGround = new Vector2(gunPosition.x, gunPosition.y - gunHeight);
    float selectedTargetHeight = Mathf.Abs(aimPosition.y - targetBottomIntersection.y);

    if (tryResolveSimpleTarget != null &&
      tryResolveSimpleTarget(gunPosition, gunHeight, targetBottomIntersection, selectedTargetHeight, out GameObject simpleTarget, out Vector2 simplePosition))
    {
      result = NewResult(simpleTarget, simplePosition);
      return true;
    }

    if (tryResolveObliqueTarget != null &&
      tryResolveObliqueTarget(gunPosition, gunHeight, targetBottomIntersection, selectedTargetHeight, out GameObject obliqueTarget, out Vector2 obliquePosition))
    {
      result = NewResult(obliqueTarget, obliquePosition);
      return true;
    }

    List<(GameObject obj, Vector2 intersection)> potentialInterferences = FindOrderedIntersectingDepthColliders(gunGround, targetBottomIntersection, chosenTarget);

    Debug.DrawLine(targetBottomIntersection, aimPosition, Color.green, 0);
    Debug.DrawLine(gunGround, targetBottomIntersection, Color.blue, 0);

    for (int i = 0; i < potentialInterferences.Count; i++)
    {
      GameObject parent = potentialInterferences[i].obj;
      Vector2 depthColliderIntersection = potentialInterferences[i].intersection;
      GameObject parentDepthColliderObject = FindChildWithTag(parent, "DepthCollider");
      GameObject parentHitColliderObject = FindChildWithTag(parent, "HitCollider");
      PolygonCollider2D parentDepthCollider = parentDepthColliderObject != null ? parentDepthColliderObject.GetComponent<PolygonCollider2D>() : null;
      PolygonCollider2D parentHitCollider = parentHitColliderObject != null ? parentHitColliderObject.GetComponent<PolygonCollider2D>() : null;
      if (parentDepthCollider == null || parentHitCollider == null)
      {
        continue;
      }

      float distance = Mathf.Abs(depthColliderIntersection.y - gunGround.y);
      float mainDistance = Mathf.Abs(targetBottomIntersection.y - gunGround.y);
      if (mainDistance <= Mathf.Epsilon)
      {
        continue;
      }

      float mainTargetMinusGunHeight = Mathf.Abs(selectedTargetHeight - gunHeight);
      float newHeight = distance * (mainTargetMinusGunHeight / mainDistance);
      Vector2 point = new Vector2(depthColliderIntersection.x, depthColliderIntersection.y + gunHeight + newHeight * (selectedTargetHeight > gunHeight ? 1 : -1));

      if (isInsideMinimumTargetRadius != null && isInsideMinimumTargetRadius(point))
      {
        continue;
      }

      bool isPointInsideHitCollider = IsPointInsideChildCollider(parent, point, "HitCollider");
      Debug.DrawLine(depthColliderIntersection, point, Color.cyan, 0);
      if (isPointInsideHitCollider)
      {
        result = NewResult(parent, point);
        return true;
      }

      Vector2? firstIntersectionOnHitCollider = FindFirstIntersectionOnCollider(parentHitCollider, point, aimPosition);
      if (firstIntersectionOnHitCollider == null)
      {
        continue;
      }

      Vector2 intersection = firstIntersectionOnHitCollider ?? Vector2.zero;
      if (isInsideMinimumTargetRadius != null && isInsideMinimumTargetRadius(intersection))
      {
        continue;
      }

      Debug.DrawLine(depthColliderIntersection, intersection, Color.green, 0);
      result = NewResult(parent, intersection);
      return true;
    }

    result = NewResult(chosenTarget, aimPosition);
    return true;
  }

  private static LegacyDepthTargetingStrategyResult NewResult(GameObject obj, Vector2 position)
  {
    return new LegacyDepthTargetingStrategyResult
    {
      ResultObject = obj,
      ResultPosition = position
    };
  }

  private static bool IsPointInsideChildCollider(GameObject parent, Vector2 point, string colliderName)
  {
    GameObject hitColliderChild = FindChildWithTag(parent, colliderName);
    if (hitColliderChild == null)
    {
      return false;
    }

    PolygonCollider2D polygonCollider = hitColliderChild.GetComponent<PolygonCollider2D>();
    return polygonCollider != null && polygonCollider.OverlapPoint(point);
  }

  private static GameObject FindChildWithTag(GameObject parent, string tag)
  {
    if (parent == null)
    {
      return null;
    }

    foreach (Transform child in parent.transform)
    {
      if (child.CompareTag(tag))
      {
        return child.gameObject;
      }
    }

    return null;
  }

  private static Vector2? FindFirstIntersectionOnCollider(PolygonCollider2D polygon, Vector2 src, Vector2 dest)
  {
    List<Vector2> intersectionPoints = new List<Vector2>();
    Vector2[] polygonPoints = polygon.points;
    int pointsCount = polygonPoints.Length;

    for (int i = 0; i < pointsCount; i++)
    {
      Vector2 p1 = polygon.transform.TransformPoint(polygonPoints[i]);
      Vector2 p2 = polygon.transform.TransformPoint(polygonPoints[(i + 1) % pointsCount]);
      if (Utilities.LinesIntersect(src, dest, p1, p2, out Vector2? intersection) && intersection != null)
      {
        intersectionPoints.Add(intersection ?? Vector2.zero);
      }
    }

    List<Vector2> sortedIntersectionPoints = intersectionPoints
      .OrderBy(point => Vector2.Distance(src, point))
      .ToList();
    return sortedIntersectionPoints.Count > 0 ? sortedIntersectionPoints[0] : null;
  }

  private static List<(GameObject obj, Vector2 intersection)> FindOrderedIntersectingDepthColliders(Vector2 src, Vector2 dest, GameObject chosenTarget)
  {
    Camera mainCamera = Camera.main;
    float verticalHeight = mainCamera.orthographicSize * 2.0f;
    float verticalWidth = verticalHeight * mainCamera.aspect;
    Vector2 center = (Vector2)mainCamera.transform.position;
    Vector2 size = new Vector2(verticalWidth, verticalHeight);

    Collider2D[] colliders = Physics2D.OverlapBoxAll(center, size, 0);
    List<(GameObject obj, Vector2 intersection)> intersectingDepthColliders = new List<(GameObject obj, Vector2 intersection)>();
    foreach (Collider2D collider in colliders)
    {
      if (!collider.gameObject.CompareTag("DepthCollider") ||
        collider.gameObject.transform.parent == null ||
        collider.gameObject.transform.parent.gameObject == chosenTarget)
      {
        continue;
      }

      PolygonCollider2D polygonCollider = collider as PolygonCollider2D;
      if (polygonCollider == null)
      {
        continue;
      }

      Vector2? intersection = FindFirstIntersectionOnCollider(polygonCollider, src, dest);
      if (intersection != null)
      {
        intersectingDepthColliders.Add((collider.gameObject, intersection ?? Vector2.zero));
      }
    }

    List<(GameObject obj, Vector2 intersection)> filteredAndSorted = intersectingDepthColliders
      .Where(item => item.obj.transform.parent != null)
      .Select(item => (item.obj.transform.parent.gameObject, item.intersection))
      .OrderBy(item => item.intersection.y)
      .ToList();

    bool isAscending = src.y <= dest.y;
    if (!isAscending)
    {
      filteredAndSorted.Reverse();
    }

    return filteredAndSorted;
  }

  private static Vector2? FindVerticalIntersectionPoint(PolygonCollider2D polygon, Vector2 point, bool findHighest)
  {
    List<float> intersectionsY = new List<float>();
    Vector2[] worldPoints = new Vector2[polygon.points.Length];
    for (int i = 0; i < polygon.points.Length; i++)
    {
      worldPoints[i] = polygon.transform.TransformPoint(polygon.points[i]);
    }

    for (int i = 0; i < worldPoints.Length; i++)
    {
      Vector2 start = worldPoints[i];
      Vector2 end = worldPoints[(i + 1) % worldPoints.Length];
      if ((point.x >= start.x && point.x <= end.x) || (point.x >= end.x && point.x <= start.x))
      {
        float fraction = (point.x - start.x) / (end.x - start.x);
        float intersectY = start.y + fraction * (end.y - start.y);
        intersectionsY.Add(intersectY);
      }
    }

    if (intersectionsY.Count == 0)
    {
      return null;
    }

    float resultY = findHighest ? Mathf.Max(intersectionsY.ToArray()) : Mathf.Min(intersectionsY.ToArray());
    return new Vector2(point.x, resultY);
  }
}
