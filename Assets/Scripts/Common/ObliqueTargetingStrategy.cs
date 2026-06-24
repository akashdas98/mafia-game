using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObliqueTargetingStrategyResult
{
  public GameObject ResultObject;
  public Vector2 ResultPosition;
  public bool UseObliqueBlockedHighlight;
  public bool HasDebugRay;
  public ObliqueRay DebugRay;
  public bool DebugBlocked;
  public ObliqueRayHit DebugHit;
  public string DebugStatus;
}

public static class ObliqueTargetingStrategy
{
  private class ObliqueFaceAimCandidate
  {
    public Vector3 LogicPoint;
    public float Score;
  }

  public static bool TryResolveTargetPath(
    bool useObliqueLoftLos,
    bool drawDebug,
    GameObject chosenTarget,
    Vector2 aimPosition,
    Vector2 gunPosition,
    float gunHeight,
    Vector2 targetGround,
    float targetHeight,
    System.Func<Transform, GameObject, bool> belongsToObject,
    System.Func<ObliqueLoftCollider, GameObject> resolveHitObject,
    System.Func<Vector2, bool> isInsideMinimumTargetRadius,
    out ObliqueTargetingStrategyResult result)
  {
    result = NewDefaultResult(chosenTarget, aimPosition);

    if (!useObliqueLoftLos || chosenTarget == null)
    {
      result.HasDebugRay = false;
      result.DebugStatus = "";
      return false;
    }

    Vector2 gunGround = new Vector2(gunPosition.x, gunPosition.y - gunHeight);
    ObliqueRay ray = ObliqueLoftLos.CreateShotRay(gunGround, gunHeight, targetGround, targetHeight);
    PrepareRayDebug(result, ray);

    ObliqueLoftCollider[] allLoftColliders = GetRebuiltLoftColliders();
    int invalidCount = CountInvalid(allLoftColliders);
    List<ObliqueLoftCollider> candidates = allLoftColliders
      .Where(collider => collider != null)
      .Where(collider => collider.UseInRaycasts && collider.IsValid)
      .Where(collider => belongsToObject == null || !belongsToObject(collider.transform, chosenTarget))
      .ToList();

    if (candidates.Count == 0)
    {
      result.DebugStatus = invalidCount > 0
        ? "Oblique clear: no valid obstacle loft colliders (" + invalidCount + " invalid)."
        : "Oblique clear: no obstacle loft colliders.";
      DrawRayIfRequested(drawDebug, ray, Color.green);
      return true;
    }

    result.DebugBlocked = TryRaycastOutsideMinimumTargetRadius(ray, candidates, isInsideMinimumTargetRadius, out ObliqueRayHit hit);
    result.DebugHit = hit;
    result.DebugStatus = result.DebugBlocked
      ? "Oblique blocked by " + hit.HitObject.name + " " + hit.SurfaceType + " face #" + hit.FaceIndex + "."
      : "Oblique clear through " + candidates.Count + " obstacle loft collider(s).";

    if (!result.DebugBlocked)
    {
      DrawRayIfRequested(drawDebug, ray, Color.green);
      return true;
    }

    result.ResultObject = ResolveHitObject(resolveHitObject, hit.Collider, chosenTarget);
    result.ResultPosition = LogicPointToVisualPoint(hit.Point);
    result.UseObliqueBlockedHighlight = true;
    DrawBlockedRayIfRequested(drawDebug, ray, hit);
    return true;
  }

  public static bool TryResolveStaticBlocker(
    bool useObliqueLoftLos,
    bool drawDebug,
    Vector2 gunGround,
    float gunHeight,
    Vector2 targetGround,
    float targetHeight,
    GameObject targetObject,
    System.Func<Transform, GameObject, bool> belongsToObject,
    System.Func<ObliqueLoftCollider, GameObject> resolveHitObject,
    System.Func<Vector2, bool> isInsideMinimumTargetRadius,
    out ObliqueTargetingStrategyResult result)
  {
    result = NewDefaultResult(targetObject, targetGround + Vector2.up * targetHeight);

    if (!useObliqueLoftLos)
    {
      return false;
    }

    ObliqueRay ray = ObliqueLoftLos.CreateShotRay(gunGround, gunHeight, targetGround, targetHeight);
    PrepareRayDebug(result, ray);

    ObliqueLoftCollider[] allLoftColliders = GetRebuiltLoftColliders();
    int invalidCount = CountInvalid(allLoftColliders);
    List<ObliqueLoftCollider> candidates = allLoftColliders
      .Where(collider => collider != null)
      .Where(collider => collider.UseInRaycasts && collider.IsValid)
      .Where(collider => belongsToObject == null || !belongsToObject(collider.transform, targetObject))
      .ToList();

    if (candidates.Count == 0)
    {
      result.DebugStatus = invalidCount > 0
        ? "Oblique clear: no valid static loft blockers (" + invalidCount + " invalid)."
        : "Oblique clear: no static loft blockers.";
      DrawRayIfRequested(drawDebug, ray, Color.green);
      return false;
    }

    result.DebugBlocked = TryRaycastOutsideMinimumTargetRadius(ray, candidates, isInsideMinimumTargetRadius, out ObliqueRayHit hit);
    result.DebugHit = hit;
    result.DebugStatus = result.DebugBlocked
      ? "Oblique blocked by " + hit.HitObject.name + " " + hit.SurfaceType + " face #" + hit.FaceIndex + "."
      : "Oblique clear through " + candidates.Count + " static loft blocker(s).";

    if (!result.DebugBlocked)
    {
      DrawRayIfRequested(drawDebug, ray, Color.green);
      return false;
    }

    result.ResultObject = ResolveHitObject(resolveHitObject, hit.Collider, targetObject);
    result.ResultPosition = LogicPointToVisualPoint(hit.Point);
    result.UseObliqueBlockedHighlight = true;
    DrawBlockedRayIfRequested(drawDebug, ray, hit);
    return true;
  }

  public static bool TryResolveDirectTarget(
    bool useObliqueLoftLos,
    bool drawDebug,
    GameObject chosenTarget,
    Vector2 aimPosition,
    Vector2 gunPosition,
    float gunHeight,
    System.Func<Transform, GameObject, bool> belongsToObject,
    System.Func<ObliqueLoftCollider, GameObject> resolveHitObject,
    System.Func<Vector2, bool> isInsideMinimumTargetRadius,
    out ObliqueTargetingStrategyResult result)
  {
    result = NewDefaultResult(chosenTarget, aimPosition);

    if (!TryGetSelectedAim(useObliqueLoftLos, chosenTarget, aimPosition, gunPosition, gunHeight, out Vector2 targetGround, out float targetHeight, out ObliqueLoftCollider selectedCollider))
    {
      return false;
    }

    Vector2 gunGround = new Vector2(gunPosition.x, gunPosition.y - gunHeight);
    ObliqueRay ray = CreateDirectTargetRay(gunGround, gunHeight, targetGround, targetHeight, selectedCollider);
    PrepareRayDebug(result, ray);

    List<ObliqueLoftCollider> candidates = GetRebuiltLoftColliders()
      .Where(collider => collider != null)
      .Where(collider => collider.UseInRaycasts && collider.IsValid)
      .ToList();

    if (!TryRaycastOutsideMinimumTargetRadius(ray, candidates, isInsideMinimumTargetRadius, out ObliqueRayHit hit))
    {
      result.DebugStatus = "Oblique target clear: no loft face hit along extended target ray.";
      DrawRayIfRequested(drawDebug, ray, Color.green);
      return true;
    }

    result.DebugBlocked = true;
    result.DebugHit = hit;
    result.ResultObject = ResolveHitObject(resolveHitObject, hit.Collider, chosenTarget);
    result.ResultPosition = LogicPointToVisualPoint(hit.Point);
    result.UseObliqueBlockedHighlight = belongsToObject != null && !belongsToObject(hit.Collider.transform, chosenTarget);
    result.DebugStatus = result.UseObliqueBlockedHighlight
      ? "Oblique blocked by " + hit.HitObject.name + " before selected loft target."
      : "Oblique target hit: " + hit.HitObject.name + " " + hit.SurfaceType + " face #" + hit.FaceIndex + ".";

    if (drawDebug)
    {
      Debug.DrawLine(LogicPointToVisualPoint(ray.From), LogicPointToVisualPoint(ray.To), result.UseObliqueBlockedHighlight ? Color.magenta : Color.green, 0);
      DrawHitNormal(hit);
    }

    return true;
  }

  public static bool TryGetSelectedAim(
    bool useObliqueLoftLos,
    GameObject chosenTarget,
    Vector2 aimPosition,
    Vector2 gunPosition,
    float gunHeight,
    out Vector2 targetGround,
    out float targetHeight,
    out ObliqueLoftCollider selectedCollider)
  {
    targetGround = aimPosition;
    targetHeight = 0f;
    selectedCollider = null;

    if (!useObliqueLoftLos || chosenTarget == null || !TryFindObliqueLoftColliderForObject(chosenTarget, out selectedCollider))
    {
      return false;
    }

    selectedCollider.Rebuild();
    Vector2 gunGround = new Vector2(gunPosition.x, gunPosition.y - gunHeight);
    return selectedCollider.IsValid && TryGetAim(selectedCollider, aimPosition, gunGround, gunHeight, out targetGround, out targetHeight);
  }

  public static bool HasProjectedFaceAt(ObliqueLoftCollider collider, Vector2 aimPosition)
  {
    return GetProjectedFaceAimCandidates(collider, aimPosition, null).Count > 0;
  }

  private static ObliqueTargetingStrategyResult NewDefaultResult(GameObject resultObject, Vector2 resultPosition)
  {
    return new ObliqueTargetingStrategyResult
    {
      ResultObject = resultObject,
      ResultPosition = resultPosition,
      UseObliqueBlockedHighlight = false,
      HasDebugRay = false,
      DebugBlocked = false,
      DebugHit = default(ObliqueRayHit),
      DebugStatus = string.Empty
    };
  }

  private static void PrepareRayDebug(ObliqueTargetingStrategyResult result, ObliqueRay ray)
  {
    result.HasDebugRay = true;
    result.DebugRay = ray;
    result.DebugBlocked = false;
    result.DebugHit = default(ObliqueRayHit);
  }

  private static ObliqueLoftCollider[] GetRebuiltLoftColliders()
  {
    ObliqueLoftCollider[] allLoftColliders = Object.FindObjectsOfType<ObliqueLoftCollider>();
    foreach (ObliqueLoftCollider collider in allLoftColliders)
    {
      collider?.Rebuild();
    }

    return allLoftColliders;
  }

  private static int CountInvalid(IEnumerable<ObliqueLoftCollider> colliders)
  {
    return colliders.Count(collider => collider != null && !collider.IsValid);
  }

  private static bool TryFindObliqueLoftColliderForObject(GameObject obj, out ObliqueLoftCollider collider)
  {
    collider = null;
    if (obj == null)
    {
      return false;
    }

    collider = obj.GetComponent<ObliqueLoftCollider>() ?? obj.GetComponentInChildren<ObliqueLoftCollider>() ?? obj.GetComponentInParent<ObliqueLoftCollider>();
    return collider != null;
  }

  private static ObliqueRay CreateDirectTargetRay(Vector2 gunGround, float gunHeight, Vector2 targetGround, float targetHeight, ObliqueLoftCollider selectedCollider)
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

  private static bool TryGetAim(ObliqueLoftCollider collider, Vector2 aimPosition, Vector2 gunGround, float gunHeight, out Vector2 targetGround, out float targetHeight)
  {
    targetGround = aimPosition;
    targetHeight = 0f;

    if (TryGetProjectedFaceAim(collider, aimPosition, gunGround, gunHeight, out targetGround, out targetHeight))
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

  private static bool TryGetProjectedFaceAim(ObliqueLoftCollider collider, Vector2 aimPosition, Vector2 gunGround, float gunHeight, out Vector2 targetGround, out float targetHeight)
  {
    targetGround = aimPosition;
    targetHeight = 0f;

    Vector3 shooterLogic = ObliqueRay.FromGround(gunGround, gunHeight);
    List<ObliqueFaceAimCandidate> candidates = GetProjectedFaceAimCandidates(collider, aimPosition, shooterLogic);
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

  private static List<ObliqueFaceAimCandidate> GetProjectedFaceAimCandidates(ObliqueLoftCollider collider, Vector2 aimPosition, Vector3? shooterLogic)
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
          Score = ScoreProjectedFaceAim(logicPoint, faceNormal, shooterLogic)
        });
      }
    }

    return candidates;
  }

  private static float ScoreProjectedFaceAim(Vector3 logicPoint, Vector3 faceNormal, Vector3? shooterLogic)
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

  private static bool TryGetProjectedTriangleAim(Vector2 aimPosition, Vector3 a, Vector3 b, Vector3 c, out Vector3 logicPoint)
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

  private static bool TryGetBarycentric(Vector2 point, Vector2 a, Vector2 b, Vector2 c, out Vector3 barycentric)
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

  private static bool TryGetGroundPointBelowAim(PolygonCollider2D collider, Vector2 aimPoint, out Vector2 groundPoint)
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

  private static bool TryRaycastOutsideMinimumTargetRadius(
    ObliqueRay ray,
    IEnumerable<ObliqueLoftCollider> colliders,
    System.Func<Vector2, bool> isInsideMinimumTargetRadius,
    out ObliqueRayHit closestHit)
  {
    closestHit = default(ObliqueRayHit);
    bool hasHit = false;
    float closestDistance = float.PositiveInfinity;

    if (ray.Length <= Mathf.Epsilon || colliders == null)
    {
      return false;
    }

    foreach (ObliqueLoftCollider collider in colliders)
    {
      if (collider == null || !collider.isActiveAndEnabled || !collider.UseInRaycasts || !collider.ProjectedBoundsIntersects(ray))
      {
        continue;
      }

      foreach (ObliqueLoftFace face in collider.GeneratedFaces)
      {
        if (!ObliqueRaycaster.TryIntersectFace(ray, collider, face, out ObliqueRayHit hit) ||
          hit.Distance >= closestDistance ||
          (isInsideMinimumTargetRadius != null && isInsideMinimumTargetRadius(LogicPointToVisualPoint(hit.Point))))
        {
          continue;
        }

        closestDistance = hit.Distance;
        closestHit = hit;
        hasHit = true;
      }
    }

    return hasHit;
  }

  private static GameObject ResolveHitObject(System.Func<ObliqueLoftCollider, GameObject> resolveHitObject, ObliqueLoftCollider collider, GameObject fallback)
  {
    if (resolveHitObject == null)
    {
      return collider != null ? collider.gameObject : fallback;
    }

    return resolveHitObject(collider);
  }

  private static Vector2 LogicPointToVisualPoint(Vector3 logicPoint)
  {
    return new Vector2(logicPoint.x, logicPoint.z + logicPoint.y);
  }

  private static void DrawRayIfRequested(bool drawDebug, ObliqueRay ray, Color color)
  {
    if (drawDebug)
    {
      Debug.DrawLine(LogicPointToVisualPoint(ray.From), LogicPointToVisualPoint(ray.To), color, 0);
    }
  }

  private static void DrawBlockedRayIfRequested(bool drawDebug, ObliqueRay ray, ObliqueRayHit hit)
  {
    if (!drawDebug)
    {
      return;
    }

    Debug.DrawLine(LogicPointToVisualPoint(ray.From), LogicPointToVisualPoint(ray.To), Color.magenta, 0);
    DrawHitNormal(hit);
  }

  private static void DrawHitNormal(ObliqueRayHit hit)
  {
    Debug.DrawLine(LogicPointToVisualPoint(hit.Point), LogicPointToVisualPoint(hit.Point + hit.Normal * 0.35f), Color.yellow, 0);
  }
}
