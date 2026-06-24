using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class SimpleTarget : MonoBehaviour
{
  [SerializeField] private bool useInTargeting = true;
  [Tooltip("Local Y position on the HitCollider used as the SimpleTarget ground reference. The line spans the hit polygon's full world-space width.")]
  [SerializeField] private float groundLineLocalY = -0.2f;
  [SerializeField] private bool applyLayerProfilesAutomatically = true;
  [SerializeField] private Transform spritesRoot;
  [SerializeField] private SpriteRenderer[] excludedShapeRenderers;
  [SerializeField] private PolygonCollider2D hitCollider;
  private string lastAppliedLayerProfileKey;

  public bool UseInTargeting => useInTargeting;
  public bool ApplyLayerProfilesAutomatically => applyLayerProfilesAutomatically;
  public Transform SpritesRoot => ResolveSpritesRoot();
  public PolygonCollider2D HitCollider => ResolveHitCollider();

  public bool IsValid => TryGetHitPaths(out List<Vector2[]> paths) && paths.Count > 0;

  public bool TryGetGroundPointBelowAim(Vector2 aimPoint, out Vector2 groundPoint)
  {
    groundPoint = Vector2.zero;
    if (!TryGetGroundBaseline(out Vector2 left, out Vector2 right))
    {
      return false;
    }

    if (!IsBetween(aimPoint.x, left.x, right.x))
    {
      return false;
    }

    groundPoint = new Vector2(aimPoint.x, left.y);
    return true;
  }

  public bool TryGetGroundBaselineIntersection(Vector2 fromGround, Vector2 toGround, out Vector2 groundPoint)
  {
    groundPoint = Vector2.zero;
    if (!TryGetGroundBaseline(out Vector2 left, out Vector2 right))
    {
      return false;
    }

    if (Utilities.LinesIntersect(fromGround, toGround, left, right, out Vector2? hit) && hit != null)
    {
      groundPoint = hit.Value;
      return true;
    }

    return false;
  }

  public bool TryGetGroundBaseline(out Vector2 left, out Vector2 right)
  {
    left = Vector2.zero;
    right = Vector2.zero;
    if (!TryGetHitPaths(out List<Vector2[]> paths) || paths.Count == 0)
    {
      return false;
    }

    bool hasPoint = false;
    float minX = 0f;
    float maxX = 0f;
    for (int pathIndex = 0; pathIndex < paths.Count; pathIndex++)
    {
      Vector2[] points = paths[pathIndex];
      for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
      {
        Vector2 point = points[pointIndex];
        if (!hasPoint)
        {
          minX = point.x;
          maxX = point.x;
          hasPoint = true;
        }
        else
        {
          minX = Mathf.Min(minX, point.x);
          maxX = Mathf.Max(maxX, point.x);
        }
      }
    }

    if (!hasPoint)
    {
      return false;
    }

    float groundY = GetGroundLineWorldY();
    left = new Vector2(minX, groundY);
    right = new Vector2(maxX, groundY);
    return !Mathf.Approximately(minX, maxX);
  }

  public bool ContainsHitPoint(Vector2 point)
  {
    if (!TryGetHitPaths(out List<Vector2[]> paths))
    {
      return false;
    }

    for (int i = 0; i < paths.Count; i++)
    {
      if (IsPointInPolygon(point, paths[i]))
      {
        return true;
      }
    }

    return false;
  }

  public bool TryGetFirstHitPolygonIntersection(Vector2 fromVisual, Vector2 toVisual, out Vector2 intersection)
  {
    intersection = Vector2.zero;
    if (!TryGetHitPaths(out List<Vector2[]> paths))
    {
      return false;
    }

    List<Vector2> intersections = new List<Vector2>();
    for (int pathIndex = 0; pathIndex < paths.Count; pathIndex++)
    {
      Vector2[] points = paths[pathIndex];
      for (int i = 0; i < points.Length; i++)
      {
        Vector2 a = points[i];
        Vector2 b = points[(i + 1) % points.Length];
        if (Utilities.LinesIntersect(fromVisual, toVisual, a, b, out Vector2? hit) && hit != null)
        {
          intersections.Add(hit.Value);
        }
      }
    }

    if (ContainsHitPoint(fromVisual))
    {
      intersections.Add(fromVisual);
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

  public bool TryGetHitPolygon(out Vector2[] points)
  {
    PolygonCollider2D hit = HitCollider;
    if (hit == null || hit.pathCount == 0 || hit.GetPath(0).Length < 3)
    {
      points = null;
      return false;
    }

    points = GetWorldPath(hit, 0);
    return true;
  }

  public bool TryGetHitPaths(out List<Vector2[]> paths)
  {
    paths = new List<Vector2[]>();
    PolygonCollider2D hit = HitCollider;
    if (hit == null || hit.pathCount == 0)
    {
      return false;
    }

    for (int i = 0; i < hit.pathCount; i++)
    {
      if (hit.GetPath(i).Length >= 3)
      {
        paths.Add(GetWorldPath(hit, i));
      }
    }

    return paths.Count > 0;
  }

  public int GetIncludedSpriteRendererCount()
  {
    return GetIncludedSpriteRenderers().Count;
  }

  public int GetCurrentHitPathCount()
  {
    return TryGetHitPaths(out List<Vector2[]> paths) ? paths.Count : 0;
  }

  public bool HasSpritesRoot()
  {
    return ResolveSpritesRoot() != null;
  }

  public void SetHitCollider(PolygonCollider2D collider)
  {
    hitCollider = collider;
  }

  public bool ApplyLayerProfilesToHitCollider(bool force)
  {
    PolygonCollider2D hit = HitCollider;
    if (hit == null)
    {
      return false;
    }

    List<SimpleTargetLayer> layers = GetIncludedSimpleTargetLayers();
    string profileKey = BuildLayerProfileKey(layers);
    if (!force && profileKey == lastAppliedLayerProfileKey)
    {
      return false;
    }

    List<Vector2[]> colliderPaths = new List<Vector2[]>();
    for (int i = 0; i < layers.Count; i++)
    {
      SimpleTargetLayer layer = layers[i];
      if (layer != null && layer.TryGetCurrentColliderPaths(hit, out List<Vector2[]> layerPaths))
      {
        colliderPaths.AddRange(layerPaths);
      }
    }

    if (colliderPaths.Count == 0)
    {
      lastAppliedLayerProfileKey = profileKey;
      return false;
    }

    hit.pathCount = colliderPaths.Count;
    for (int i = 0; i < colliderPaths.Count; i++)
    {
      hit.SetPath(i, colliderPaths[i]);
    }

    lastAppliedLayerProfileKey = profileKey;
    return true;
  }

  public List<SimpleTargetLayer> GetIncludedSimpleTargetLayers()
  {
    List<SimpleTargetLayer> included = new List<SimpleTargetLayer>();
    Transform root = ResolveSpritesRoot();
    SimpleTargetLayer[] layers = root != null
      ? root.GetComponentsInChildren<SimpleTargetLayer>(true)
      : GetComponentsInChildren<SimpleTargetLayer>(true);

    for (int i = 0; i < layers.Length; i++)
    {
      SimpleTargetLayer layer = layers[i];
      SpriteRenderer spriteRenderer = layer != null ? layer.SpriteRenderer : null;
      if (IsIncludedSpriteRenderer(spriteRenderer))
      {
        included.Add(layer);
      }
    }

    return included;
  }

  private bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
  {
    bool inside = false;
    for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
    {
      Vector2 a = polygon[i];
      Vector2 b = polygon[j];
      bool crosses = (a.y > point.y) != (b.y > point.y);
      if (!crosses)
      {
        continue;
      }

      float x = (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
      if (point.x < x)
      {
        inside = !inside;
      }
    }

    return inside;
  }

  public GameObject GetTargetObject()
  {
    Highlighter highlighter = GetComponent<Highlighter>() ?? GetComponentInChildren<Highlighter>() ?? GetComponentInParent<Highlighter>();
    return highlighter != null ? highlighter.gameObject : gameObject;
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

    return FindColliderInOwningHierarchy("HitCollider");
  }

  public List<SpriteRenderer> GetIncludedSpriteRenderers()
  {
    List<SpriteRenderer> included = new List<SpriteRenderer>();
    Transform root = ResolveSpritesRoot();
    SpriteRenderer[] renderers = root != null
      ? root.GetComponentsInChildren<SpriteRenderer>(true)
      : GetComponentsInChildren<SpriteRenderer>(true);

    for (int i = 0; i < renderers.Length; i++)
    {
      SpriteRenderer spriteRenderer = renderers[i];
      if (!IsIncludedSpriteRenderer(spriteRenderer))
      {
        continue;
      }

      included.Add(spriteRenderer);
    }

    return included;
  }

  public bool IsIncludedSpriteRenderer(SpriteRenderer spriteRenderer)
  {
    return spriteRenderer != null &&
      spriteRenderer.enabled &&
      spriteRenderer.gameObject.activeInHierarchy &&
      spriteRenderer.sprite != null &&
      !IsExcluded(spriteRenderer);
  }

  private bool IsExcluded(SpriteRenderer spriteRenderer)
  {
    if (excludedShapeRenderers == null)
    {
      return false;
    }

    for (int i = 0; i < excludedShapeRenderers.Length; i++)
    {
      if (excludedShapeRenderers[i] == spriteRenderer)
      {
        return true;
      }
    }

    return false;
  }

  public Transform ResolveSpritesRoot()
  {
    if (spritesRoot != null)
    {
      return spritesRoot;
    }

    Transform direct = FindDescendantNamed(transform, "Sprites");
    if (direct != null)
    {
      return direct;
    }

    Transform current = transform.parent;
    while (current != null)
    {
      Transform candidate = FindDescendantNamed(current, "Sprites");
      if (candidate != null)
      {
        return candidate;
      }

      current = current.parent;
    }

    return null;
  }

  private Transform FindDescendantNamed(Transform root, string childName)
  {
    if (root == null)
    {
      return null;
    }

    if (root.name == childName)
    {
      return root;
    }

    for (int i = 0; i < root.childCount; i++)
    {
      Transform result = FindDescendantNamed(root.GetChild(i), childName);
      if (result != null)
      {
        return result;
      }
    }

    return null;
  }

  private float GetGroundLineWorldY()
  {
    PolygonCollider2D hit = HitCollider;
    return hit != null
      ? hit.transform.TransformPoint(new Vector2(0f, groundLineLocalY)).y
      : transform.TransformPoint(new Vector2(0f, groundLineLocalY)).y;
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

  private PolygonCollider2D FindColliderInOwningHierarchy(string tag)
  {
    Transform root = transform.root;
    PolygonCollider2D[] colliders = root != null
      ? root.GetComponentsInChildren<PolygonCollider2D>(true)
      : GetComponentsInChildren<PolygonCollider2D>(true);
    for (int i = 0; i < colliders.Length; i++)
    {
      PolygonCollider2D collider = colliders[i];
      if (collider != null && collider.CompareTag(tag))
      {
        return collider;
      }
    }

    return null;
  }

  private Vector2[] GetWorldPath(PolygonCollider2D collider, int pathIndex)
  {
    Vector2[] localPoints = collider.GetPath(pathIndex);
    Vector2[] points = new Vector2[localPoints.Length];
    for (int i = 0; i < localPoints.Length; i++)
    {
      points[i] = collider.transform.TransformPoint(localPoints[i]);
    }

    return points;
  }

  private bool IsBetween(float value, float a, float b)
  {
    return value >= Mathf.Min(a, b) && value <= Mathf.Max(a, b);
  }

  private void Update()
  {
    if (applyLayerProfilesAutomatically)
    {
      ApplyLayerProfilesToHitCollider(false);
    }
  }

  private string BuildLayerProfileKey(List<SimpleTargetLayer> layers)
  {
    if (layers == null || layers.Count == 0)
    {
      return "";
    }

    List<string> parts = new List<string>();
    for (int i = 0; i < layers.Count; i++)
    {
      SimpleTargetLayer layer = layers[i];
      SpriteRenderer renderer = layer != null ? layer.SpriteRenderer : null;
      Sprite sprite = renderer != null ? renderer.sprite : null;
      parts.Add(renderer != null ? renderer.GetInstanceID().ToString() : "0");
      parts.Add(sprite != null ? sprite.GetInstanceID().ToString() : "0");
      parts.Add(layer != null && layer.HasCurrentSpriteProfile() ? "1" : "0");
    }

    return string.Join("|", parts);
  }
}
