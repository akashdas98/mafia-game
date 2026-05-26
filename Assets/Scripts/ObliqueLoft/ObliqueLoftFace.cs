using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ObliqueLoftFace
{
  [SerializeField] private List<Vector3> vertices = new List<Vector3>();
  [SerializeField] private Vector3 normal;
  [SerializeField] private ObliqueSurfaceType surfaceType;
  [SerializeField] private int faceIndex;

  public IReadOnlyList<Vector3> Vertices => vertices;
  public Vector3 Normal => normal;
  public ObliqueSurfaceType SurfaceType => surfaceType;
  public int FaceIndex => faceIndex;

  public ObliqueLoftFace(int faceIndex, IEnumerable<Vector3> vertices)
  {
    this.faceIndex = faceIndex;
    this.vertices.AddRange(vertices);
    Recalculate();
  }

  public void Recalculate()
  {
    normal = CalculateNormal(vertices);
    surfaceType = ObliqueLoftBuilder.ClassifySurface(normal);
  }

  public static Vector3 CalculateNormal(IReadOnlyList<Vector3> faceVertices)
  {
    if (faceVertices == null || faceVertices.Count < 3)
    {
      return Vector3.zero;
    }

    Vector3 a = faceVertices[1] - faceVertices[0];
    Vector3 b = faceVertices[2] - faceVertices[0];
    Vector3 calculated = Vector3.Cross(a, b);
    return calculated.sqrMagnitude > Mathf.Epsilon ? calculated.normalized : Vector3.zero;
  }
}
