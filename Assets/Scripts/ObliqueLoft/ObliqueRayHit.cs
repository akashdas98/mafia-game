using UnityEngine;

public struct ObliqueRayHit
{
  public ObliqueLoftCollider Collider;
  public GameObject HitObject;
  public Vector3 Point;
  public float Distance;
  public ObliqueSurfaceType SurfaceType;
  public Vector3 Normal;
  public int FaceIndex;
}
