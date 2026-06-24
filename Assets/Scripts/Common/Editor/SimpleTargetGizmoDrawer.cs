using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class SimpleTargetGizmoDrawer
{
  private static readonly Color GroundBaselineColor = new Color(1f, 0.58f, 0.08f, 0.85f);
  private static readonly Color HitOutlineColor = new Color(0.2f, 0.75f, 1f, 0.95f);
  private static readonly Color HitPointColor = new Color(0.75f, 0.95f, 1f, 1f);

  static SimpleTargetGizmoDrawer()
  {
    SceneView.duringSceneGui -= DrawSimpleTargetSceneGizmos;
    SceneView.duringSceneGui += DrawSimpleTargetSceneGizmos;
  }

  private static void DrawSimpleTargetSceneGizmos(SceneView sceneView)
  {
    SimpleTarget[] targets = Resources.FindObjectsOfTypeAll<SimpleTarget>();
    for (int i = 0; i < targets.Length; i++)
    {
      DrawSimpleTargetGizmos(targets[i]);
    }
  }

  private static void DrawSimpleTargetGizmos(SimpleTarget target)
  {
    if (target == null || !target.isActiveAndEnabled || EditorUtility.IsPersistent(target) ||
        !target.gameObject.scene.IsValid() || !target.gameObject.scene.isLoaded ||
        !IsTargetOrRelatedObjectSelected(target))
    {
      return;
    }

    if (GetSelectedLayerForTarget(target) != null)
    {
      return;
    }

    if (target.ApplyLayerProfilesAutomatically && target.ApplyLayerProfilesToHitCollider(false))
    {
      PolygonCollider2D hitCollider = target.HitCollider;
      if (hitCollider != null)
      {
        EditorUtility.SetDirty(hitCollider);
      }
    }

    CompareFunction previousZTest = Handles.zTest;
    Color previousColor = Handles.color;
    Handles.zTest = CompareFunction.Always;

    if (target.TryGetHitPaths(out List<Vector2[]> paths))
    {
      for (int i = 0; i < paths.Count; i++)
      {
        DrawOutlinePolygon(paths[i], target.transform.position.z, HitOutlineColor);
      }
    }

    if (target.TryGetGroundBaseline(out Vector2 left, out Vector2 right))
    {
      Handles.color = GroundBaselineColor;
      Handles.DrawAAPolyLine(2.5f, ToScenePoint(left, target.transform.position.z), ToScenePoint(right, target.transform.position.z));
    }

    Handles.color = previousColor;
    Handles.zTest = previousZTest;
  }

  [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.Pickable)]
  private static void DrawSelectedSimpleTargetGizmo(SimpleTarget target, GizmoType gizmoType)
  {
    // Pickable hook only. SceneView.duringSceneGui owns the visual drawing so child selection does not affect it.
  }

  private static void DrawOutlinePolygon(Vector2[] points, float z, Color color)
  {
    if (points == null || points.Length < 3)
    {
      return;
    }

    Handles.color = color;
    for (int i = 0; i < points.Length; i++)
    {
      Handles.DrawAAPolyLine(2.5f, ToScenePoint(points[i], z), ToScenePoint(points[(i + 1) % points.Length], z));
    }
  }

  private static Vector3 ToScenePoint(Vector2 point, float z)
  {
    return new Vector3(point.x, point.y, z);
  }

  internal static void DrawEditableLayerCollider(SimpleTargetLayer layer)
  {
    if (layer == null || layer.Target == null || layer.SpriteRenderer == null || layer.SpriteRenderer.sprite == null)
    {
      return;
    }

    SimpleTarget target = layer.Target;
    PolygonCollider2D hitCollider = target.HitCollider;
    if (hitCollider == null)
    {
      return;
    }

    SimpleTargetSpriteOutlineProfile profile = layer.FindProfile(layer.SpriteRenderer.sprite);
    if (profile == null)
    {
      return;
    }

    CompareFunction previousZTest = Handles.zTest;
    Color previousColor = Handles.color;
    Handles.zTest = CompareFunction.Always;

    for (int pathIndex = 0; pathIndex < profile.Paths.Count; pathIndex++)
    {
      SimpleTargetPath path = profile.Paths[pathIndex];
      if (path == null || path.Points.Count < 3)
      {
        continue;
      }

      for (int pointIndex = 0; pointIndex < path.Points.Count; pointIndex++)
      {
        Vector3 scenePoint = LayerPointToWorld(layer, path.Points[pointIndex]);
        Vector3 nextScenePoint = LayerPointToWorld(layer, path.Points[(pointIndex + 1) % path.Points.Count]);
        Handles.color = HitOutlineColor;
        Handles.DrawAAPolyLine(2.5f, scenePoint, nextScenePoint);

        Handles.color = HitPointColor;
        float size = HandleUtility.GetHandleSize(scenePoint) * 0.055f;
        EditorGUI.BeginChangeCheck();
        Vector3 moved = Handles.FreeMoveHandle(scenePoint, size, Vector3.zero, Handles.DotHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
          Undo.RecordObject(layer, "Move SimpleTarget Layer Hit Point");
          path.SetPoint(pointIndex, WorldToLayerPoint(layer, moved));
          target.ApplyLayerProfilesToHitCollider(true);
          EditorUtility.SetDirty(layer);
          EditorUtility.SetDirty(hitCollider);
          SceneView.RepaintAll();
        }
      }
    }

    Handles.color = previousColor;
    Handles.zTest = previousZTest;
  }

  private static Vector3 LayerPointToWorld(SimpleTargetLayer layer, Vector2 point)
  {
    SpriteRenderer renderer = layer.SpriteRenderer;
    if (renderer.flipX)
    {
      point.x = -point.x;
    }

    if (renderer.flipY)
    {
      point.y = -point.y;
    }

    return renderer.transform.TransformPoint(point);
  }

  private static Vector2 WorldToLayerPoint(SimpleTargetLayer layer, Vector3 worldPoint)
  {
    SpriteRenderer renderer = layer.SpriteRenderer;
    Vector2 point = renderer.transform.InverseTransformPoint(worldPoint);
    if (renderer.flipX)
    {
      point.x = -point.x;
    }

    if (renderer.flipY)
    {
      point.y = -point.y;
    }

    return point;
  }

  private static bool IsTargetOrRelatedObjectSelected(SimpleTarget target)
  {
    Transform targetTransform = target.transform;
    GameObject[] selectedObjects = Selection.gameObjects;
    for (int i = 0; i < selectedObjects.Length; i++)
    {
      GameObject selected = selectedObjects[i];
      if (selected == null)
      {
        continue;
      }

      Transform selectedTransform = selected.transform;
      if (selectedTransform == targetTransform ||
          selectedTransform.IsChildOf(targetTransform) ||
          targetTransform.IsChildOf(selectedTransform))
      {
        return true;
      }

      SimpleTargetLayer selectedLayer = selected.GetComponentInParent<SimpleTargetLayer>();
      if (selectedLayer != null && selectedLayer.Target == target)
      {
        return true;
      }

      if (IsSelectedIncludedSpriteLayer(target, selectedTransform))
      {
        return true;
      }
    }

    return false;
  }

  private static SimpleTargetLayer GetSelectedLayerForTarget(SimpleTarget target)
  {
    GameObject[] selectedObjects = Selection.gameObjects;
    for (int i = 0; i < selectedObjects.Length; i++)
    {
      GameObject selected = selectedObjects[i];
      if (selected == null)
      {
        continue;
      }

      SimpleTargetLayer selectedLayer = selected.GetComponentInParent<SimpleTargetLayer>();
      if (selectedLayer != null && selectedLayer.Target == target)
      {
        return selectedLayer;
      }
    }

    return null;
  }

  private static bool IsSelectedIncludedSpriteLayer(SimpleTarget target, Transform selectedTransform)
  {
    if (target == null || selectedTransform == null)
    {
      return false;
    }

    List<SpriteRenderer> renderers = target.GetIncludedSpriteRenderers();
    for (int i = 0; i < renderers.Count; i++)
    {
      SpriteRenderer renderer = renderers[i];
      if (renderer == null)
      {
        continue;
      }

      Transform rendererTransform = renderer.transform;
      if (selectedTransform == rendererTransform ||
          selectedTransform.IsChildOf(rendererTransform) ||
          rendererTransform.IsChildOf(selectedTransform))
      {
        return true;
      }
    }

    return false;
  }
}

[CustomEditor(typeof(SimpleTarget))]
public class SimpleTargetEditor : Editor
{
  private const byte AlphaThreshold = 0;

  private SerializedProperty useInTargetingProperty;
  private SerializedProperty groundLineLocalYProperty;
  private SerializedProperty spritesRootProperty;
  private SerializedProperty excludedShapeRenderersProperty;
  private SerializedProperty hitColliderProperty;

  private void OnEnable()
  {
    useInTargetingProperty = serializedObject.FindProperty("useInTargeting");
    groundLineLocalYProperty = serializedObject.FindProperty("groundLineLocalY");
    spritesRootProperty = serializedObject.FindProperty("spritesRoot");
    excludedShapeRenderersProperty = serializedObject.FindProperty("excludedShapeRenderers");
    hitColliderProperty = serializedObject.FindProperty("hitCollider");
  }

  public override void OnInspectorGUI()
  {
    serializedObject.Update();

    EditorGUILayout.PropertyField(useInTargetingProperty);
    EditorGUILayout.PropertyField(groundLineLocalYProperty);

    EditorGUILayout.PropertyField(hitColliderProperty);

    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Auto Detect From Sprites", EditorStyles.boldLabel);
    EditorGUILayout.PropertyField(spritesRootProperty);
    EditorGUILayout.PropertyField(excludedShapeRenderersProperty);

    serializedObject.ApplyModifiedProperties();

    SimpleTarget simpleTarget = (SimpleTarget)target;
    EnsureLayerComponents(simpleTarget);
    DrawAutoDetectStatus(simpleTarget);

    if (GUILayout.Button("Auto Detect Current Sprite Outlines"))
    {
      AutoDetectCurrentSpriteOutlines(simpleTarget);
    }

    if (GUILayout.Button("Auto Detect All Animator Frames"))
    {
      int detected = 0;
      List<SimpleTargetLayer> layers = simpleTarget.GetIncludedSimpleTargetLayers();
      for (int i = 0; i < layers.Count; i++)
      {
        detected += AutoDetectAllAnimatorSprites(layers[i]);
      }

      EditorUtility.DisplayDialog(
        "Auto Detect All Animator Frames",
        $"Detected outlines for {detected} layer sprite profile(s).",
        "OK"
      );
    }
  }

  private void DrawAutoDetectStatus(SimpleTarget target)
  {
    EditorGUILayout.LabelField("Sprites Root Found", target.HasSpritesRoot() ? "Yes" : "No");
    EditorGUILayout.LabelField("Included Renderers", target.GetIncludedSpriteRendererCount().ToString());
    EditorGUILayout.LabelField("Current Hit Paths", target.GetCurrentHitPathCount().ToString());

    if (!target.HasSpritesRoot())
    {
      EditorGUILayout.HelpBox("No Sprites child was found. Auto-detect will fall back to SpriteRenderers under this SimpleTarget object.", MessageType.Warning);
    }

    if (target.HitCollider == null)
    {
      EditorGUILayout.HelpBox("No Hit Collider is assigned or discoverable. Auto-detect will create one on this object.", MessageType.Info);
    }
  }

  private void AutoDetectCurrentSpriteOutlines(SimpleTarget target)
  {
    EnsureLayerComponents(target);

    List<SimpleTargetLayer> layers = target.GetIncludedSimpleTargetLayers();
    for (int i = 0; i < layers.Count; i++)
    {
      AutoDetectCurrentSprite(layers[i], false);
    }

    PolygonCollider2D hitCollider = EnsureHitCollider(target);
    if (hitCollider == null)
    {
      EditorGUILayout.HelpBox("Could not create or resolve a Hit Collider.", MessageType.Error);
      return;
    }

    if (!target.ApplyLayerProfilesToHitCollider(true))
    {
      EditorUtility.DisplayDialog(
        "Auto Detect Current Sprite Outlines",
        "No opaque sprite pixels were found in the included current sprites.",
        "OK"
      );
      return;
    }

    EditorUtility.SetDirty(hitCollider);
    SceneView.RepaintAll();

    Debug.Log(
      $"SimpleTarget auto-detected sprite outline profiles and applied them into {hitCollider.name}.",
      target
    );
  }

  internal static PolygonCollider2D EnsureHitCollider(SimpleTarget target)
  {
    PolygonCollider2D hitCollider = target.HitCollider;
    if (hitCollider != null)
    {
      return hitCollider;
    }

    Undo.IncrementCurrentGroup();
    hitCollider = Undo.AddComponent<PolygonCollider2D>(target.gameObject);
    hitCollider.isTrigger = true;
    target.SetHitCollider(hitCollider);
    EditorUtility.SetDirty(target);
    return hitCollider;
  }

  internal static bool AutoDetectCurrentSprite(SimpleTargetLayer layer, bool applyToParent)
  {
    if (layer == null || layer.SpriteRenderer == null || layer.SpriteRenderer.sprite == null)
    {
      return false;
    }

    List<Vector2[]> paths = TraceSpriteAlphaOutlines(layer.SpriteRenderer.sprite);
    if (paths.Count == 0)
    {
      return false;
    }

    Undo.RecordObject(layer, "Auto Detect SimpleTarget Layer Sprite");
    layer.CaptureProfile(layer.SpriteRenderer.sprite, paths);
    EditorUtility.SetDirty(layer);

    if (applyToParent && layer.Target != null)
    {
      PolygonCollider2D hitCollider = EnsureHitCollider(layer.Target);
      if (hitCollider != null)
      {
        Undo.RecordObject(hitCollider, "Apply SimpleTarget Layer Profiles");
        layer.Target.ApplyLayerProfilesToHitCollider(true);
        EditorUtility.SetDirty(hitCollider);
      }
    }

    SceneView.RepaintAll();
    return true;
  }

  internal static int AutoDetectAllAnimatorSprites(SimpleTargetLayer layer)
  {
    if (layer == null || layer.SpriteRenderer == null)
    {
      return 0;
    }

    List<Sprite> sprites = CollectAnimatorSprites(layer);
    int detected = 0;
    Undo.RecordObject(layer, "Auto Detect SimpleTarget Layer Animator Sprites");
    for (int i = 0; i < sprites.Count; i++)
    {
      Sprite sprite = sprites[i];
      List<Vector2[]> paths = TraceSpriteAlphaOutlines(sprite);
      if (paths.Count == 0)
      {
        continue;
      }

      layer.CaptureProfile(sprite, paths);
      detected++;
    }

    EditorUtility.SetDirty(layer);
    if (layer.Target != null)
    {
      PolygonCollider2D hitCollider = EnsureHitCollider(layer.Target);
      if (hitCollider != null)
      {
        Undo.RecordObject(hitCollider, "Apply SimpleTarget Layer Profiles");
        layer.Target.ApplyLayerProfilesToHitCollider(true);
        EditorUtility.SetDirty(hitCollider);
      }
    }

    SceneView.RepaintAll();
    return detected;
  }

  internal static List<Vector2[]> TraceSpriteAlphaOutlines(Sprite sprite)
  {
    List<Vector2[]> paths = new List<Vector2[]>();
    if (sprite == null || sprite.texture == null)
    {
      return paths;
    }

    string texturePath = AssetDatabase.GetAssetPath(sprite.texture);
    TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
    bool restoreReadable = false;

    if (importer != null && !importer.isReadable)
    {
      restoreReadable = true;
      importer.isReadable = true;
      importer.SaveAndReimport();
    }

    try
    {
      Rect rect = sprite.rect;
      int width = Mathf.RoundToInt(rect.width);
      int height = Mathf.RoundToInt(rect.height);
      int originX = Mathf.RoundToInt(rect.x);
      int originY = Mathf.RoundToInt(rect.y);
      Color32[] pixels = sprite.texture.GetPixels32();
      int textureWidth = sprite.texture.width;

      bool IsOpaque(int x, int y)
      {
        if (x < 0 || y < 0 || x >= width || y >= height)
        {
          return false;
        }

        int textureX = originX + x;
        int textureY = originY + y;
        int pixelIndex = textureY * textureWidth + textureX;
        return pixelIndex >= 0 && pixelIndex < pixels.Length && pixels[pixelIndex].a > AlphaThreshold;
      }

      Dictionary<GridPoint, List<GridPoint>> edges = new Dictionary<GridPoint, List<GridPoint>>();
      for (int y = 0; y < height; y++)
      {
        for (int x = 0; x < width; x++)
        {
          if (!IsOpaque(x, y))
          {
            continue;
          }

          if (!IsOpaque(x, y - 1))
          {
            AddEdge(edges, new GridPoint(x, y), new GridPoint(x + 1, y));
          }

          if (!IsOpaque(x + 1, y))
          {
            AddEdge(edges, new GridPoint(x + 1, y), new GridPoint(x + 1, y + 1));
          }

          if (!IsOpaque(x, y + 1))
          {
            AddEdge(edges, new GridPoint(x + 1, y + 1), new GridPoint(x, y + 1));
          }

          if (!IsOpaque(x - 1, y))
          {
            AddEdge(edges, new GridPoint(x, y + 1), new GridPoint(x, y));
          }
        }
      }

      paths.AddRange(BuildOutlinePaths(edges, sprite));
    }
    catch (UnityException ex)
    {
      Debug.LogError($"Could not read sprite pixels for {sprite.name}: {ex.Message}");
    }
    finally
    {
      if (importer != null && restoreReadable)
      {
        importer.isReadable = false;
        importer.SaveAndReimport();
      }
    }

    return paths;
  }

  private static List<Vector2[]> BuildOutlinePaths(Dictionary<GridPoint, List<GridPoint>> edges, Sprite sprite)
  {
    List<Vector2[]> paths = new List<Vector2[]>();
    while (edges.Count > 0)
    {
      GridPoint start = default;
      foreach (GridPoint key in edges.Keys)
      {
        start = key;
        break;
      }

      List<GridPoint> gridPath = new List<GridPoint>();
      GridPoint current = start;
      int guard = 0;

      while (guard++ < 100000)
      {
        gridPath.Add(current);
        if (!TryTakeNextEdge(edges, current, out GridPoint next))
        {
          break;
        }

        current = next;
        if (current.Equals(start))
        {
          break;
        }
      }

      if (gridPath.Count < 3)
      {
        continue;
      }

      Vector2[] path = new Vector2[gridPath.Count];
      for (int i = 0; i < gridPath.Count; i++)
      {
        path[i] = GridToSpriteLocal(gridPath[i], sprite);
      }

      paths.Add(path);
    }

    return paths;
  }

  private static Vector2 GridToSpriteLocal(GridPoint point, Sprite sprite)
  {
    return new Vector2(
      (point.x - sprite.pivot.x) / sprite.pixelsPerUnit,
      (point.y - sprite.pivot.y) / sprite.pixelsPerUnit
    );
  }

  private static void AddEdge(Dictionary<GridPoint, List<GridPoint>> edges, GridPoint from, GridPoint to)
  {
    if (!edges.TryGetValue(from, out List<GridPoint> destinations))
    {
      destinations = new List<GridPoint>();
      edges.Add(from, destinations);
    }

    destinations.Add(to);
  }

  private static bool TryTakeNextEdge(Dictionary<GridPoint, List<GridPoint>> edges, GridPoint from, out GridPoint to)
  {
    to = default;
    if (!edges.TryGetValue(from, out List<GridPoint> destinations) || destinations.Count == 0)
    {
      return false;
    }

    to = destinations[0];
    destinations.RemoveAt(0);
    if (destinations.Count == 0)
    {
      edges.Remove(from);
    }

    return true;
  }

  private readonly struct GridPoint
  {
    public readonly int x;
    public readonly int y;

    public GridPoint(int x, int y)
    {
      this.x = x;
      this.y = y;
    }

    public override bool Equals(object obj)
    {
      return obj is GridPoint other && x == other.x && y == other.y;
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return (x * 397) ^ y;
      }
    }
  }

  private void EnsureLayerComponents(SimpleTarget target)
  {
    if (target == null)
    {
      return;
    }

    List<SpriteRenderer> renderers = target.GetIncludedSpriteRenderers();
    for (int i = 0; i < renderers.Count; i++)
    {
      SpriteRenderer renderer = renderers[i];
      if (renderer == null)
      {
        continue;
      }

      SimpleTargetLayer layer = renderer.GetComponent<SimpleTargetLayer>();
      if (layer == null)
      {
        layer = Undo.AddComponent<SimpleTargetLayer>(renderer.gameObject);
      }

      layer.Configure(target, renderer);
      EditorUtility.SetDirty(layer);
    }
  }

  private static List<Sprite> CollectAnimatorSprites(SimpleTargetLayer layer)
  {
    HashSet<Sprite> sprites = new HashSet<Sprite>();
    if (layer.SpriteRenderer != null && layer.SpriteRenderer.sprite != null)
    {
      sprites.Add(layer.SpriteRenderer.sprite);
    }

    Animator animator = layer.GetComponent<Animator>();
    RuntimeAnimatorController controller = animator != null ? animator.runtimeAnimatorController : null;
    if (animator == null || controller == null)
    {
      return sprites.ToList();
    }

    string spriteRendererPath = AnimationUtility.CalculateTransformPath(layer.SpriteRenderer.transform, animator.transform);
    AnimationClip[] clips = controller.animationClips;
    for (int clipIndex = 0; clipIndex < clips.Length; clipIndex++)
    {
      AnimationClip clip = clips[clipIndex];
      if (clip == null)
      {
        continue;
      }

      EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
      for (int bindingIndex = 0; bindingIndex < bindings.Length; bindingIndex++)
      {
        EditorCurveBinding binding = bindings[bindingIndex];
        if (binding.type != typeof(SpriteRenderer) ||
          binding.propertyName != "m_Sprite" ||
          binding.path != spriteRendererPath)
        {
          continue;
        }

        ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
        for (int keyIndex = 0; keyIndex < keyframes.Length; keyIndex++)
        {
          if (keyframes[keyIndex].value is Sprite sprite)
          {
            sprites.Add(sprite);
          }
        }
      }
    }

    return sprites.ToList();
  }
}

[CustomEditor(typeof(SimpleTargetLayer))]
public class SimpleTargetLayerEditor : Editor
{
  private void OnSceneGUI()
  {
    SimpleTargetLayer layer = (SimpleTargetLayer)target;
    SimpleTarget simpleTarget = layer != null ? layer.Target : null;
    if (simpleTarget == null)
    {
      return;
    }

    if (simpleTarget.ApplyLayerProfilesAutomatically && simpleTarget.ApplyLayerProfilesToHitCollider(false))
    {
      PolygonCollider2D hitCollider = simpleTarget.HitCollider;
      if (hitCollider != null)
      {
        EditorUtility.SetDirty(hitCollider);
      }
    }

    SimpleTargetGizmoDrawer.DrawEditableLayerCollider(layer);
    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
  }

  public override void OnInspectorGUI()
  {
    DrawDefaultInspector();

    SimpleTargetLayer layer = (SimpleTargetLayer)target;
    EditorGUILayout.Space();
    EditorGUILayout.LabelField("SimpleTarget Layer", EditorStyles.boldLabel);
    EditorGUILayout.LabelField("Parent Target", layer.Target != null ? layer.Target.name : "None");
    EditorGUILayout.LabelField("Sprite Renderer", layer.SpriteRenderer != null ? layer.SpriteRenderer.name : "None");
    EditorGUILayout.LabelField("Current Sprite", layer.SpriteRenderer != null && layer.SpriteRenderer.sprite != null ? layer.SpriteRenderer.sprite.name : "None");
    EditorGUILayout.LabelField("Stored Sprite Profiles", layer.ProfileCount.ToString());
    EditorGUILayout.LabelField("Current Profile", layer.HasCurrentSpriteProfile() ? "Exists" : "Missing");

    if (GUILayout.Button("Auto Detect For Current Sprite"))
    {
      if (!SimpleTargetEditor.AutoDetectCurrentSprite(layer, true))
      {
        EditorUtility.DisplayDialog("Auto Detect For Current Sprite", "No opaque sprite pixels were found for the current sprite.", "OK");
      }
    }

    if (GUILayout.Button("Auto Detect For All Animator Frames"))
    {
      int detected = SimpleTargetEditor.AutoDetectAllAnimatorSprites(layer);
      EditorUtility.DisplayDialog(
        "Auto Detect For All Animator Frames",
        $"Detected outlines for {detected} sprite(s).",
        "OK"
      );
    }
  }
}
