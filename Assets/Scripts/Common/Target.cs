using UnityEngine;

public class Target : MonoBehaviour
{
  [SerializeField] private SpriteRenderer sprite;
  [Header("Aim Points")]
  [Tooltip("Stable world point used as the logical shot-line origin. Leave blank to use the controller gun-height fallback.")]
  [SerializeField] private Transform aimOrigin;
  [Tooltip("Animation-frame-specific visual gun point. The equipped gun is rendered here, while logic still uses Aim Origin. Leave blank to use the old origin-to-target distance fallback.")]
  [SerializeField] private Transform gunPoint;
  [Header("Testing")]
  [Tooltip("Allows aiming and target selection without an equipped gun. Trigger pulls still require an equipped gun.")]
  [SerializeField] private bool allowTargetingWithoutEquippedItem = false;
  [Header("Oblique Loft LOS")]
  [SerializeField] private bool useSimpleTargeting = true;
  [SerializeField] private bool useObliqueLoftLos = false;
  [SerializeField] private bool drawObliqueLoftDebug = true;

  private bool enabled = false;
  private WeaponUser weaponUser;
  private GameObject? chosenTarget, actualTarget;
  private Vector2 position, actualPosition;
  private bool hasObliqueDebugRay;
  private ObliqueRay lastObliqueDebugRay;
  private bool lastObliqueDebugBlocked;
  private ObliqueRayHit lastObliqueDebugHit;
  private string lastObliqueDebugStatus = "";
  private bool actualTargetUsesObliqueHighlight;
  private TargetingLosMode lastTargetingLosMode = TargetingLosMode.None;

  public bool DrawObliqueLoftDebug => drawObliqueLoftDebug;
  public bool HasObliqueDebugRay => hasObliqueDebugRay;
  public ObliqueRay LastObliqueDebugRay => lastObliqueDebugRay;
  public bool LastObliqueDebugBlocked => lastObliqueDebugBlocked;
  public ObliqueRayHit LastObliqueDebugHit => lastObliqueDebugHit;
  public string LastObliqueDebugStatus => lastObliqueDebugStatus;
  public bool AllowTargetingWithoutEquippedItem => allowTargetingWithoutEquippedItem;
  public bool HasAimOrigin => aimOrigin != null;
  public bool HasGunPoint => gunPoint != null;
  public bool HasActiveAim => enabled;

  public void Initialize(WeaponUser weaponUser)
  {
    this.weaponUser = weaponUser;
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
    if (weaponUser != null && weaponUser.IsInsideMinimumTargetRadius(position))
    {
      Reset();
      return;
    }

    this.enabled = true;
    this.position = position;

    if (this.actualTarget)
    {
      ResolveHighlighter(this.actualTarget)?.ResetHighlight();
    }
    this.actualTargetUsesObliqueHighlight = false;
    this.lastTargetingLosMode = TargetingLosMode.None;

    this.chosenTarget = GetChosenTarget();
    if (this.chosenTarget != null)
    {
      TargetingResult res = GetActualTargetResult();

      this.actualTarget = res.ActualTarget;
      this.actualPosition = res.ActualPoint;
      this.actualTargetUsesObliqueHighlight = res.UseObliqueBlockedHighlight;
      Highlighter highlighter = ResolveHighlighter(this.actualTarget);
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
      ResolveHighlighter(this.actualTarget)?.ResetHighlight();
    }
    this.chosenTarget = null;
    this.actualTarget = null;
    this.hasObliqueDebugRay = false;
    this.actualTargetUsesObliqueHighlight = false;
    this.lastTargetingLosMode = TargetingLosMode.None;
  }

  public Vector2 GetAimOriginPosition()
  {
    return aimOrigin != null ? (Vector2)aimOrigin.position : Vector2.zero;
  }

  public Vector2 GetGunPointPosition()
  {
    return gunPoint != null ? (Vector2)gunPoint.position : Vector2.zero;
  }


  private void SetVisibility(bool isVisible)
  {
    if (sprite != null)
    {
      sprite.enabled = isVisible;
    }
  }

  private Highlighter ResolveHighlighter(GameObject obj)
  {
    if (obj == null)
    {
      return null;
    }

    SimpleTarget simpleTarget = FindSimpleTargetForObject(obj);
    if (simpleTarget != null)
    {
      Highlighter simpleTargetHighlighter = simpleTarget.GetComponent<Highlighter>() ??
        simpleTarget.GetComponentInChildren<Highlighter>() ??
        simpleTarget.GetComponentInParent<Highlighter>();
      if (simpleTargetHighlighter != null)
      {
        return simpleTargetHighlighter;
      }
    }

    Highlighter highlighter = obj.GetComponent<Highlighter>() ?? obj.GetComponentInChildren<Highlighter>();
    if (highlighter != null)
    {
      return highlighter;
    }

    Transform parent = obj.transform.parent;
    while (parent != null)
    {
      highlighter = parent.GetComponent<Highlighter>() ?? parent.GetComponentInChildren<Highlighter>();
      if (highlighter != null)
      {
        return highlighter;
      }

      parent = parent.parent;
    }

    return null;
  }

  private GameObject? GetChosenTarget()
  {
    return TargetSelectionResolver.GetChosenTarget(this.position, HasProjectedObliqueFaceAt);
  }

  private bool TryGetObliqueActualTarget(Vector2 gunPosition, float gunHeight, Vector2 targetGround, float targetHeight, out GameObject resultObject, out Vector2 resultPosition)
  {
    resultObject = chosenTarget;
    resultPosition = this.position;

    if (!ObliqueTargetingStrategy.TryResolveTargetPath(
      useObliqueLoftLos,
      drawObliqueLoftDebug,
      chosenTarget,
      this.position,
      gunPosition,
      gunHeight,
      targetGround,
      targetHeight,
      BelongsToObject,
      ResolveHitObject,
      IsInsideMinimumTargetRadius,
      out ObliqueTargetingStrategyResult result))
    {
      ApplyObliqueStrategyResult(result);
      return false;
    }

    ApplyObliqueStrategyResult(result);
    resultObject = result.ResultObject;
    resultPosition = result.ResultPosition;
    actualTargetUsesObliqueHighlight = result.UseObliqueBlockedHighlight;
    lastTargetingLosMode = TargetingLosMode.ObliqueLoft;
    return true;
  }

  private bool TryGetSimpleTargetActualTarget(Vector2 gunPosition, float gunHeight, Vector2 intendedGround, float intendedTargetHeight, out GameObject resultObject, out Vector2 resultPosition)
  {
    resultObject = chosenTarget;
    resultPosition = this.position;

    if (!SimpleTargetingStrategy.TryResolve(
      useSimpleTargeting,
      chosenTarget,
      this.position,
      gunPosition,
      gunHeight,
      intendedGround,
      intendedTargetHeight,
      TryGetStaticObliqueActualTarget,
      IsSelfSimpleTarget,
      BelongsToObject,
      IsInsideMinimumTargetRadius,
      out SimpleTargetingStrategyResult result))
    {
      return false;
    }

    resultObject = result.ResultObject;
    resultPosition = result.ResultPosition;
    actualTargetUsesObliqueHighlight = result.UseObliqueBlockedHighlight;
    lastTargetingLosMode = result.UseObliqueBlockedHighlight ? TargetingLosMode.ObliqueLoft : TargetingLosMode.SimpleTarget;
    if (!string.IsNullOrEmpty(result.DebugStatus))
    {
      lastObliqueDebugStatus = result.DebugStatus;
    }
    return true;
  }

  private bool TryGetStaticObliqueActualTarget(Vector2 gunGround, float gunHeight, Vector2 targetGround, float targetHeight, GameObject targetObject, out GameObject resultObject, out Vector2 resultPosition)
  {
    resultObject = targetObject;
    resultPosition = targetGround + Vector2.up * targetHeight;

    if (!ObliqueTargetingStrategy.TryResolveStaticBlocker(
      useObliqueLoftLos,
      drawObliqueLoftDebug,
      gunGround,
      gunHeight,
      targetGround,
      targetHeight,
      targetObject,
      BelongsToObject,
      ResolveHitObject,
      IsInsideMinimumTargetRadius,
      out ObliqueTargetingStrategyResult result))
    {
      ApplyObliqueStrategyResult(result);
      return false;
    }

    ApplyObliqueStrategyResult(result);
    resultObject = result.ResultObject;
    resultPosition = result.ResultPosition;
    lastTargetingLosMode = TargetingLosMode.ObliqueLoft;
    return true;
  }

  private bool TryGetDirectObliqueActualTarget(Vector2 gunPosition, float gunHeight, out GameObject resultObject, out Vector2 resultPosition)
  {
    resultObject = chosenTarget;
    resultPosition = this.position;

    if (!ObliqueTargetingStrategy.TryResolveDirectTarget(
      useObliqueLoftLos,
      drawObliqueLoftDebug,
      chosenTarget,
      this.position,
      gunPosition,
      gunHeight,
      BelongsToObject,
      ResolveHitObject,
      IsInsideMinimumTargetRadius,
      out ObliqueTargetingStrategyResult result))
    {
      ApplyObliqueStrategyResult(result);
      return false;
    }

    ApplyObliqueStrategyResult(result);
    resultObject = result.ResultObject;
    resultPosition = result.ResultPosition;
    actualTargetUsesObliqueHighlight = result.UseObliqueBlockedHighlight;
    lastTargetingLosMode = TargetingLosMode.ObliqueLoft;
    return true;
  }

  private bool TryGetSelectedObliqueAim(Vector2 gunPosition, float gunHeight, out Vector2 targetGround, out float targetHeight, out ObliqueLoftCollider selectedCollider)
  {
    return ObliqueTargetingStrategy.TryGetSelectedAim(
      useObliqueLoftLos,
      chosenTarget,
      this.position,
      gunPosition,
      gunHeight,
      out targetGround,
      out targetHeight,
      out selectedCollider);
  }

  private void ApplyObliqueStrategyResult(ObliqueTargetingStrategyResult result)
  {
    if (result == null)
    {
      hasObliqueDebugRay = false;
      lastObliqueDebugStatus = "";
      return;
    }

    hasObliqueDebugRay = result.HasDebugRay;
    lastObliqueDebugRay = result.DebugRay;
    lastObliqueDebugBlocked = result.DebugBlocked;
    lastObliqueDebugHit = result.DebugHit;
    lastObliqueDebugStatus = result.DebugStatus ?? "";
  }

  private bool HasProjectedObliqueFaceAt(ObliqueLoftCollider collider, Vector2 aimPosition)
  {
    return ObliqueTargetingStrategy.HasProjectedFaceAt(collider, aimPosition);
  }

  private SimpleTarget FindSimpleTargetForObject(GameObject obj)
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

  private bool IsSelfSimpleTarget(SimpleTarget simpleTarget)
  {
    return simpleTarget != null && (transform.IsChildOf(simpleTarget.transform) || simpleTarget.transform.IsChildOf(transform));
  }

  private bool IsInsideMinimumTargetRadius(Vector2 point)
  {
    return weaponUser != null && weaponUser.IsInsideMinimumTargetRadius(point);
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
    if (weaponUser == null)
    {
      return (chosenTarget, this.position);
    }

    Vector2 gunPosition = weaponUser.GetAimOriginPosition();
    float gunHeight = weaponUser.GetGunHeight();

    if (TryGetSelectedObliqueAim(gunPosition, gunHeight, out Vector2 obliqueTargetGround, out float obliqueTargetHeight, out ObliqueLoftCollider _))
    {
      if (TryGetSimpleTargetActualTarget(gunPosition, gunHeight, obliqueTargetGround, obliqueTargetHeight, out GameObject obliqueLineSimpleTarget, out Vector2 obliqueLineSimplePosition))
      {
        return (obliqueLineSimpleTarget, obliqueLineSimplePosition);
      }
    }

    if (TryGetDirectObliqueActualTarget(gunPosition, gunHeight, out GameObject directObliqueTarget, out Vector2 directObliquePosition))
    {
      return (directObliqueTarget, directObliquePosition);
    }

    SimpleTarget selectedSimpleTarget = FindSimpleTargetForObject(chosenTarget);
    if (selectedSimpleTarget != null &&
      selectedSimpleTarget.UseInTargeting &&
      selectedSimpleTarget.IsValid &&
      selectedSimpleTarget.TryGetGroundPointBelowAim(this.position, out Vector2 simpleTargetGround))
    {
      float simpleTargetHeight = Mathf.Max(0f, this.position.y - simpleTargetGround.y);
      if (TryGetSimpleTargetActualTarget(gunPosition, gunHeight, simpleTargetGround, simpleTargetHeight, out GameObject directSimpleTarget, out Vector2 directSimplePosition))
      {
        return (directSimpleTarget, directSimplePosition);
      }
    }

    if (LegacyDepthTargetingStrategy.TryResolve(
      chosenTarget,
      this.position,
      gunPosition,
      gunHeight,
      TryGetSimpleTargetActualTarget,
      TryGetObliqueActualTarget,
      IsInsideMinimumTargetRadius,
      out LegacyDepthTargetingStrategyResult legacyResult))
    {
      if (lastTargetingLosMode == TargetingLosMode.None)
      {
        lastTargetingLosMode = TargetingLosMode.LegacyDepthCollider;
      }

      return (legacyResult.ResultObject, legacyResult.ResultPosition);
    }

    return (chosenTarget, this.position);
  }

  private TargetingResult GetActualTargetResult()
  {
    lastTargetingLosMode = TargetingLosMode.None;
    (GameObject obj, Vector2 pos) result = GetActualTarget();
    TargetingLosMode mode = lastTargetingLosMode;

    if (result.obj == chosenTarget && result.pos == this.position)
    {
      mode = TargetingLosMode.None;
    }

    return TargetingResult.Resolved(
      chosenTarget,
      this.position,
      result.obj,
      result.pos,
      mode,
      actualTargetUsesObliqueHighlight,
      lastObliqueDebugStatus
    );
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
      if (sprite != null)
      {
        sprite.transform.position = this.actualPosition;
      }
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
