using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
[ExecuteAlways]
public class ObliqueLoftCollider : MonoBehaviour
{
  [SerializeField] private bool useInRaycasts = true;
  [SerializeField] private bool showGizmos;
  [SerializeField] private List<Vector2> footprint = new List<Vector2>();
  [SerializeField] private List<ObliqueLoftSlice> slices = new List<ObliqueLoftSlice>();
  [SerializeField] private List<ObliqueLoftFace> generatedFaces = new List<ObliqueLoftFace>();
  [SerializeField] private List<string> validationErrors = new List<string>();

  public bool UseInRaycasts => useInRaycasts;
  public IReadOnlyList<Vector2> Footprint => footprint;
  public List<Vector2> EditableFootprint => footprint;
  public IReadOnlyList<ObliqueLoftSlice> Slices => slices;
  public List<ObliqueLoftSlice> EditableSlices => slices;
  public IReadOnlyList<ObliqueLoftFace> GeneratedFaces => generatedFaces;
  public IReadOnlyList<string> ValidationErrors => validationErrors;
  public bool IsValid => validationErrors.Count == 0;
  public bool ShowGizmos => showGizmos;
  public PolygonCollider2D FootprintCollider => EnsureFootprintCollider();

  public void ResetToBox(float width = 1f, float depth = 1f, float height = 0.5f)
  {
    showGizmos = false;
    float halfWidth = width * 0.5f;
    float halfDepth = depth * 0.5f;
    footprint = new List<Vector2>
    {
      new Vector2(-halfWidth, -halfDepth),
      new Vector2(halfWidth, -halfDepth),
      new Vector2(halfWidth, halfDepth),
      new Vector2(-halfWidth, halfDepth)
    };

    slices = new List<ObliqueLoftSlice>
    {
      new ObliqueLoftSlice("Front", -halfDepth),
      new ObliqueLoftSlice("Back", halfDepth)
    };

    slices[0].EditablePoints.Add(new Vector2(-halfWidth, -halfDepth));
    slices[0].EditablePoints.Add(new Vector2(-halfWidth, -halfDepth + height));
    slices[0].EditablePoints.Add(new Vector2(halfWidth, -halfDepth + height));
    slices[0].EditablePoints.Add(new Vector2(halfWidth, -halfDepth));

    slices[1].EditablePoints.Add(new Vector2(-halfWidth, halfDepth));
    slices[1].EditablePoints.Add(new Vector2(-halfWidth, halfDepth + height));
    slices[1].EditablePoints.Add(new Vector2(halfWidth, halfDepth + height));
    slices[1].EditablePoints.Add(new Vector2(halfWidth, halfDepth));

    Rebuild();
  }

  public void Rebuild()
  {
    validationErrors = ObliqueLoftBuilder.Validate(footprint, slices);
    generatedFaces = validationErrors.Count == 0 ? ObliqueLoftBuilder.BuildFaces(slices) : new List<ObliqueLoftFace>();
    SyncFootprintCollider();
  }

  public Vector3 LocalToLogicWorld(Vector3 local)
  {
    Vector3 groundScene = LocalGroundToScene(new Vector2(local.x, local.z));
    return new Vector3(
      groundScene.x,
      local.y * GetLogicHeightScale(),
      groundScene.y
    );
  }

  public Vector3 LocalDirectionToLogicWorld(Vector3 localDirection)
  {
    Vector2 groundSceneDirection = LocalGroundVectorToScene(new Vector2(localDirection.x, localDirection.z));
    return new Vector3(
      groundSceneDirection.x,
      localDirection.y * GetLogicHeightScale(),
      groundSceneDirection.y
    ).normalized;
  }

  public Vector3 LogicWorldToScene(Vector3 logic)
  {
    return new Vector3(logic.x, logic.z + logic.y, transform.position.z);
  }

  public Vector3 LocalGroundToScene(Vector2 localGround)
  {
    Vector2 scene = (Vector2)transform.position + LocalGroundVectorToScene(localGround);
    return new Vector3(scene.x, scene.y, transform.position.z);
  }

  public Vector2 SceneToLocalGround(Vector3 scenePoint)
  {
    Vector2 delta = (Vector2)scenePoint - (Vector2)transform.position;
    Vector2 xAxis = GetSceneGroundXAxis();
    Vector2 yAxis = GetSceneGroundYAxis();
    float determinant = xAxis.x * yAxis.y - xAxis.y * yAxis.x;
    if (Mathf.Abs(determinant) <= 0.000001f)
    {
      return Vector2.zero;
    }

    float localX = (delta.x * yAxis.y - delta.y * yAxis.x) / determinant;
    float localY = (xAxis.x * delta.y - xAxis.y * delta.x) / determinant;
    return new Vector2(localX, localY);
  }

  public float GetLogicHeightScale()
  {
    return Mathf.Abs(transform.lossyScale.y);
  }

  public bool ProjectedBoundsIntersects(ObliqueRay ray)
  {
    if (generatedFaces.Count == 0)
    {
      return false;
    }

    Bounds bounds = GetLogicBounds();
    Vector2 rayMin = new Vector2(Mathf.Min(ray.From.x, ray.To.x), Mathf.Min(ray.From.z, ray.To.z));
    Vector2 rayMax = new Vector2(Mathf.Max(ray.From.x, ray.To.x), Mathf.Max(ray.From.z, ray.To.z));
    return rayMax.x >= bounds.min.x && rayMin.x <= bounds.max.x && rayMax.y >= bounds.min.z && rayMin.y <= bounds.max.z;
  }

  public Bounds GetLogicBounds()
  {
    bool hasPoint = false;
    Bounds bounds = new Bounds();

    foreach (ObliqueLoftFace face in generatedFaces)
    {
      foreach (Vector3 vertex in face.Vertices)
      {
        Vector3 world = LocalToLogicWorld(vertex);
        if (!hasPoint)
        {
          bounds = new Bounds(world, Vector3.zero);
          hasPoint = true;
        }
        else
        {
          bounds.Encapsulate(world);
        }
      }
    }

    return bounds;
  }

  private void OnValidate()
  {
    Rebuild();
  }

  private void OnEnable()
  {
    Rebuild();
  }

  private void Awake()
  {
    Rebuild();
  }

  private void Reset()
  {
    ResetToBox();
  }

  private void OnDrawGizmos()
  {
  }

  private void SyncFootprintCollider()
  {
    PolygonCollider2D polygonCollider = EnsureFootprintCollider();
    if (polygonCollider == null)
    {
      return;
    }

    polygonCollider.isTrigger = false;
    polygonCollider.pathCount = 1;
    polygonCollider.SetPath(0, footprint.ToArray());
  }

  private PolygonCollider2D EnsureFootprintCollider()
  {
    PolygonCollider2D polygonCollider = GetComponent<PolygonCollider2D>();
    if (polygonCollider == null)
    {
      polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
    }

    return polygonCollider;
  }

  private Vector2 LocalGroundVectorToScene(Vector2 localGround)
  {
    return GetSceneGroundXAxis() * localGround.x + GetSceneGroundYAxis() * localGround.y;
  }

  private Vector2 GetSceneGroundXAxis()
  {
    Vector3 axis = transform.TransformVector(Vector3.right);
    return new Vector2(axis.x, axis.y);
  }

  private Vector2 GetSceneGroundYAxis()
  {
    Vector3 axis = transform.TransformVector(Vector3.up);
    return new Vector2(axis.x, axis.y);
  }
}
