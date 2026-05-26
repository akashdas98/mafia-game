using UnityEngine;

public struct ObliqueRay
{
  public readonly Vector3 From;
  public readonly Vector3 To;
  public readonly Vector3 Direction;
  public readonly float Length;

  public ObliqueRay(Vector3 from, Vector3 to)
  {
    From = from;
    To = to;
    Vector3 delta = to - from;
    Length = delta.magnitude;
    Direction = Length > Mathf.Epsilon ? delta / Length : Vector3.zero;
  }

  public Vector3 GetPoint(float distance)
  {
    return From + Direction * distance;
  }

  public static Vector3 FromGround(Vector2 groundPosition, float height)
  {
    return new Vector3(groundPosition.x, height, groundPosition.y);
  }
}
