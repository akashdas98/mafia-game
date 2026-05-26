using System.Collections.Generic;
using UnityEngine;

public static class ObliqueRaycaster
{
  private const float PlaneEpsilon = 0.0001f;

  public static bool TryRaycast(ObliqueRay ray, IEnumerable<ObliqueLoftCollider> colliders, out ObliqueRayHit closestHit)
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
      if (collider == null || !collider.isActiveAndEnabled || !collider.UseInRaycasts)
      {
        continue;
      }

      if (!collider.ProjectedBoundsIntersects(ray))
      {
        continue;
      }

      if (TryRaycast(ray, collider, out ObliqueRayHit hit) && hit.Distance < closestDistance)
      {
        closestDistance = hit.Distance;
        closestHit = hit;
        hasHit = true;
      }
    }

    return hasHit;
  }

  public static bool TryRaycast(ObliqueRay ray, ObliqueLoftCollider collider, out ObliqueRayHit closestHit)
  {
    closestHit = default(ObliqueRayHit);
    bool hasHit = false;
    float closestDistance = float.PositiveInfinity;

    IReadOnlyList<ObliqueLoftFace> faces = collider.GeneratedFaces;
    for (int i = 0; i < faces.Count; i++)
    {
      ObliqueLoftFace face = faces[i];
      if (TryIntersectFace(ray, collider, face, out ObliqueRayHit hit) && hit.Distance < closestDistance)
      {
        closestDistance = hit.Distance;
        closestHit = hit;
        hasHit = true;
      }
    }

    return hasHit;
  }

  public static bool TryIntersectFace(ObliqueRay ray, ObliqueLoftCollider collider, ObliqueLoftFace face, out ObliqueRayHit hit)
  {
    hit = default(ObliqueRayHit);
    if (face.Vertices.Count < 3 || face.Normal == Vector3.zero)
    {
      return false;
    }

    Vector3 normal = collider.LocalDirectionToLogicWorld(face.Normal);
    if (!TryIntersectFaceTriangles(ray, collider, face, out float distance))
    {
      return false;
    }

    Vector3 point = ray.GetPoint(distance);
    hit = new ObliqueRayHit
    {
      Collider = collider,
      HitObject = collider.gameObject,
      Point = point,
      Distance = distance,
      SurfaceType = face.SurfaceType,
      Normal = normal.normalized,
      FaceIndex = face.FaceIndex
    };
    return true;
  }

  private static bool TryIntersectFaceTriangles(ObliqueRay ray, ObliqueLoftCollider collider, ObliqueLoftFace face, out float closestDistance)
  {
    closestDistance = float.PositiveInfinity;
    bool hasHit = false;
    Vector3 a = collider.LocalToLogicWorld(face.Vertices[0]);
    for (int i = 1; i < face.Vertices.Count - 1; i++)
    {
      Vector3 b = collider.LocalToLogicWorld(face.Vertices[i]);
      Vector3 c = collider.LocalToLogicWorld(face.Vertices[i + 1]);
      if (TryIntersectTriangle(ray, a, b, c, out float distance) && distance < closestDistance)
      {
        closestDistance = distance;
        hasHit = true;
      }
    }

    return hasHit;
  }

  private static bool TryIntersectTriangle(ObliqueRay ray, Vector3 a, Vector3 b, Vector3 c, out float distance)
  {
    distance = 0f;
    Vector3 edge1 = b - a;
    Vector3 edge2 = c - a;
    Vector3 h = Vector3.Cross(ray.Direction, edge2);
    float determinant = Vector3.Dot(edge1, h);
    if (Mathf.Abs(determinant) < PlaneEpsilon)
    {
      return false;
    }

    float inverseDeterminant = 1f / determinant;
    Vector3 s = ray.From - a;
    float u = inverseDeterminant * Vector3.Dot(s, h);
    if (u < -PlaneEpsilon || u > 1f + PlaneEpsilon)
    {
      return false;
    }

    Vector3 q = Vector3.Cross(s, edge1);
    float v = inverseDeterminant * Vector3.Dot(ray.Direction, q);
    if (v < -PlaneEpsilon || u + v > 1f + PlaneEpsilon)
    {
      return false;
    }

    distance = inverseDeterminant * Vector3.Dot(edge2, q);
    return distance >= -PlaneEpsilon && distance <= ray.Length + PlaneEpsilon;
  }

  private static bool IsPointInsideFace(Vector3 point, ObliqueLoftCollider collider, ObliqueLoftFace face, Vector3 normal)
  {
    int droppedAxis = GetDominantAxis(normal);
    Vector2 projectedPoint = Project(point, droppedAxis);

    bool inside = false;
    int vertexCount = face.Vertices.Count;
    for (int i = 0, j = vertexCount - 1; i < vertexCount; j = i++)
    {
      Vector2 a = Project(collider.LocalToLogicWorld(face.Vertices[i]), droppedAxis);
      Vector2 b = Project(collider.LocalToLogicWorld(face.Vertices[j]), droppedAxis);

      bool intersects = ((a.y > projectedPoint.y) != (b.y > projectedPoint.y)) &&
        (projectedPoint.x < (b.x - a.x) * (projectedPoint.y - a.y) / ((b.y - a.y) + Mathf.Epsilon) + a.x);

      if (intersects)
      {
        inside = !inside;
      }
    }

    return inside || IsPointOnFaceEdge(projectedPoint, collider, face, droppedAxis);
  }

  private static bool IsPointOnFaceEdge(Vector2 point, ObliqueLoftCollider collider, ObliqueLoftFace face, int droppedAxis)
  {
    const float edgeEpsilon = 0.001f;
    for (int i = 0; i < face.Vertices.Count; i++)
    {
      Vector2 a = Project(collider.LocalToLogicWorld(face.Vertices[i]), droppedAxis);
      Vector2 b = Project(collider.LocalToLogicWorld(face.Vertices[(i + 1) % face.Vertices.Count]), droppedAxis);
      float length = Vector2.Distance(a, b);
      if (length <= Mathf.Epsilon)
      {
        continue;
      }

      float distance = Mathf.Abs((b.x - a.x) * (a.y - point.y) - (a.x - point.x) * (b.y - a.y)) / length;
      float dot = Vector2.Dot(point - a, b - a);
      if (distance <= edgeEpsilon && dot >= -edgeEpsilon && dot <= length * length + edgeEpsilon)
      {
        return true;
      }
    }

    return false;
  }

  private static int GetDominantAxis(Vector3 normal)
  {
    Vector3 abs = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
    if (abs.x >= abs.y && abs.x >= abs.z)
    {
      return 0;
    }

    return abs.y >= abs.z ? 1 : 2;
  }

  private static Vector2 Project(Vector3 value, int droppedAxis)
  {
    switch (droppedAxis)
    {
      case 0:
        return new Vector2(value.y, value.z);
      case 1:
        return new Vector2(value.x, value.z);
      default:
        return new Vector2(value.x, value.y);
    }
  }
}
