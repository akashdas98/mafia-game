using UnityEngine;

public enum TargetingLosMode
{
  None,
  SimpleTarget,
  ObliqueLoft,
  LegacyDepthCollider
}

public class TargetingResult
{
  public GameObject IntendedTarget { get; private set; }
  public GameObject ActualTarget { get; private set; }
  public GameObject Blocker { get; private set; }
  public Vector2 IntendedPoint { get; private set; }
  public Vector2 ActualPoint { get; private set; }
  public Vector2 GroundPoint { get; private set; }
  public float TargetHeight { get; private set; }
  public TargetingLosMode LosMode { get; private set; }
  public string DebugStatus { get; private set; }
  public bool UseObliqueBlockedHighlight { get; private set; }

  public static TargetingResult Resolved(
    GameObject intendedTarget,
    Vector2 intendedPoint,
    GameObject actualTarget,
    Vector2 actualPoint,
    TargetingLosMode losMode,
    bool useObliqueBlockedHighlight,
    string debugStatus)
  {
    return new TargetingResult
    {
      IntendedTarget = intendedTarget,
      IntendedPoint = intendedPoint,
      ActualTarget = actualTarget,
      ActualPoint = actualPoint,
      Blocker = actualTarget != intendedTarget ? actualTarget : null,
      GroundPoint = actualPoint,
      TargetHeight = Mathf.Max(0f, actualPoint.y - intendedPoint.y),
      LosMode = losMode,
      UseObliqueBlockedHighlight = useObliqueBlockedHighlight,
      DebugStatus = debugStatus ?? string.Empty
    };
  }
}
