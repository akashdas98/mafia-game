using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public delegate bool StaticTargetBlockerResolver(
  Vector2 gunGround,
  float gunHeight,
  Vector2 targetGround,
  float targetHeight,
  GameObject targetObject,
  out GameObject resultObject,
  out Vector2 resultPosition);

public class SimpleTargetingStrategyResult
{
  public GameObject ResultObject;
  public Vector2 ResultPosition;
  public bool UseObliqueBlockedHighlight;
  public string DebugStatus;
}

public static class SimpleTargetingStrategy
{
  private class SimpleTargetCandidate
  {
    public SimpleTarget Target;
    public GameObject TargetObject;
    public Vector2 GroundPoint;
    public Vector2 HitPoint;
    public float TargetHeight;
    public float Distance;
  }

  public static bool TryResolve(
    bool useSimpleTargeting,
    GameObject chosenTarget,
    Vector2 aimPosition,
    Vector2 gunPosition,
    float gunHeight,
    Vector2 intendedGround,
    float intendedTargetHeight,
    StaticTargetBlockerResolver tryResolveStaticBlocker,
    System.Func<SimpleTarget, bool> isSelfSimpleTarget,
    System.Func<Transform, GameObject, bool> belongsToObject,
    System.Func<Vector2, bool> isInsideMinimumTargetRadius,
    out SimpleTargetingStrategyResult result)
  {
    result = null;

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
      chosenTarget,
      aimPosition,
      gunPosition,
      gunGround,
      intendedGround,
      intendedVisualPoint,
      isSelfSimpleTarget,
      belongsToObject,
      isInsideMinimumTargetRadius
    );

    if (candidates.Count == 0)
    {
      if (chosenSimpleTarget == null)
      {
        return false;
      }

      return TryResolveStaticBlockerOnly(
        tryResolveStaticBlocker,
        gunGround,
        gunHeight,
        intendedGround,
        intendedTargetHeight,
        chosenTarget,
        out result
      );
    }

    foreach (SimpleTargetCandidate candidate in candidates.OrderBy(candidate => candidate.Distance))
    {
      if (tryResolveStaticBlocker != null &&
        tryResolveStaticBlocker(gunGround, gunHeight, candidate.GroundPoint, candidate.TargetHeight, candidate.TargetObject, out GameObject blockedObject, out Vector2 blockedPosition))
      {
        result = new SimpleTargetingStrategyResult
        {
          ResultObject = blockedObject,
          ResultPosition = blockedPosition,
          UseObliqueBlockedHighlight = blockedObject != candidate.TargetObject
        };
        return true;
      }

      result = new SimpleTargetingStrategyResult
      {
        ResultObject = candidate.TargetObject,
        ResultPosition = candidate.HitPoint,
        UseObliqueBlockedHighlight = false,
        DebugStatus = "Simple target hit: " + candidate.TargetObject.name + "."
      };
      return true;
    }

    return TryResolveStaticBlockerOnly(
      tryResolveStaticBlocker,
      gunGround,
      gunHeight,
      intendedGround,
      intendedTargetHeight,
      chosenTarget,
      out result
    );
  }

  private static bool TryResolveStaticBlockerOnly(
    StaticTargetBlockerResolver tryResolveStaticBlocker,
    Vector2 gunGround,
    float gunHeight,
    Vector2 targetGround,
    float targetHeight,
    GameObject targetObject,
    out SimpleTargetingStrategyResult result)
  {
    result = null;
    if (tryResolveStaticBlocker == null ||
      !tryResolveStaticBlocker(gunGround, gunHeight, targetGround, targetHeight, targetObject, out GameObject resultObject, out Vector2 resultPosition))
    {
      return false;
    }

    result = new SimpleTargetingStrategyResult
    {
      ResultObject = resultObject,
      ResultPosition = resultPosition,
      UseObliqueBlockedHighlight = resultObject != targetObject
    };
    return true;
  }

  private static List<SimpleTargetCandidate> BuildSimpleTargetCandidates(
    SimpleTarget[] simpleTargets,
    SimpleTarget chosenSimpleTarget,
    GameObject chosenTarget,
    Vector2 aimPosition,
    Vector2 gunPosition,
    Vector2 gunGround,
    Vector2 intendedGround,
    Vector2 intendedVisualPoint,
    System.Func<SimpleTarget, bool> isSelfSimpleTarget,
    System.Func<Transform, GameObject, bool> belongsToObject,
    System.Func<Vector2, bool> isInsideMinimumTargetRadius)
  {
    List<SimpleTargetCandidate> candidates = new List<SimpleTargetCandidate>();

    foreach (SimpleTarget simpleTarget in simpleTargets)
    {
      if (simpleTarget == null ||
        !simpleTarget.UseInTargeting ||
        !simpleTarget.IsValid ||
        (isSelfSimpleTarget != null && isSelfSimpleTarget(simpleTarget)))
      {
        continue;
      }

      bool isChosen = simpleTarget == chosenSimpleTarget ||
        (belongsToObject != null && belongsToObject(simpleTarget.transform, chosenTarget));
      Vector2 groundPoint;
      Vector2 hitPoint;

      if (isChosen)
      {
        hitPoint = aimPosition;
        if (!simpleTarget.ContainsHitPoint(hitPoint) &&
          !simpleTarget.TryGetFirstHitPolygonIntersection(gunPosition, intendedVisualPoint, out hitPoint))
        {
          continue;
        }
      }
      else if (!simpleTarget.TryGetFirstHitPolygonIntersection(gunPosition, intendedVisualPoint, out hitPoint))
      {
        continue;
      }

      if (!simpleTarget.TryGetGroundBaselineIntersection(gunGround, intendedGround, out groundPoint))
      {
        continue;
      }

      candidates.Add(new SimpleTargetCandidate
      {
        Target = simpleTarget,
        TargetObject = simpleTarget.GetTargetObject(),
        GroundPoint = groundPoint,
        HitPoint = hitPoint,
        TargetHeight = Mathf.Max(0f, hitPoint.y - groundPoint.y),
        Distance = Vector2.Distance(gunGround, groundPoint)
      });
    }

    return candidates
      .Where(candidate => isInsideMinimumTargetRadius == null || !isInsideMinimumTargetRadius(candidate.HitPoint))
      .ToList();
  }

  private static SimpleTarget FindSimpleTargetForObject(GameObject obj)
  {
    if (obj == null)
    {
      return null;
    }

    SimpleTarget simpleTarget = obj.GetComponent<SimpleTarget>() ?? obj.GetComponentInChildren<SimpleTarget>() ?? obj.GetComponentInParent<SimpleTarget>();
    if (simpleTarget != null)
    {
      return simpleTarget;
    }

    Transform parent = obj.transform.parent;
    while (parent != null)
    {
      simpleTarget = parent.GetComponent<SimpleTarget>() ?? parent.GetComponentInChildren<SimpleTarget>();
      if (simpleTarget != null)
      {
        return simpleTarget;
      }

      parent = parent.parent;
    }

    return null;
  }
}
