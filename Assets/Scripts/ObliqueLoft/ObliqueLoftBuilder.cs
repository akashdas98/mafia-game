using System.Collections.Generic;
using UnityEngine;

public static class ObliqueLoftBuilder
{
  private const float MinimumFootprintFlatEdgeLength = 1f / 32f;
  private const float ConnectorYTolerance = 0.0001f;
  private const float CrossingPenalty = 1000000f;
  private const float PinchPenalty = 10000f;
  private const float VolumeAreaReward = 0.25f;

  public static List<string> Validate(IReadOnlyList<Vector2> footprint, IReadOnlyList<ObliqueLoftSlice> slices)
  {
    List<string> errors = new List<string>();

    if (footprint == null || footprint.Count < 3)
    {
      errors.Add("Footprint must contain at least three points.");
    }

    if (slices == null || slices.Count < 2)
    {
      errors.Add("At least two slices are required: front and back.");
      return errors;
    }

    int vertexCount = slices[0].Points.Count;
    if (vertexCount < 3)
    {
      errors.Add("Slices must contain at least three points.");
    }

    for (int i = 0; i < slices.Count; i++)
    {
      slices[i].EnsurePointOrder();
      if (slices[i].Points.Count < 3)
      {
        errors.Add("Slices must contain at least three points.");
        break;
      }

      if (slices[i].Points.Count != vertexCount)
      {
        errors.Add("All slices must use the same vertex count.");
        break;
      }

      if (!TryGetBottomConnectorIndices(slices[i].Points, out int leftConnectorIndex, out int rightConnectorIndex))
      {
        errors.Add(slices[i].Name + " must have two distinct bottom connector points.");
        break;
      }
    }

    for (int i = 1; i < slices.Count; i++)
    {
      if (slices[i].Depth <= slices[i - 1].Depth)
      {
        errors.Add("Slice depths must be ordered from front to back.");
        break;
      }
    }

    if (footprint != null && footprint.Count >= 3 && Mathf.Abs(CalculateSignedArea(footprint)) <= Mathf.Epsilon)
    {
      errors.Add("Footprint area is zero. Points must form a valid closed polygon.");
    }

    if (footprint != null && footprint.Count >= 3)
    {
      GetDepthRange(footprint, out float minDepth, out float maxDepth);
      if (!HasHorizontalEdgeAtDepth(footprint, minDepth, MinimumFootprintFlatEdgeLength))
      {
        errors.Add("Footprint front edge must be a horizontal edge with at least 1px length.");
      }

      if (!HasHorizontalEdgeAtDepth(footprint, maxDepth, MinimumFootprintFlatEdgeLength))
      {
        errors.Add("Footprint back edge must be a horizontal edge with at least 1px length.");
      }
    }

    return errors;
  }

  public static List<ObliqueLoftFace> BuildFaces(IReadOnlyList<ObliqueLoftSlice> slices)
  {
    List<ObliqueLoftFace> faces = new List<ObliqueLoftFace>();
    if (slices == null || slices.Count < 2 || slices[0].Points.Count < 3)
    {
      return faces;
    }

    int faceIndex = 0;
    List<List<Vector3>> canonicalSlices = new List<List<Vector3>>();
    foreach (ObliqueLoftSlice slice in slices)
    {
      slice.EnsurePointOrder();
      List<Vector3> previousSlice = canonicalSlices.Count > 0 ? canonicalSlices[canonicalSlices.Count - 1] : null;
      List<Vector3> canonicalSlice = GetCanonicalSliceVertices(slice, previousSlice);
      if (previousSlice != null && previousSlice.Count == canonicalSlice.Count)
      {
        canonicalSlice = OptimizeConnectionLanes(previousSlice, canonicalSlice);
      }

      canonicalSlices.Add(canonicalSlice);
    }

    Vector3 volumeCenter = CalculateVolumeCenter(canonicalSlices);
    faces.Add(CreateOutwardFace(faceIndex++, canonicalSlices[0], volumeCenter));

    for (int sliceIndex = 0; sliceIndex < slices.Count - 1; sliceIndex++)
    {
      List<Vector3> a = canonicalSlices[sliceIndex];
      List<Vector3> b = canonicalSlices[sliceIndex + 1];

      int vertexCount = Mathf.Min(a.Count, b.Count);
      for (int i = 0; i < vertexCount; i++)
      {
        int j = (i + 1) % vertexCount;
        faces.Add(CreateOutwardFace(faceIndex++, new[]
        {
          a[i],
          b[i],
          b[j],
          a[j]
        }, volumeCenter));
      }
    }

    faces.Add(CreateOutwardFace(faceIndex, canonicalSlices[canonicalSlices.Count - 1], volumeCenter));

    return faces;
  }

  private static ObliqueLoftFace CreateOutwardFace(int faceIndex, IEnumerable<Vector3> vertices, Vector3 volumeCenter)
  {
    List<Vector3> faceVertices = new List<Vector3>(vertices);
    Vector3 normal = ObliqueLoftFace.CalculateNormal(faceVertices);
    Vector3 faceCenter = CalculateFaceCenter(faceVertices);
    if (normal != Vector3.zero && Vector3.Dot(normal, faceCenter - volumeCenter) < 0f)
    {
      faceVertices.Reverse();
    }

    return new ObliqueLoftFace(faceIndex, faceVertices);
  }

  private static Vector3 CalculateVolumeCenter(IReadOnlyList<List<Vector3>> slices)
  {
    Vector3 center = Vector3.zero;
    int count = 0;
    foreach (List<Vector3> slice in slices)
    {
      foreach (Vector3 vertex in slice)
      {
        center += vertex;
        count++;
      }
    }

    return count > 0 ? center / count : Vector3.zero;
  }

  private static Vector3 CalculateFaceCenter(IReadOnlyList<Vector3> vertices)
  {
    Vector3 center = Vector3.zero;
    for (int i = 0; i < vertices.Count; i++)
    {
      center += vertices[i];
    }

    return vertices.Count > 0 ? center / vertices.Count : Vector3.zero;
  }

  public static ObliqueSurfaceType ClassifySurface(Vector3 normal)
  {
    if (normal == Vector3.zero)
    {
      return ObliqueSurfaceType.Unknown;
    }

    float ax = Mathf.Abs(normal.x);
    float ay = Mathf.Abs(normal.y);
    float az = Mathf.Abs(normal.z);

    if (ay >= ax && ay >= az)
    {
      if (normal.y > 0f)
      {
        return ay > 0.85f ? ObliqueSurfaceType.Top : ObliqueSurfaceType.SlopedTop;
      }

      return ay > 0.85f ? ObliqueSurfaceType.Bottom : ObliqueSurfaceType.SlopedBottom;
    }

    if (az >= ax)
    {
      return normal.z < 0f ? ObliqueSurfaceType.Front : ObliqueSurfaceType.Back;
    }

    return ObliqueSurfaceType.Side;
  }

  private static IEnumerable<Vector3> GetSliceVertices(ObliqueLoftSlice slice, bool reverse)
  {
    if (reverse)
    {
      slice.EnsurePointOrder();
      for (int i = slice.PointOrder.Count - 1; i >= 0; i--)
      {
        yield return slice.GetLocalVertexInConnectionOrder(i);
      }
    }
    else
    {
      slice.EnsurePointOrder();
      for (int i = 0; i < slice.PointOrder.Count; i++)
      {
        yield return slice.GetLocalVertexInConnectionOrder(i);
      }
    }
  }

  private static List<Vector3> GetCanonicalSliceVertices(ObliqueLoftSlice slice, IReadOnlyList<Vector3> previousSlice)
  {
    List<Vector3> vertices = new List<Vector3>();
    if (!TryGetBottomConnectorIndices(slice.Points, out int leftConnectorIndex, out int rightConnectorIndex))
    {
      vertices.AddRange(GetSliceVertices(slice, false));
      return vertices;
    }

    int count = slice.PointOrder.Count;
    int leftOrderIndex = slice.IndexOfConnectionPoint(leftConnectorIndex);
    int rightOrderIndex = slice.IndexOfConnectionPoint(rightConnectorIndex);
    if (leftOrderIndex < 0 || rightOrderIndex < 0)
    {
      vertices.AddRange(GetSliceVertices(slice, false));
      return vertices;
    }

    int direction = ChooseConnectorPathDirection(slice, leftOrderIndex, rightOrderIndex, previousSlice);
    return BuildConnectorRootedVertices(slice, leftOrderIndex, direction);
  }

  private static List<Vector3> BuildConnectorRootedVertices(ObliqueLoftSlice slice, int leftOrderIndex, int direction)
  {
    List<Vector3> vertices = new List<Vector3>();
    int count = slice.PointOrder.Count;
    for (int step = 0; step < count; step++)
    {
      int orderIndex = Mod(leftOrderIndex + direction * step, count);
      vertices.Add(slice.GetLocalVertexInConnectionOrder(orderIndex));
    }

    return vertices;
  }

  private static int ChooseConnectorPathDirection(
    ObliqueLoftSlice slice,
    int leftOrderIndex,
    int rightOrderIndex,
    IReadOnlyList<Vector3> previousSlice)
  {
    if (previousSlice != null && previousSlice.Count == slice.PointOrder.Count)
    {
      List<Vector3> forwardVertices = BuildConnectorRootedVertices(slice, leftOrderIndex, 1);
      List<Vector3> backwardVertices = BuildConnectorRootedVertices(slice, leftOrderIndex, -1);
      int forwardCrossings = CountConnectionCrossings(previousSlice, forwardVertices);
      int backwardCrossings = CountConnectionCrossings(previousSlice, backwardVertices);
      if (forwardCrossings != backwardCrossings)
      {
        return forwardCrossings < backwardCrossings ? 1 : -1;
      }

      float forwardScore = CalculateLaneMatchScore(previousSlice, forwardVertices);
      float backwardScore = CalculateLaneMatchScore(previousSlice, backwardVertices);
      if (!Mathf.Approximately(forwardScore, backwardScore))
      {
        return forwardScore < backwardScore ? 1 : -1;
      }
    }

    float forwardMaxHeight = GetMaxVisualYOnPath(slice, leftOrderIndex, rightOrderIndex, 1);
    float backwardMaxHeight = GetMaxVisualYOnPath(slice, leftOrderIndex, rightOrderIndex, -1);

    if (!Mathf.Approximately(forwardMaxHeight, backwardMaxHeight))
    {
      return forwardMaxHeight > backwardMaxHeight ? 1 : -1;
    }

    int forwardSteps = CountStepsOnPath(slice.PointOrder.Count, leftOrderIndex, rightOrderIndex, 1);
    int backwardSteps = CountStepsOnPath(slice.PointOrder.Count, leftOrderIndex, rightOrderIndex, -1);
    return forwardSteps >= backwardSteps ? 1 : -1;
  }

  private static float CalculateLaneMatchScore(IReadOnlyList<Vector3> previousSlice, IReadOnlyList<Vector3> candidateSlice)
  {
    float score = 0f;
    for (int i = 0; i < previousSlice.Count; i++)
    {
      Vector3 delta = previousSlice[i] - candidateSlice[i];
      score += delta.x * delta.x + delta.y * delta.y;
    }

    return score;
  }

  private static List<Vector3> OptimizeConnectionLanes(IReadOnlyList<Vector3> previousSlice, IReadOnlyList<Vector3> candidateSlice)
  {
    List<Vector3> best = new List<Vector3>(candidateSlice);
    float bestScore = CalculateConnectionLaneScore(previousSlice, best);
    bool improved = true;
    int repairLimit = best.Count * best.Count * 2;

    for (int repairStep = 0; repairStep < repairLimit && improved; repairStep++)
    {
      improved = false;
      for (int startIndex = 1; startIndex < best.Count - 1; startIndex++)
      {
        for (int endIndex = startIndex + 1; endIndex < best.Count; endIndex++)
        {
          List<Vector3> candidate = new List<Vector3>(best);
          ReverseRange(candidate, startIndex, endIndex);
          float candidateScore = CalculateConnectionLaneScore(previousSlice, candidate);
          if (candidateScore + Mathf.Epsilon >= bestScore)
          {
            continue;
          }

          best = candidate;
          bestScore = candidateScore;
          improved = true;
        }
      }
    }

    return best;
  }

  private static float CalculateConnectionLaneScore(IReadOnlyList<Vector3> previousSlice, IReadOnlyList<Vector3> candidateSlice)
  {
    int crossings = CountConnectionCrossings(previousSlice, candidateSlice);
    float matchScore = CalculateLaneMatchScore(previousSlice, candidateSlice);
    float pinchScore = 0f;
    float areaScore = 0f;

    for (int i = 0; i < previousSlice.Count; i++)
    {
      int j = (i + 1) % previousSlice.Count;
      Vector2 a = ProjectLogicToScene(previousSlice[i]);
      Vector2 b = ProjectLogicToScene(candidateSlice[i]);
      Vector2 c = ProjectLogicToScene(candidateSlice[j]);
      Vector2 d = ProjectLogicToScene(previousSlice[j]);
      float area = Mathf.Abs(CalculateProjectedQuadArea(a, b, c, d));
      areaScore += area;

      if (IsProjectedQuadPinched(a, b, c, d) || area <= Mathf.Epsilon)
      {
        pinchScore += 1f;
      }
      else
      {
        pinchScore += 1f / Mathf.Max(area, 0.0001f);
      }
    }

    return crossings * CrossingPenalty + pinchScore * PinchPenalty + matchScore - areaScore * VolumeAreaReward;
  }

  private static int CountConnectionCrossings(IReadOnlyList<Vector3> previousSlice, IReadOnlyList<Vector3> candidateSlice)
  {
    int crossings = 0;
    for (int i = 0; i < previousSlice.Count; i++)
    {
      for (int j = i + 1; j < previousSlice.Count; j++)
      {
        if (ConnectionSegmentsIntersect(previousSlice[i], candidateSlice[i], previousSlice[j], candidateSlice[j]))
        {
          crossings++;
        }
      }
    }

    return crossings;
  }

  private static bool TryGetFirstConnectionCrossing(
    IReadOnlyList<Vector3> previousSlice,
    IReadOnlyList<Vector3> candidateSlice,
    out int firstIndex,
    out int secondIndex)
  {
    firstIndex = -1;
    secondIndex = -1;
    for (int i = 0; i < previousSlice.Count; i++)
    {
      for (int j = i + 1; j < previousSlice.Count; j++)
      {
        if (!ConnectionSegmentsIntersect(previousSlice[i], candidateSlice[i], previousSlice[j], candidateSlice[j]))
        {
          continue;
        }

        firstIndex = i;
        secondIndex = j;
        return true;
      }
    }

    return false;
  }

  private static bool IsProjectedQuadPinched(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
  {
    return SegmentsIntersect(a, b, c, d) ||
      SegmentsIntersect(b, c, d, a) ||
      SegmentsIntersect(a, c, b, d);
  }

  private static float CalculateProjectedQuadArea(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
  {
    return 0.5f * (
      a.x * b.y - b.x * a.y +
      b.x * c.y - c.x * b.y +
      c.x * d.y - d.x * c.y +
      d.x * a.y - a.x * d.y);
  }

  private static bool ConnectionSegmentsIntersect(Vector3 previousA, Vector3 candidateA, Vector3 previousB, Vector3 candidateB)
  {
    Vector2 a = ProjectLogicToScene(previousA);
    Vector2 b = ProjectLogicToScene(candidateA);
    Vector2 c = ProjectLogicToScene(previousB);
    Vector2 d = ProjectLogicToScene(candidateB);
    return SegmentsIntersect(a, b, c, d);
  }

  private static Vector2 ProjectLogicToScene(Vector3 vertex)
  {
    return new Vector2(vertex.x, vertex.y + vertex.z);
  }

  private static void ReverseRange(List<Vector3> vertices, int startIndex, int endIndex)
  {
    while (startIndex < endIndex)
    {
      Vector3 swap = vertices[startIndex];
      vertices[startIndex] = vertices[endIndex];
      vertices[endIndex] = swap;
      startIndex++;
      endIndex--;
    }
  }

  private static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
  {
    if (ApproximatelySamePoint(a, c) ||
      ApproximatelySamePoint(a, d) ||
      ApproximatelySamePoint(b, c) ||
      ApproximatelySamePoint(b, d))
    {
      return false;
    }

    float o1 = Cross(b - a, c - a);
    float o2 = Cross(b - a, d - a);
    float o3 = Cross(d - c, a - c);
    float o4 = Cross(d - c, b - c);

    if (Mathf.Abs(o1) <= Mathf.Epsilon && IsPointOnSegment(a, b, c))
    {
      return true;
    }

    if (Mathf.Abs(o2) <= Mathf.Epsilon && IsPointOnSegment(a, b, d))
    {
      return true;
    }

    if (Mathf.Abs(o3) <= Mathf.Epsilon && IsPointOnSegment(c, d, a))
    {
      return true;
    }

    if (Mathf.Abs(o4) <= Mathf.Epsilon && IsPointOnSegment(c, d, b))
    {
      return true;
    }

    return o1 * o2 < 0f && o3 * o4 < 0f;
  }

  private static bool IsPointOnSegment(Vector2 a, Vector2 b, Vector2 point)
  {
    return point.x >= Mathf.Min(a.x, b.x) - Mathf.Epsilon &&
      point.x <= Mathf.Max(a.x, b.x) + Mathf.Epsilon &&
      point.y >= Mathf.Min(a.y, b.y) - Mathf.Epsilon &&
      point.y <= Mathf.Max(a.y, b.y) + Mathf.Epsilon;
  }

  private static bool ApproximatelySamePoint(Vector2 a, Vector2 b)
  {
    return (a - b).sqrMagnitude <= Mathf.Epsilon;
  }

  private static float Cross(Vector2 a, Vector2 b)
  {
    return a.x * b.y - a.y * b.x;
  }

  private static float GetMaxVisualYOnPath(ObliqueLoftSlice slice, int startIndex, int endIndex, int direction)
  {
    float maxY = slice.Points[slice.GetConnectionPointIndex(startIndex)].y;
    int count = slice.PointOrder.Count;
    int index = startIndex;
    for (int step = 0; step < count; step++)
    {
      maxY = Mathf.Max(maxY, slice.Points[slice.GetConnectionPointIndex(index)].y);
      if (index == endIndex)
      {
        break;
      }

      index = Mod(index + direction, count);
    }

    return maxY;
  }

  private static int CountStepsOnPath(int count, int startIndex, int endIndex, int direction)
  {
    int index = startIndex;
    for (int step = 0; step < count; step++)
    {
      if (index == endIndex)
      {
        return step + 1;
      }

      index = Mod(index + direction, count);
    }

    return count;
  }

  private static bool TryGetBottomConnectorIndices(IReadOnlyList<Vector2> points, out int leftIndex, out int rightIndex)
  {
    leftIndex = -1;
    rightIndex = -1;
    if (points == null || points.Count < 2)
    {
      return false;
    }

    float minY = points[0].y;
    for (int i = 1; i < points.Count; i++)
    {
      minY = Mathf.Min(minY, points[i].y);
    }

    int bottomTieCount = 0;
    float leftX = 0f;
    float rightX = 0f;
    for (int i = 0; i < points.Count; i++)
    {
      if (Mathf.Abs(points[i].y - minY) > ConnectorYTolerance)
      {
        continue;
      }

      if (leftIndex < 0 || points[i].x < leftX)
      {
        leftIndex = i;
        leftX = points[i].x;
      }

      if (rightIndex < 0 || points[i].x > rightX)
      {
        rightIndex = i;
        rightX = points[i].x;
      }

      bottomTieCount++;
    }

    if (bottomTieCount >= 2)
    {
      return leftIndex >= 0 && rightIndex >= 0 && leftIndex != rightIndex;
    }

    int lowestIndex = leftIndex;
    int secondIndex = FindSecondLowestPointIndex(points, lowestIndex);
    if (points[secondIndex].x < points[lowestIndex].x)
    {
      leftIndex = secondIndex;
      rightIndex = lowestIndex;
    }
    else
    {
      leftIndex = lowestIndex;
      rightIndex = secondIndex;
    }

    return leftIndex != rightIndex;
  }

  private static int FindSecondLowestPointIndex(IReadOnlyList<Vector2> points, int lowestIndex)
  {
    int secondIndex = lowestIndex == 0 ? 1 : 0;
    for (int i = 0; i < points.Count; i++)
    {
      if (i == lowestIndex)
      {
        continue;
      }

      Vector2 point = points[i];
      Vector2 second = points[secondIndex];
      if (point.y < second.y - ConnectorYTolerance ||
        Mathf.Abs(point.y - second.y) <= ConnectorYTolerance &&
        Mathf.Abs(point.x - points[lowestIndex].x) > Mathf.Abs(second.x - points[lowestIndex].x))
      {
        secondIndex = i;
      }
    }

    return secondIndex;
  }

  private static int Mod(int value, int divisor)
  {
    int result = value % divisor;
    return result < 0 ? result + divisor : result;
  }

  private static float CalculateSignedArea(IReadOnlyList<Vector2> points)
  {
    float area = 0f;
    for (int i = 0; i < points.Count; i++)
    {
      Vector2 a = points[i];
      Vector2 b = points[(i + 1) % points.Count];
      area += a.x * b.y - b.x * a.y;
    }

    return area * 0.5f;
  }

  private static void GetDepthRange(IReadOnlyList<Vector2> points, out float minDepth, out float maxDepth)
  {
    minDepth = points[0].y;
    maxDepth = points[0].y;
    for (int i = 1; i < points.Count; i++)
    {
      minDepth = Mathf.Min(minDepth, points[i].y);
      maxDepth = Mathf.Max(maxDepth, points[i].y);
    }
  }

  private static bool HasHorizontalEdgeAtDepth(IReadOnlyList<Vector2> points, float depth, float minimumLength)
  {
    for (int i = 0; i < points.Count; i++)
    {
      Vector2 a = points[i];
      Vector2 b = points[(i + 1) % points.Count];
      if (Mathf.Approximately(a.y, depth) &&
        Mathf.Approximately(b.y, depth) &&
        Mathf.Abs(a.x - b.x) >= minimumLength)
      {
        return true;
      }
    }

    return false;
  }
}
