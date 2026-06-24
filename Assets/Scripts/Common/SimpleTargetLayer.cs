using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SimpleTargetLayer : MonoBehaviour
{
  [SerializeField] private SimpleTarget simpleTarget;
  [SerializeField] private SpriteRenderer spriteRenderer;
  [SerializeField] private List<SimpleTargetSpriteOutlineProfile> spriteProfiles = new List<SimpleTargetSpriteOutlineProfile>();

  public SimpleTarget Target => simpleTarget != null ? simpleTarget : ResolveTarget();
  public SpriteRenderer SpriteRenderer => spriteRenderer != null ? spriteRenderer : ResolveSpriteRenderer();
  public IReadOnlyList<SimpleTargetSpriteOutlineProfile> SpriteProfiles => spriteProfiles;

  public void Configure(SimpleTarget target, SpriteRenderer renderer)
  {
    simpleTarget = target;
    spriteRenderer = renderer;
  }

  public bool HasCurrentSpriteProfile()
  {
    SpriteRenderer renderer = SpriteRenderer;
    return renderer != null && renderer.sprite != null && FindProfile(renderer.sprite) != null;
  }

  public int ProfileCount => spriteProfiles.Count;

  public void CaptureProfile(Sprite sprite, IReadOnlyList<Vector2[]> localPaths)
  {
    if (sprite == null || localPaths == null)
    {
      return;
    }

    SimpleTargetSpriteOutlineProfile profile = FindProfile(sprite);
    if (profile == null)
    {
      profile = new SimpleTargetSpriteOutlineProfile();
      spriteProfiles.Add(profile);
    }

    profile.Capture(sprite, localPaths);
  }

  public bool TryGetCurrentColliderPaths(PolygonCollider2D hitCollider, out List<Vector2[]> colliderPaths)
  {
    colliderPaths = new List<Vector2[]>();
    SpriteRenderer renderer = SpriteRenderer;
    if (renderer == null || renderer.sprite == null || hitCollider == null)
    {
      return false;
    }

    SimpleTargetSpriteOutlineProfile profile = FindProfile(renderer.sprite);
    if (profile == null)
    {
      return false;
    }

    for (int pathIndex = 0; pathIndex < profile.Paths.Count; pathIndex++)
    {
      SimpleTargetPath sourcePath = profile.Paths[pathIndex];
      if (sourcePath == null || sourcePath.Points.Count < 3)
      {
        continue;
      }

      Vector2[] colliderPath = new Vector2[sourcePath.Points.Count];
      for (int pointIndex = 0; pointIndex < sourcePath.Points.Count; pointIndex++)
      {
        Vector2 point = sourcePath.Points[pointIndex];
        if (renderer.flipX)
        {
          point.x = -point.x;
        }

        if (renderer.flipY)
        {
          point.y = -point.y;
        }

        Vector3 worldPoint = renderer.transform.TransformPoint(point);
        colliderPath[pointIndex] = hitCollider.transform.InverseTransformPoint(worldPoint);
      }

      colliderPaths.Add(colliderPath);
    }

    return colliderPaths.Count > 0;
  }

  public SimpleTargetSpriteOutlineProfile FindProfile(Sprite sprite)
  {
    if (sprite == null)
    {
      return null;
    }

    for (int i = 0; i < spriteProfiles.Count; i++)
    {
      SimpleTargetSpriteOutlineProfile profile = spriteProfiles[i];
      if (profile != null && profile.Sprite == sprite)
      {
        return profile;
      }
    }

    return null;
  }

  private void Reset()
  {
    ResolveTarget();
    ResolveSpriteRenderer();
  }

  private void OnValidate()
  {
    ResolveTarget();
    ResolveSpriteRenderer();
  }

  private SimpleTarget ResolveTarget()
  {
    if (simpleTarget == null)
    {
      simpleTarget = GetComponentInParent<SimpleTarget>();
    }

    return simpleTarget;
  }

  private SpriteRenderer ResolveSpriteRenderer()
  {
    if (spriteRenderer == null)
    {
      spriteRenderer = GetComponent<SpriteRenderer>();
    }

    return spriteRenderer;
  }
}

[Serializable]
public class SimpleTargetSpriteOutlineProfile
{
  [SerializeField] private Sprite sprite;
  [SerializeField] private List<SimpleTargetPath> paths = new List<SimpleTargetPath>();

  public Sprite Sprite => sprite;
  public IReadOnlyList<SimpleTargetPath> Paths => paths;

  public void Capture(Sprite sourceSprite, IReadOnlyList<Vector2[]> sourcePaths)
  {
    sprite = sourceSprite;
    paths = new List<SimpleTargetPath>();
    for (int i = 0; i < sourcePaths.Count; i++)
    {
      Vector2[] sourcePath = sourcePaths[i];
      if (sourcePath == null || sourcePath.Length < 3)
      {
        continue;
      }

      SimpleTargetPath path = new SimpleTargetPath();
      path.SetPoints(sourcePath);
      paths.Add(path);
    }
  }
}

[Serializable]
public class SimpleTargetPath
{
  [SerializeField] private List<Vector2> points = new List<Vector2>();

  public IReadOnlyList<Vector2> Points => points;

  public void SetPoints(IReadOnlyList<Vector2> sourcePoints)
  {
    points = sourcePoints != null ? new List<Vector2>(sourcePoints) : new List<Vector2>();
  }

  public void SetPoint(int index, Vector2 point)
  {
    if (index < 0 || index >= points.Count)
    {
      return;
    }

    points[index] = point;
  }
}
