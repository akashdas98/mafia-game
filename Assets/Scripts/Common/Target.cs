using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Target : Base
{
  [SerializeField] private SpriteRenderer sprite;

  private bool enabled = false;
  private GunController gunController;
  private GameObject? chosenTarget, actualTarget;
  private Vector2 position, actualPosition;


  public void Initialize(GunController gunController)
  {
    this.gunController = gunController;
  }

  public Vector2 GetChosenPosition()
  {
    return this.position;
  }

  public Vector2 GetActualPosition()
  {
    return this.actualPosition;
  }

  public void AimAt(Vector2 position)
  {
    this.enabled = true;
    this.position = position;

    if (this.actualTarget)
    {
      this.actualTarget.GetComponent<Highlighter>()?.ResetHighlight();
    }

    this.chosenTarget = GetChosenTarget();
    if (this.chosenTarget != null)
    {
      var res = GetActualTarget();

      this.actualTarget = res.obj;
      this.actualPosition = res.pos;
      this.actualTarget?.GetComponent<Highlighter>()?.Highlight();
    }
    else
    {
      this.actualPosition = this.position;
    }
  }

  public void Reset()
  {
    this.enabled = false;
    if (this.actualTarget)
    {
      this.actualTarget.GetComponent<Highlighter>()?.ResetHighlight();
    }
    this.chosenTarget = null;
    this.actualTarget = null;
  }


  private void SetVisibility(bool isVisible)
  {
    sprite.enabled = isVisible;
  }

  private bool IsPointInsideChildCollider(GameObject parent, Vector2 point, string colliderName)
  {
    // Find child with the tag
    GameObject hitColliderChild = FindChildWithTag(parent, colliderName);

    if (hitColliderChild != null)
    {
      PolygonCollider2D polygonCollider = hitColliderChild.GetComponent<PolygonCollider2D>();

      if (polygonCollider != null && polygonCollider.OverlapPoint(point))
      {
        // If the point is inside the polygon, keep this GameObject
        return true;
      }
    }

    return false;
  }

  private List<GameObject> GetViewSortedObjectsUnderPoint(Vector2 clickPosition)
  {
    RaycastHit2D[] hits = Physics2D.RaycastAll(clickPosition, Vector2.zero);
    List<GameObject> sortedObjects = hits
        .Where(hit => hit.collider.gameObject.CompareTag("EnclosureCollider")) // Filter by tag
        .Where(hit => hit.collider.gameObject.transform.parent != null) // Ensure the object has a parent
        .Select(hit => hit.collider.gameObject.transform.parent.gameObject) // Select the parent GameObject
        .OrderByDescending(go => go.GetComponent<Renderer>() != null ? go.GetComponent<Renderer>().sortingLayerID : 0)
        .ThenByDescending(go => go.GetComponent<Renderer>() != null ? go.GetComponent<Renderer>().sortingOrder : 0)
        .ThenBy(go => go.transform.position.y)
        .ToList();

    return sortedObjects;
  }

  private GameObject? GetChosenTarget()
  {
    List<GameObject> objectsUnderTarget = GetViewSortedObjectsUnderPoint(this.position);

    if (objectsUnderTarget.Any())
    {
      GameObject topObject = objectsUnderTarget[0];
      return topObject;
    }
    return null;
  }

  private GameObject? FindChildWithTag(GameObject parent, string tag)
  {
    foreach (Transform child in parent.transform)
    {
      if (child.CompareTag(tag))
      {
        return child.gameObject;
      }
    }
    return null;
  }

  private Vector2? FindFirstIntersectionOnCollider(PolygonCollider2D polygon, Vector2 src, Vector2 dest)
  {
    List<Vector2> intersectionPoints = new List<Vector2>();

    Vector2[] polygonPoints = polygon.points;
    int pointsCount = polygonPoints.Length;

    // Loop through each edge of the polygon
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

    if (sortedIntersectionPoints.Count > 0)
    {
      return sortedIntersectionPoints[0];
    }

    return null;
  }

  private List<(GameObject obj, Vector2 intersection)> FindOrderedIntersectingDepthColliders(Vector2 src, Vector2 dest)
  {
    Camera mainCamera = Camera.main; // Ensure you have a reference to the main camera

    // Get the camera's bounds
    float verticalHeight = mainCamera.orthographicSize * 2.0f;
    float verticalWidth = verticalHeight * mainCamera.aspect;
    Vector2 center = (Vector2)mainCamera.transform.position;
    Vector2 size = new Vector2(verticalWidth, verticalHeight);

    // Fetch only the colliders within the camera's view
    Collider2D[] colliders = Physics2D.OverlapBoxAll(center, size, 0);
    List<(GameObject obj, Vector2 intersection)> intersectingDepthColliders = new List<(GameObject obj, Vector2 intersection)>();

    foreach (var collider in colliders)
    {
      if (collider.gameObject.CompareTag("DepthCollider") && collider.gameObject.transform.parent.gameObject != chosenTarget)
      {
        PolygonCollider2D polygonCollider = collider as PolygonCollider2D;
        if (polygonCollider != null)
        {
          Vector2? intersection = FindFirstIntersectionOnCollider(polygonCollider, src, dest);
          if (intersection != null)
          {
            intersectingDepthColliders.Add((collider.gameObject, intersection ?? Vector2.zero));
          }
        }
      }
    }

    // Filter to include only objects with parents, and select the parent for the final list
    var filteredAndSorted = intersectingDepthColliders
        .Where(item => item.obj.transform.parent != null) // Filter out objects without parents / chosenTarget
        .Select(item => (item.obj.transform.parent.gameObject, item.intersection)) // Select the parent
        .OrderBy(item => item.intersection.y) // Sort by intersection y-value
        .ToList();

    // Adjust sorting direction based on src to dest direction
    bool isAscending = src.y <= dest.y;
    if (!isAscending)
    {
      filteredAndSorted.Reverse();
    }

    return filteredAndSorted; // Return only the parent GameObjects
  }

  private Vector2? FindVerticalIntersectionPoint(PolygonCollider2D polygon, Vector2 point, bool findHighest)
  {
    List<float> intersectionsY = new List<float>();

    // Convert local points to world points
    Vector2[] worldPoints = new Vector2[polygon.points.Length];
    for (int i = 0; i < polygon.points.Length; i++)
    {
      worldPoints[i] = polygon.transform.TransformPoint(polygon.points[i]);
    }

    for (int i = 0; i < worldPoints.Length; i++)
    {
      Vector2 start = worldPoints[i];
      Vector2 end = worldPoints[(i + 1) % worldPoints.Length]; // Loop back to the first point

      // Check if the line crosses the x value of the point
      if ((point.x >= start.x && point.x <= end.x) || (point.x >= end.x && point.x <= start.x))
      {
        // Find intersection point on this edge
        float fraction = (point.x - start.x) / (end.x - start.x);
        float intersectY = start.y + fraction * (end.y - start.y);

        intersectionsY.Add(intersectY);
      }
    }

    // No intersections found
    if (intersectionsY.Count == 0) return null;

    // Find and return the highest or lowest intersection point based on the parameter
    float resultY = findHighest ? Mathf.Max(intersectionsY.ToArray()) : Mathf.Min(intersectionsY.ToArray());
    return new Vector2(point.x, resultY);
  }

  private (GameObject obj, Vector2 pos) GetActualTarget()
  {
    Vector2 gunPosition = gunController.GetGun().GetPosition();
    float gunDistance = gunController.GetGunDistance();
    float gunHeight = gunController.GetGunHeight(); ;

    Vector2 targetBottomIntersection = FindVerticalIntersectionPoint(
      FindChildWithTag(chosenTarget, "DepthCollider").GetComponent<PolygonCollider2D>(),
      this.position,
      false
    ) ?? Vector2.zero;

    bool vertical = Vector2.Angle(gunPosition - targetBottomIntersection, gunPosition - (gunPosition + new Vector2(0, 100))) <= 2f;

    Vector2 gunGround = new Vector2(gunPosition.x, gunPosition.y - gunHeight);

    List<(GameObject obj, Vector2 intersection)> potentialInterferences = FindOrderedIntersectingDepthColliders(
      gunGround,
      targetBottomIntersection
    );

    Debug.DrawLine(targetBottomIntersection, this.position, Color.green, 0);
    Debug.DrawLine(gunGround, targetBottomIntersection, Color.blue, 0);

    for (int i = 0; i < potentialInterferences.Count; i++)
    {
      GameObject parent = potentialInterferences[i].obj;
      Vector2 depthColliderIntersection = potentialInterferences[i].intersection;
      PolygonCollider2D polygonCollider = FindChildWithTag(parent, "DepthCollider").GetComponent<PolygonCollider2D>();

      float targetHeight = Mathf.Abs(this.position.y - targetBottomIntersection.y);
      float distance = Mathf.Abs(depthColliderIntersection.y - gunGround.y);
      float mainDistance = Mathf.Abs(targetBottomIntersection.y - gunGround.y);
      float mainTargetMinusGunHeight = Mathf.Abs(targetHeight - gunHeight);
      float newHeight = distance * (mainTargetMinusGunHeight / mainDistance);

      Vector2 point = new Vector2(depthColliderIntersection.x, depthColliderIntersection.y + gunHeight + newHeight * (targetHeight > gunHeight ? 1 : -1));

      if (point != null)
      {
        bool isPointInsideHitCollider = IsPointInsideChildCollider(parent, point, "HitCollider");
        Debug.DrawLine(depthColliderIntersection, point, Color.cyan, 0);
        if (isPointInsideHitCollider)
        {
          return (parent, point);
        }
        else
        {
          Vector2? firstIntersectionOnHitCollider = FindFirstIntersectionOnCollider(
            FindChildWithTag(parent, "HitCollider").GetComponent<PolygonCollider2D>(),
            point,
            this.position
          );
          if (firstIntersectionOnHitCollider != null)
          {
            Vector2 intersection = firstIntersectionOnHitCollider ?? Vector2.zero;
            Debug.DrawLine(depthColliderIntersection, intersection, Color.green, 0);
            return (parent, intersection);
          }
        }
      }
    }

    return (chosenTarget, this.position);
  }


  void Start()
  {
    SetVisibility(false);
  }

  private void Render()
  {
    if (enabled)
    {
      SetVisibility(true);
      this.transform.position = this.actualPosition;
    }
    else
    {
      SetVisibility(false);
    }
  }

  void FixedUpdate()
  {
    Render();
  }
}
