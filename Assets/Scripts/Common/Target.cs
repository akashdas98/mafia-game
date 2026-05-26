using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Target : Base
{
  [SerializeField] private SpriteRenderer sprite;
  [Header("Oblique Loft LOS")]
  [SerializeField] private bool useSimpleTargeting = true;
  [SerializeField] private bool useObliqueLoftLos = false;
  [SerializeField] private bool drawObliqueLoftDebug = true;

  private bool enabled = false;
  private GunController gunController;
  private GameObject? chosenTarget, actualTarget;
  private Vector2 position, actualPosition;
  private bool hasObliqueDebugRay;
  private ObliqueRay lastObliqueDebugRay;
  private bool lastObliqueDebugBlocked;
  private ObliqueRayHit lastObliqueDebugHit;
  private string lastObliqueDebugStatus = "";
  private bool actualTargetUsesObliqueHighlight;

  private class SimpleTargetCandidate
  {
    public SimpleTarget Target;
    public GameObject TargetObject;
    public Vector2 GroundPoint;
    public Vector2 HitPoint;
    public float TargetHeight;
    public float Distance;
  }

  private class ObliqueFaceAimCandidate
  {
    public Vector3 LogicPoint;
    public float Score;
  }

  public bool DrawObliqueLoftDebug => drawObliqueLoftDebug;
  public bool HasObliqueDebugRay => hasObliqueDebugRay;
  public ObliqueRay LastObliqueDebugRay => lastObliqueDebugRay;
  public bool LastObliqueDebugBlocked => lastObliqueDebugBlocked;
  public ObliqueRayHit LastObliqueDebugHit => lastObliqueDebugHit;
  public string LastObliqueDebugStatus => lastObliqueDebugStatus;

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
    this.actualTargetUsesObliqueHighlight = false;

    this.chosenTarget = GetChosenTarget();
    if (this.chosenTarget != null)
    {
      var res = GetActualTarget();

      this.actualTarget = res.obj;
      this.actualPosition = res.pos;
      Highlighter highlighter = this.actualTarget?.GetComponent<Highlighter>();
      if (this.actualTargetUsesObliqueHighlight)
      {
        highlighter?.HighlightObliqueBlocked();
      }
      else
      {
        highlighter?.Highlight();
      }
    }
    else
    {
      this.actualPosition = this.position;
      this.hasObliqueDebugRay = false;
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
    this.hasObliqueDebugRay = false;
    this.actualTargetUsesObliqueHighlight = false;
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
        .Select(hit => ResolveTargetFromRaycastHit(hit.collider))
        .Where(go => go != null)
        .Concat(GetProjectedObliqueObjectsUnderPoint(clickPosition))
        .Distinct()
        .OrderByDescending(go => go.GetComponent<Renderer>() != null ? go.GetComponent<Renderer>().sortingLayerID : 0)
        .ThenByDescending(go => go.GetComponent<Renderer>() != null ? go.GetComponent<Renderer>().sortingOrder : 0)
        .ThenBy(go => go.transform.position.y)
        .ToList();

    return sortedObjects;
  }

  private IEnumerable<GameObject> GetProjectedObliqueObjectsUnderPoint(Vector2 clickPosition)
  {
    foreach (ObliqueLoftCollider loftCollider in Object.FindObjectsOfType<ObliqueLoftCollider>())
    {
      if (loftCollider == null || !loftCollider.UseInRaycasts)
      {
        continue;
      }

      loftCollider.Rebuild();
      if (loftCollider.IsValid && HasProjectedObliqueFaceAt(loftCollider, clickPosition))
      {
        yield return loftCollider.gameObject;
      }
    }
  }

  private GameObject ResolveTargetFromRaycastHit(Collider2D hitCollider)
  {
    if (hitCollider == null)
    {
      return null;
    }

    if (hitCollider.gameObject.CompareTag("EnclosureCollider") && hitCollider.transform.parent != null)
    {
      return hitCollider.transform.parent.gameObject;
    }

    ObliqueLoftCollider loftCollider = hitCollider.GetComponent<ObliqueLoftCollider>() ?? hitCollider.GetComponentInParent<ObliqueLoftCollider>();
    if (loftCollider != null && loftCollider.UseInRaycasts)
    {
      return loftCollider.gameObject;
    }

    return null;
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

  private bool TryGetObliqueActualTarget(Vector2 gunPosition, float gunHeight, Vector2 targetGround, float targetHeight, out GameObject resultObject, out Vector2 resultPosition)
  {
    resultObject = chosenTarget;
    resultPosition = this.position;

    if (!useObliqueLoftLos || chosenTarget == null)
    {
      hasObliqueDebugRay = false;
      lastObliqueDebugStatus = "";
      return false;
    }

    Vector2 gunGround = new Vector2(gunPosition.x, gunPosition.y - gunHeight);
    ObliqueRay ray = ObliqueLoftLos.CreateShotRay(gunGround, gunHeight, targetGround, targetHeight);
    hasObliqueDebugRay = true;
    lastObliqueDebugRay = ray;
    lastObliqueDebugBlocked = false;
    lastObliqueDebugHit = default(ObliqueRayHit);

    ObliqueLoftCollider[] allLoftColliders = Object.FindObjectsOfType<ObliqueLoftCollider>();
    foreach (ObliqueLoftCollider collider in allLoftColliders)
    {
      collider?.Rebuild();
    }

    int invalidCount = allLoftColliders.Count(collider => collider != null && !collider.IsValid);
    List<ObliqueLoftCollider> candidates = allLoftColliders
      .Where(collider => collider != null)
      .Where(collider => collider.UseInRaycasts && collider.IsValid)
      .Where(collider => !BelongsToObject(collider.transform, chosenTarget))
      .ToList();

    if (candidates.Count == 0)
    {
      resultObject = chosenTarget;
      resultPosition = this.position;
      lastObliqueDebugStatus = invalidCount > 0
        ? "Oblique clear: no valid obstacle loft colliders (" + invalidCount + " invalid)."
        : "Oblique clear: no obstacle loft colliders.";

      if (drawObliqueLoftDebug)
      {
        Debug.DrawLine(LogicPointToVisualPoint(ray.From), LogicPointToVisualPoint(ray.To), Color.green, 0);
      }

      return true;
    }

    lastObliqueDebugBlocked = ObliqueRaycaster.TryRaycast(ray, candidates, out ObliqueRayHit hit);
    lastObliqueDebugHit = hit;
    lastObliqueDebugStatus = lastObliqueDebugBlocked
      ? "Oblique blocked by " + hit.HitObject.name + " " + hit.SurfaceType + " face #" + hit.FaceIndex + "."
      : "Oblique clear through " + candidates.Count + " obstacle loft collider(s).";

    if (lastObliqueDebugBlocked)
    {
      resultObject = ResolveHitObject(hit.Collider);
      resultPosition = LogicPointToVisualPoint(hit.Point);
      actualTargetUsesObliqueHighlight = true;

      if (drawObliqueLoftDebug)
      {
        Debug.DrawLine(LogicPointToVisualPoint(ray.From), LogicPointToVisualPoint(ray.To), Color.magenta, 0);
        Debug.DrawLine(LogicPointToVisualPoint(hit.Point), LogicPointToVisualPoint(hit.Point + hit.Normal * 0.35f), Color.yellow, 0);
      }

      return true;
    }

    if (drawObliqueLoftDebug)
    {
      Debug.DrawLine(LogicPointToVisualPoint(ray.From), LogicPointToVisualPoint(ray.To), Color.green, 0);
    }

    return true;
  }

  private bool TryGetSimpleTargetActualTarget(Vector2 gunPosition, float gunHeight, Vector2 intendedGround, float intendedTargetHeight, out GameObject resultObject, out Vector2 resultPosition)
  {
    resultObject = chosenTarget;
    resultPosition = this.position;

    if (!useSimpleTargeting || chosenTarget == null)
    {
      return false;
    }

    Vector2 gunGround = new Vector2(gunPosition.x, gunPosition.y - gunHeight);
    Vector2 intendedVisualPoint = intendedGround + Vector2.up * intendedTargetHeight;
    SimpleTarget[] simpleTargets = Object.FindObjectsOfType<SimpleTarget>();
    SimpleTarget chosenSimpleTarget = FindSimpleTargetForObject(chosenTarget);
    List<SimpleTargetCandidate> candidates = BuildSimpleTargetCandidates(
      simpleTargets,
      chosenSimpleTarget,
      gunPosition,
      gunGround,
      intendedGround,
      intendedVisualPoint,
      gunHeight,
      intendedTargetHeight
    );

    if (candidates.Count == 0)
    {
      if (chosenSimpleTarget == null)
      {
        return false;
      }

      return TryGetStaticObliqueActualTarget(gunGround, gunHeight, intendedGround, intendedTargetHeight, chosenTarget, out resultObject, out resultPosition);
    }

    foreach (SimpleTargetCandidate candidate in candidates.OrderBy(candidate => candidate.Distance))
    {
      if (TryGetStaticObliqueActualTarget(gunGround, gunHeight, candidate.GroundPoint, candidate.TargetHeight, candidate.TargetObject, out resultObject, out resultPosition))
      {
        if (resultObject != candidate.TargetObject)
        {
          actualTargetUsesObliqueHighlight = true;
        }

        return true;
      }

      resultObject = candidate.TargetObject;
      resultPosition = candidate.HitPoint;
      actualTargetUsesObliqueHighlight = false;
      lastObliqueDebugStatus = "Simple target hit: " + candidate.TargetObject.name + ".";
      return true;
    }

    return TryGetStaticObliqueActualTarget(gunGround, gunHeight, intendedGround, intendedTargetHeight, chosenTarget, out resultObject, out resultPosition);
  }

  private List<SimpleTargetCandidate> BuildSimpleTargetCandidates(
    SimpleTarget[] simpleTargets,
    SimpleTarget chosenSimpleTarget,
    Vector2 gunPosition,
    Vector2 gunGround,
    Vector2 intendedGround,
    Vector2 intendedVisualPoint,
    float gunHeight,
    float intendedTargetHeight)
  {
    List<SimpleTargetCandidate> candidates = new List<SimpleTargetCandidate>();

    foreach (SimpleTarget simpleTarget in simpleTargets)
    {
      if (simpleTarget == null || !simpleTarget.UseInTargeting || !simpleTarget.IsValid || IsSelfSimpleTarget(simpleTarget))
      {
        continue;
      }

      bool isChosen = simpleTarget == chosenSimpleTarget || BelongsToObject(simpleTarget.transform, chosenTarget);
      Vector2 groundPoint;
      float targetHeight;
      Vector2 hitPoint;

      if (isChosen)
      {
        if (!simpleTarget.TryGetGroundPointBelowAim(this.position, out groundPoint))
        {
          continue;
        }

        targetHeight = Mathf.Max(0f, this.position.y - groundPoint.y);
        hitPoint = this.position;
        if (!simpleTarget.ContainsHitPoint(hitPoint) &&
          !simpleTarget.TryGetFirstHitPolygonIntersection(gunPosition, intendedVisualPoint, out hitPoint))
        {
          continue;
        }
      }
      else
      {
        if (!simpleTarget.TryGetFirstGroundIntersection(gunGround, intendedGround, out groundPoint))
        {
          continue;
        }

        float t = GetGroundLineT(gunGround, intendedGround, groundPoint);
        if (t < 0f || t > 1f)
        {
          continue;
        }

        targetHeight = Mathf.Lerp(gunHeight, intendedTargetHeight, t);
        hitPoint = groundPoint + Vector2.up * targetHeight;
        if (!simpleTarget.ContainsHitPoint(hitPoint))
        {
          if (!simpleTarget.TryGetFirstHitPolygonIntersection(gunPosition, intendedVisualPoint, out hitPoint))
          {
            continue;
          }

          targetHeight = Mathf.Max(0f, hitPoint.y - groundPoint.y);
        }
      }

      candidates.Add(new SimpleTargetCandidate
      {
        Target = simpleTarget,
        TargetObject = simpleTarget.GetTargetObject(),
        GroundPoint = groundPoint,
        HitPoint = hitPoint,
        TargetHeight = targetHeight,
        Distance = Vector2.Distance(gunGround, groundPoint)
      });
    }

    return candidates;
  }

  private bool TryGetStaticObliqueActualTarget(Vector2 gunGround, float gunHeight, Vector2 targetGround, float targetHeight, GameObject targetObject, out GameObject resultObject, out Vector2 resultPosition)
  {
    resultObject = targetObject;
    resultPosition = targetGround + Vector2.up * targetHeight;

    if (!useObliqueLoftLos)
    {
      return false;
    }

    ObliqueRay ray = ObliqueLoftLos.CreateShotRay(gunGround, gunHeight, targetGround, targetHeight);
    hasObliqueDebugRay = true;
    lastObliqueDebugRay = ray;
    lastObliqueDebugBlocked = false;
    lastObliqueDebugHit = default(ObliqueRayHit);

    ObliqueLoftCollider[] allLoftColliders = Object.FindObjectsOfType<ObliqueLoftCollider>();
    foreach (ObliqueLoftCollider collider in allLoftColliders)
    {
      collider?.Rebuild();
    }

    int invalidCount = allLoftColliders.Count(collider => collider != null && !collider.IsValid);
    List<ObliqueLoftCollider> candidates = allLoftColliders
      .Where(collider => collider != null)
      .Where(collider => collider.UseInRaycasts && collider.IsValid)
      .Where(collider => !BelongsToObject(collider.transform, targetObject))
      .ToList();

    if (candidates.Count == 0)
    {
      lastObliqueDebugStatus = invalidCount > 0
        ? "Oblique clear: no valid static loft blockers (" + invalidCount + " invalid)."
        : "Oblique clear: no static loft blockers.";

      if (drawObliqueLoftDebug)
      {
        Debug.DrawLine(LogicPointToVisualPoint(ray.From), LogicPointToVisualPoint(ray.To), Color.green, 0);
      }

      return false;
    }

    lastObliqueDebugBlocked = ObliqueRaycaster.TryRaycast(ray, candidates, out ObliqueRayHit hit);
    lastObliqueDebugHit = hit;
    lastObliqueDebugStatus = lastObliqueDebugBlocked
      ? "Oblique blocked by " + hit.HitObject.name + " " + hit.SurfaceType + " face #" + hit.FaceIndex + "."
      : "Oblique clear through " + candidates.Count + " static loft blocker(s).";

    if (!lastObliqueDebugBlocked)
    {
      if (drawObliqueLoftDebug)
      {
        Debug.DrawLine(LogicPointToVisualPoint(ray.From), LogicPointToVisualPoint(ray.To), Color.green, 0);
      }

      return false;
    }

    resultObject = ResolveHitObject(hit.Collider);
    resultPosition = LogicPointToVisualPoint(hit.Point);

    if (drawObliqueLoftDebug)
    {
      Debug.DrawLine(LogicPointToVisualPoint(ray.From), LogicPointToVisualPoint(ray.To), Color.magenta, 0);
      Debug.DrawLine(LogicPointToVisualPoint(hit.Point), LogicPointToVisualPoint(hit.Point + hit.Normal * 0.35f), Color.yellow, 0);
    }

    return true;
  }

  private bool TryGetDirectObliqueActualTarget(Vector2 gunPosition, float gunHeight, out GameObject resultObject, out Vector2 resultPosition)
  {
    resultObject = chosenTarget;
    resultPosition = this.position;

    if (!useObliqueLoftLos || chosenTarget == null || !TryFindObliqueLoftColliderForObject(chosenTarget, out ObliqueLoftCollider selectedCollider))
    {
      return false;
    }

    selectedCollider.Rebuild();
    Vector2 gunGround = new Vector2(gunPosition.x, gunPosition.y - gunHeight);
    if (!selectedCollider.IsValid || !TryGetObliqueAim(selectedCollider, this.position, gunGround, gunHeight, out Vector2 targetGround, out float targetHeight))
    {
      return false;
    }

    ObliqueRay ray = CreateDirectObliqueTargetRay(gunGround, gunHeight, targetGround, targetHeight, selectedCollider);
    hasObliqueDebugRay = true;
    lastObliqueDebugRay = ray;
    lastObliqueDebugBlocked = false;
    lastObliqueDebugHit = default(ObliqueRayHit);

    ObliqueLoftCollider[] allLoftColliders = Object.FindObjectsOfType<ObliqueLoftCollider>();
    foreach (ObliqueLoftCollider collider in allLoftColliders)
    {
      collider?.Rebuild();
    }

    List<ObliqueLoftCollider> candidates = allLoftColliders
      .Where(collider => collider != null)
      .Where(collider => collider.UseInRaycasts && collider.IsValid)
      .ToList();

    if (!ObliqueRaycaster.TryRaycast(ray, candidates, out ObliqueRayHit hit))
    {
      lastObliqueDebugStatus = "Oblique target clear: no loft face hit along extended target ray.";
      if (drawObliqueLoftDebug)
      {
        Debug.DrawLine(LogicPointToVisualPoint(ray.From), LogicPointToVisualPoint(ray.To), Color.green, 0);
      }

      return true;
    }

    lastObliqueDebugBlocked = true;
    lastObliqueDebugHit = hit;
    resultObject = ResolveHitObject(hit.Collider);
    resultPosition = LogicPointToVisualPoint(hit.Point);
    actualTargetUsesObliqueHighlight = !BelongsToObject(hit.Collider.transform, chosenTarget);
    lastObliqueDebugStatus = actualTargetUsesObliqueHighlight
      ? "Oblique blocked by " + hit.HitObject.name + " before selected loft target."
      : "Oblique target hit: " + hit.HitObject.name + " " + hit.SurfaceType + " face #" + hit.FaceIndex + ".";

    if (drawObliqueLoftDebug)
    {
      Debug.DrawLine(LogicPointToVisualPoint(ray.From), LogicPointToVisualPoint(ray.To), actualTargetUsesObliqueHighlight ? Color.magenta : Color.green, 0);
      Debug.DrawLine(LogicPointToVisualPoint(hit.Point), LogicPointToVisualPoint(hit.Point + hit.Normal * 0.35f), Color.yellow, 0);
    }

    return true;
  }

  private bool TryFindObliqueLoftColliderForObject(GameObject obj, out ObliqueLoftCollider collider)
  {
    collider = null;
    if (obj == null)
    {
      return false;
    }

    collider = obj.GetComponent<ObliqueLoftCollider>() ?? obj.GetComponentInChildren<ObliqueLoftCollider>() ?? obj.GetComponentInParent<ObliqueLoftCollider>();
    return collider != null;
  }

  private ObliqueRay CreateDirectObliqueTargetRay(Vector2 gunGround, float gunHeight, Vector2 targetGround, float targetHeight, ObliqueLoftCollider selectedCollider)
  {
    Vector3 from = ObliqueRay.FromGround(gunGround, gunHeight);
    Vector3 aim = ObliqueRay.FromGround(targetGround, targetHeight);
    Vector3 delta = aim - from;
    if (delta.sqrMagnitude <= Mathf.Epsilon)
    {
      return new ObliqueRay(from, aim);
    }

    Bounds bounds = selectedCollider.GetLogicBounds();
    float extension = Mathf.Max(2f, Vector3.Distance(aim, bounds.center) + bounds.extents.magnitude + 0.5f);
    return new ObliqueRay(from, aim + delta.normalized * extension);
  }

  private bool TryGetObliqueAim(ObliqueLoftCollider collider, Vector2 aimPosition, Vector2 gunGround, float gunHeight, out Vector2 targetGround, out float targetHeight)
  {
    targetGround = aimPosition;
    targetHeight = 0f;

    if (TryGetProjectedObliqueFaceAim(collider, aimPosition, gunGround, gunHeight, out targetGround, out targetHeight))
    {
      return true;
    }

    PolygonCollider2D footprintCollider = collider.FootprintCollider;
    if (footprintCollider == null)
    {
      return false;
    }

    Vector2 groundPoint;
    if (!TryGetGroundPointBelowAim(footprintCollider, aimPosition, out groundPoint))
    {
      groundPoint = footprintCollider.ClosestPoint(aimPosition);
    }

    targetGround = groundPoint;
    targetHeight = Mathf.Max(0f, aimPosition.y - groundPoint.y);
    return true;
  }

  private bool HasProjectedObliqueFaceAt(ObliqueLoftCollider collider, Vector2 aimPosition)
  {
    return GetProjectedObliqueFaceAimCandidates(collider, aimPosition, null).Count > 0;
  }

  private bool TryGetProjectedObliqueFaceAim(ObliqueLoftCollider collider, Vector2 aimPosition, Vector2 gunGround, float gunHeight, out Vector2 targetGround, out float targetHeight)
  {
    targetGround = aimPosition;
    targetHeight = 0f;

    Vector3 shooterLogic = ObliqueRay.FromGround(gunGround, gunHeight);
    List<ObliqueFaceAimCandidate> candidates = GetProjectedObliqueFaceAimCandidates(collider, aimPosition, shooterLogic);
    if (candidates.Count == 0)
    {
      return false;
    }

    ObliqueFaceAimCandidate best = candidates
      .OrderByDescending(candidate => candidate.Score)
      .First();

    targetGround = new Vector2(best.LogicPoint.x, best.LogicPoint.z);
    targetHeight = Mathf.Max(0f, best.LogicPoint.y);
    return true;
  }

  private List<ObliqueFaceAimCandidate> GetProjectedObliqueFaceAimCandidates(ObliqueLoftCollider collider, Vector2 aimPosition, Vector3? shooterLogic)
  {
    List<ObliqueFaceAimCandidate> candidates = new List<ObliqueFaceAimCandidate>();

    foreach (ObliqueLoftFace face in collider.GeneratedFaces)
    {
      if (face == null || face.SurfaceType == ObliqueSurfaceType.Bottom || face.Vertices.Count < 3)
      {
        continue;
      }

      Vector3 faceNormal = collider.LocalDirectionToLogicWorld(face.Normal);
      Vector3 a = collider.LocalToLogicWorld(face.Vertices[0]);
      for (int i = 1; i < face.Vertices.Count - 1; i++)
      {
        Vector3 b = collider.LocalToLogicWorld(face.Vertices[i]);
        Vector3 c = collider.LocalToLogicWorld(face.Vertices[i + 1]);
        if (!TryGetProjectedTriangleAim(aimPosition, a, b, c, out Vector3 logicPoint))
        {
          continue;
        }

        candidates.Add(new ObliqueFaceAimCandidate
        {
          LogicPoint = logicPoint,
          Score = ScoreProjectedObliqueFaceAim(logicPoint, faceNormal, shooterLogic)
        });
      }
    }

    return candidates;
  }

  private float ScoreProjectedObliqueFaceAim(Vector3 logicPoint, Vector3 faceNormal, Vector3? shooterLogic)
  {
    if (shooterLogic.HasValue)
    {
      Vector3 toShooter = shooterLogic.Value - logicPoint;
      if (toShooter.sqrMagnitude > Mathf.Epsilon)
      {
        return Vector3.Dot(faceNormal.normalized, toShooter.normalized);
      }
    }

    Vector3 cameraDirection = new Vector3(0f, 1f, -1f).normalized;
    return Vector3.Dot(faceNormal.normalized, cameraDirection);
  }

  private bool TryGetProjectedTriangleAim(Vector2 aimPosition, Vector3 a, Vector3 b, Vector3 c, out Vector3 logicPoint)
  {
    logicPoint = Vector3.zero;
    Vector2 projectedA = LogicPointToVisualPoint(a);
    Vector2 projectedB = LogicPointToVisualPoint(b);
    Vector2 projectedC = LogicPointToVisualPoint(c);

    if (!TryGetBarycentric(aimPosition, projectedA, projectedB, projectedC, out Vector3 barycentric))
    {
      return false;
    }

    logicPoint = a * barycentric.x + b * barycentric.y + c * barycentric.z;
    return true;
  }

  private bool TryGetBarycentric(Vector2 point, Vector2 a, Vector2 b, Vector2 c, out Vector3 barycentric)
  {
    barycentric = Vector3.zero;
    const float epsilon = 0.0001f;
    Vector2 v0 = b - a;
    Vector2 v1 = c - a;
    Vector2 v2 = point - a;
    float dot00 = Vector2.Dot(v0, v0);
    float dot01 = Vector2.Dot(v0, v1);
    float dot02 = Vector2.Dot(v0, v2);
    float dot11 = Vector2.Dot(v1, v1);
    float dot12 = Vector2.Dot(v1, v2);
    float denominator = dot00 * dot11 - dot01 * dot01;
    if (Mathf.Abs(denominator) <= epsilon)
    {
      return false;
    }

    float inverseDenominator = 1f / denominator;
    float v = (dot11 * dot02 - dot01 * dot12) * inverseDenominator;
    float w = (dot00 * dot12 - dot01 * dot02) * inverseDenominator;
    float u = 1f - v - w;
    if (u < -epsilon || v < -epsilon || w < -epsilon)
    {
      return false;
    }

    barycentric = new Vector3(u, v, w);
    return true;
  }

  private bool TryGetGroundPointBelowAim(PolygonCollider2D collider, Vector2 aimPoint, out Vector2 groundPoint)
  {
    groundPoint = Vector2.zero;
    List<float> intersectionsY = new List<float>();
    Vector2[] points = collider.points;
    for (int i = 0; i < points.Length; i++)
    {
      Vector2 a = collider.transform.TransformPoint(points[i]);
      Vector2 b = collider.transform.TransformPoint(points[(i + 1) % points.Length]);
      if (Mathf.Approximately(a.x, b.x) || aimPoint.x < Mathf.Min(a.x, b.x) || aimPoint.x > Mathf.Max(a.x, b.x))
      {
        continue;
      }

      float t = (aimPoint.x - a.x) / (b.x - a.x);
      if (t >= 0f && t <= 1f)
      {
        intersectionsY.Add(Mathf.Lerp(a.y, b.y, t));
      }
    }

    if (intersectionsY.Count == 0)
    {
      return false;
    }

    groundPoint = new Vector2(aimPoint.x, intersectionsY.Min());
    return true;
  }

  private SimpleTarget FindSimpleTargetForObject(GameObject obj)
  {
    if (obj == null)
    {
      return null;
    }

    return obj.GetComponent<SimpleTarget>() ?? obj.GetComponentInChildren<SimpleTarget>() ?? obj.GetComponentInParent<SimpleTarget>();
  }

  private bool IsSelfSimpleTarget(SimpleTarget simpleTarget)
  {
    return simpleTarget != null && (transform.IsChildOf(simpleTarget.transform) || simpleTarget.transform.IsChildOf(transform));
  }

  private float GetGroundLineT(Vector2 from, Vector2 to, Vector2 point)
  {
    Vector2 delta = to - from;
    float sqrLength = delta.sqrMagnitude;
    if (sqrLength <= Mathf.Epsilon)
    {
      return 0f;
    }

    return Vector2.Dot(point - from, delta) / sqrLength;
  }

  private bool BelongsToObject(Transform candidate, GameObject obj)
  {
    if (candidate == null || obj == null)
    {
      return false;
    }

    return candidate.gameObject == obj || candidate.IsChildOf(obj.transform) || obj.transform.IsChildOf(candidate);
  }

  private GameObject ResolveHitObject(ObliqueLoftCollider collider)
  {
    if (collider == null)
    {
      return chosenTarget;
    }

    GameObject hitObject = collider.gameObject;
    if (hitObject.GetComponent<Highlighter>() != null)
    {
      return hitObject;
    }

    Highlighter childHighlighter = hitObject.GetComponentInChildren<Highlighter>();
    if (childHighlighter != null)
    {
      return childHighlighter.gameObject;
    }

    Transform parent = collider.transform.parent;
    while (parent != null)
    {
      if (parent.GetComponent<Highlighter>() != null)
      {
        return parent.gameObject;
      }

      parent = parent.parent;
    }

    parent = collider.transform.parent;
    while (parent != null)
    {
      Highlighter highlighter = parent.GetComponentInChildren<Highlighter>();
      if (highlighter != null)
      {
        return highlighter.gameObject;
      }

      parent = parent.parent;
    }

    return hitObject;
  }

  private Vector2 LogicPointToVisualPoint(Vector3 logicPoint)
  {
    return new Vector2(logicPoint.x, logicPoint.z + logicPoint.y);
  }

  public Vector3 LogicPointToScenePoint(Vector3 logicPoint)
  {
    Vector2 visualPoint = LogicPointToVisualPoint(logicPoint);
    return new Vector3(visualPoint.x, visualPoint.y, transform.position.z);
  }

  private (GameObject obj, Vector2 pos) GetActualTarget()
  {
    Vector2 gunPosition = gunController.GetGun().GetPosition();
    float gunDistance = gunController.GetGunDistance();
    float gunHeight = gunController.GetGunHeight(); ;

    if (TryGetDirectObliqueActualTarget(gunPosition, gunHeight, out GameObject directObliqueTarget, out Vector2 directObliquePosition))
    {
      return (directObliqueTarget, directObliquePosition);
    }

    GameObject depthColliderObject = FindChildWithTag(chosenTarget, "DepthCollider");
    if (depthColliderObject == null || depthColliderObject.GetComponent<PolygonCollider2D>() == null)
    {
      return (chosenTarget, this.position);
    }

    Vector2 targetBottomIntersection = FindVerticalIntersectionPoint(
      depthColliderObject.GetComponent<PolygonCollider2D>(),
      this.position,
      false
    ) ?? Vector2.zero;

    bool vertical = Vector2.Angle(gunPosition - targetBottomIntersection, gunPosition - (gunPosition + new Vector2(0, 100))) <= 2f;

    Vector2 gunGround = new Vector2(gunPosition.x, gunPosition.y - gunHeight);
    float selectedTargetHeight = Mathf.Abs(this.position.y - targetBottomIntersection.y);

    if (TryGetSimpleTargetActualTarget(gunPosition, gunHeight, targetBottomIntersection, selectedTargetHeight, out GameObject simpleTarget, out Vector2 simplePosition))
    {
      return (simpleTarget, simplePosition);
    }

    if (TryGetObliqueActualTarget(gunPosition, gunHeight, targetBottomIntersection, selectedTargetHeight, out GameObject obliqueTarget, out Vector2 obliquePosition))
    {
      return (obliqueTarget, obliquePosition);
    }

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

      float targetHeight = selectedTargetHeight;
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

  private void OnDrawGizmos()
  {
    if (!drawObliqueLoftDebug || !hasObliqueDebugRay)
    {
      return;
    }

    Gizmos.color = lastObliqueDebugBlocked ? Color.red : Color.green;
    Gizmos.DrawLine(LogicPointToScenePoint(lastObliqueDebugRay.From), LogicPointToScenePoint(lastObliqueDebugRay.To));

    if (lastObliqueDebugBlocked)
    {
      Gizmos.DrawSphere(LogicPointToScenePoint(lastObliqueDebugHit.Point), 0.08f);
      Gizmos.DrawLine(
        LogicPointToScenePoint(lastObliqueDebugHit.Point),
        LogicPointToScenePoint(lastObliqueDebugHit.Point + lastObliqueDebugHit.Normal * 0.35f)
      );
      DrawLastObliqueHitFaceGizmo();
    }
  }

  private void DrawLastObliqueHitFaceGizmo()
  {
    ObliqueLoftCollider collider = lastObliqueDebugHit.Collider;
    if (collider == null ||
      lastObliqueDebugHit.FaceIndex < 0 ||
      !TryGetGeneratedFace(collider, lastObliqueDebugHit.FaceIndex, out ObliqueLoftFace face))
    {
      return;
    }

    Gizmos.color = new Color(0.2f, 1f, 0.2f, 1f);
    for (int i = 0; i < face.Vertices.Count; i++)
    {
      Vector3 a = LogicPointToScenePoint(collider.LocalToLogicWorld(face.Vertices[i]));
      Vector3 b = LogicPointToScenePoint(collider.LocalToLogicWorld(face.Vertices[(i + 1) % face.Vertices.Count]));
      Gizmos.DrawLine(a, b);
    }
  }

  private bool TryGetGeneratedFace(ObliqueLoftCollider collider, int faceIndex, out ObliqueLoftFace face)
  {
    foreach (ObliqueLoftFace candidate in collider.GeneratedFaces)
    {
      if (candidate.FaceIndex == faceIndex)
      {
        face = candidate;
        return true;
      }
    }

    face = null;
    return false;
  }
}
