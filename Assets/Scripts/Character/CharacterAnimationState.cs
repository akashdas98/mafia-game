using UnityEngine;

[System.Serializable]
public struct CharacterAnimationState
{
  public Vector2 Movement;
  public int LastFacing;
  public bool IsAiming;
  public Vector2 AimDirection;
  public bool HasEquippedWeapon;
  public bool IsTriggerHeld;
  public bool IsReloading;
  public string ActionState;

  public float MovementMagnitude => Movement.magnitude;
}
