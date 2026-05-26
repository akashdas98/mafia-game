using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ObliqueLoftSlice
{
  [SerializeField] private string name = "Slice";
  [SerializeField] private float depth;
  [SerializeField] private List<Vector2> points = new List<Vector2>();
  [SerializeField] private List<int> pointOrder = new List<int>();

  public string Name => name;
  public float Depth => depth;
  public IReadOnlyList<Vector2> Points => points;
  public List<Vector2> EditablePoints => points;
  public IReadOnlyList<int> PointOrder
  {
    get
    {
      EnsurePointOrder();
      return pointOrder;
    }
  }
  public List<int> EditablePointOrder
  {
    get
    {
      EnsurePointOrder();
      return pointOrder;
    }
  }

  public ObliqueLoftSlice()
  {
  }

  public ObliqueLoftSlice(string name, float depth)
  {
    this.name = name;
    this.depth = depth;
  }

  public void SetName(string value)
  {
    name = value;
  }

  public void SetDepth(float value)
  {
    depth = value;
  }

  public void CopyPointsFrom(ObliqueLoftSlice source)
  {
    points.Clear();
    pointOrder.Clear();
    if (source == null)
    {
      return;
    }

    points.AddRange(source.Points);
    EnsurePointOrder();
  }

  public Vector3 GetLocalVertex(int index)
  {
    Vector2 point = points[index];
    return new Vector3(point.x, Mathf.Max(0f, point.y - depth), depth);
  }

  public int GetConnectionPointIndex(int orderIndex)
  {
    EnsurePointOrder();
    return pointOrder[orderIndex];
  }

  public Vector3 GetLocalVertexInConnectionOrder(int orderIndex)
  {
    return GetLocalVertex(GetConnectionPointIndex(orderIndex));
  }

  public void InsertConnectionPointAfter(int orderIndex, int pointIndex)
  {
    EnsurePointOrder();
    pointOrder.Insert(Mathf.Clamp(orderIndex + 1, 0, pointOrder.Count), pointIndex);
  }

  public void RemoveConnectionPoint(int pointIndex)
  {
    EnsurePointOrder();
    pointOrder.Remove(pointIndex);
    for (int i = 0; i < pointOrder.Count; i++)
    {
      if (pointOrder[i] > pointIndex)
      {
        pointOrder[i]--;
      }
    }
  }

  public void ReverseConnectionRange(int startIndex, int endIndex)
  {
    EnsurePointOrder();
    while (startIndex < endIndex)
    {
      int swap = pointOrder[startIndex];
      pointOrder[startIndex] = pointOrder[endIndex];
      pointOrder[endIndex] = swap;
      startIndex++;
      endIndex--;
    }
  }

  public int IndexOfConnectionPoint(int pointIndex)
  {
    EnsurePointOrder();
    return pointOrder.IndexOf(pointIndex);
  }

  public void EnsurePointOrder()
  {
    bool[] seen = new bool[points.Count];
    for (int i = pointOrder.Count - 1; i >= 0; i--)
    {
      int pointIndex = pointOrder[i];
      if (pointIndex < 0 || pointIndex >= points.Count || seen[pointIndex])
      {
        pointOrder.RemoveAt(i);
        continue;
      }

      seen[pointIndex] = true;
    }

    for (int i = 0; i < points.Count; i++)
    {
      if (!seen[i])
      {
        pointOrder.Add(i);
      }
    }
  }
}
