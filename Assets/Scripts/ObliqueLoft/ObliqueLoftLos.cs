using System.Collections.Generic;
using UnityEngine;

public static class ObliqueLoftLos
{
  public static ObliqueRay CreateShotRay(Vector2 shooterGroundPosition, float shootHeight, Vector2 targetGroundPosition, float targetHeight)
  {
    return new ObliqueRay(
      ObliqueRay.FromGround(shooterGroundPosition, shootHeight),
      ObliqueRay.FromGround(targetGroundPosition, targetHeight)
    );
  }

  public static bool HasLineOfSight(Vector2 shooterGroundPosition, float shootHeight, Vector2 targetGroundPosition, float targetHeight)
  {
    return !TryGetClosestHit(shooterGroundPosition, shootHeight, targetGroundPosition, targetHeight, out _);
  }

  public static bool CanHitTargetHeight(Vector2 shooterGroundPosition, float shootHeight, Vector2 targetGroundPosition, float targetHeight)
  {
    return HasLineOfSight(shooterGroundPosition, shootHeight, targetGroundPosition, targetHeight);
  }

  public static bool TryGetClosestHit(Vector2 shooterGroundPosition, float shootHeight, Vector2 targetGroundPosition, float targetHeight, out ObliqueRayHit hit)
  {
    ObliqueRay ray = CreateShotRay(shooterGroundPosition, shootHeight, targetGroundPosition, targetHeight);
    return ObliqueRaycaster.TryRaycast(ray, Object.FindObjectsOfType<ObliqueLoftCollider>(), out hit);
  }

  public static bool TryGetClosestHit(Vector2 shooterGroundPosition, float shootHeight, Vector2 targetGroundPosition, float targetHeight, IEnumerable<ObliqueLoftCollider> colliders, out ObliqueRayHit hit)
  {
    ObliqueRay ray = CreateShotRay(shooterGroundPosition, shootHeight, targetGroundPosition, targetHeight);
    return ObliqueRaycaster.TryRaycast(ray, colliders, out hit);
  }
}
