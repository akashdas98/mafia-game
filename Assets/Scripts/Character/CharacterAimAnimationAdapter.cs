using UnityEngine;

public class CharacterAimAnimationAdapter : MonoBehaviour, IAnimationParameterContributor
{
  private static readonly string IsAimingParameter = "IsAiming";
  private static readonly string AimHorizontalParameter = "AimHorizontal";
  private static readonly string AimVerticalParameter = "AimVertical";
  private static readonly string AimAngleParameter = "AimAngle";
  private static readonly string AimBucketParameter = "AimBucket";
  private static readonly string HasWeaponParameter = "HasWeapon";
  private static readonly string TriggerHeldParameter = "TriggerHeld";

  [SerializeField] private WeaponUser weaponUser;

  public void Initialize(WeaponUser fallbackWeaponUser)
  {
    if (weaponUser == null)
    {
      weaponUser = fallbackWeaponUser != null ? fallbackWeaponUser : GetComponent<WeaponUser>();
    }
  }

  public void Contribute(AnimationParameterWriter writer)
  {
    Initialize(null);

    bool isAiming = false;
    bool hasWeapon = false;
    bool triggerHeld = false;
    Vector2 aimDirection = Vector2.zero;

    if (weaponUser != null)
    {
      isAiming = weaponUser.IsAiming;
      hasWeapon = weaponUser.HasEquippedWeapon;
      triggerHeld = weaponUser.IsTriggerHeld;
      aimDirection = weaponUser.AimDirection;
    }

    writer.SetBool(IsAimingParameter, isAiming);
    writer.SetFloat(AimHorizontalParameter, aimDirection.x);
    writer.SetFloat(AimVerticalParameter, aimDirection.y);
    writer.SetFloat(AimAngleParameter, GetAimAngle(aimDirection));
    writer.SetInteger(AimBucketParameter, GetAimBucket(aimDirection));
    writer.SetBool(HasWeaponParameter, hasWeapon);
    writer.SetBool(TriggerHeldParameter, triggerHeld);
  }

  private static float GetAimAngle(Vector2 aimDirection)
  {
    if (aimDirection.sqrMagnitude <= Mathf.Epsilon)
    {
      return 0f;
    }

    float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
    return angle < 0f ? angle + 360f : angle;
  }

  private static int GetAimBucket(Vector2 aimDirection)
  {
    if (aimDirection.sqrMagnitude <= Mathf.Epsilon)
    {
      return 0;
    }

    float angle = GetAimAngle(aimDirection);
    return Mathf.RoundToInt(angle / 45f) % 8;
  }

  private void Reset()
  {
    weaponUser = GetComponent<WeaponUser>();
  }

  private void OnValidate()
  {
    if (weaponUser == null)
    {
      weaponUser = GetComponent<WeaponUser>();
    }
  }
}
