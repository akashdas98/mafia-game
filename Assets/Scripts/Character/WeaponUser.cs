using UnityEngine;

public class WeaponUser : MonoBehaviour, IAimInputReceiver, IFireInputReceiver
{
  [SerializeField] private float gunDistance = 1f;
  [SerializeField] private float gunHeight = 1.5f;
  [SerializeField] private Target target;
  [SerializeField] private Inventory inventory;

  private Gun gun;
  private bool isAiming;
  private bool isTriggerHeld;
  private Vector2 aimDirection;

  public Target Target => target;
  public bool IsAiming => isAiming;
  public bool IsTriggerHeld => isTriggerHeld;
  public bool HasEquippedWeapon => gun != null;
  public Vector2 AimDirection => aimDirection;

  public void Initialize()
  {
    if (target == null)
    {
      target = GetComponentInChildren<Target>(true);
    }

    if (inventory == null)
    {
      inventory = GetComponentInChildren<Inventory>(true);
    }

    if (target != null)
    {
      target.Initialize(this);
    }
  }

  public void Aim(Vector2? position)
  {
    if (gun == null && (target == null || !target.AllowTargetingWithoutEquippedItem))
    {
      return;
    }

    if (target == null)
    {
      return;
    }

    if (position is Vector2 pos)
    {
      target.AimAt(pos);
      isAiming = target.HasActiveAim;
      Vector2 aimVector = pos - GetAimOriginPosition();
      aimDirection = aimVector.sqrMagnitude > Mathf.Epsilon ? aimVector.normalized : Vector2.zero;
      SetGunPosition();
      Render();
    }
    else
    {
      target.Reset();
      isAiming = false;
      aimDirection = Vector2.zero;
      ResetGunPosition();
    }
  }

  public Gun GetGun()
  {
    return gun;
  }

  public Vector2 GetAimOriginPosition()
  {
    return target != null && target.HasAimOrigin ? target.GetAimOriginPosition() : GetGunInitPosition();
  }

  public Vector2 GetGunVisualPosition()
  {
    return target != null && target.HasGunPoint ? target.GetGunPointPosition() : GetGunInitPosition();
  }

  public float GetMinimumTargetDistance()
  {
    return Vector2.Distance(GetAimOriginPosition(), GetGunVisualPosition());
  }

  public bool IsInsideMinimumTargetRadius(Vector2 position)
  {
    float minimumDistance = GetMinimumTargetDistance();
    return minimumDistance > Mathf.Epsilon && Vector2.Distance(GetAimOriginPosition(), position) <= minimumDistance;
  }

  public float GetGunDistance()
  {
    return gunDistance;
  }

  public float GetGunHeight()
  {
    if (target != null && target.HasAimOrigin)
    {
      return Mathf.Max(0f, target.GetAimOriginPosition().y - transform.position.y);
    }

    return gunHeight;
  }

  public void PullTrigger()
  {
    isTriggerHeld = true;
    gun?.PullTrigger();
  }

  public void ReleaseTrigger()
  {
    isTriggerHeld = false;
    gun?.ReleaseTrigger();
  }

  public void Tick()
  {
    SetGun();
  }

  private Vector2 GetGunInitPosition()
  {
    return (Vector2)transform.position + new Vector2(0, gunHeight);
  }

  private void SetGunPosition()
  {
    if (target == null)
    {
      return;
    }

    Vector2 offset = target.HasGunPoint
      ? target.GetGunPointPosition()
      : Utilities.FindPointAtDistance(GetAimOriginPosition(), target.GetChosenPosition(), gunDistance);
    gun?.SetPosition(offset);
  }

  private void ResetGunPosition()
  {
    gun?.SetPosition(GetGunVisualPosition());
  }

  private void Render()
  {
    if (gun != null && target != null)
    {
      Debug.DrawLine(GetAimOriginPosition(), target.GetChosenPosition(), Color.red, 0);
    }
  }

  private void SetGun()
  {
    Weapon equippedWeapon = inventory != null ? inventory.GetEquippedWeapon() : null;
    gun = equippedWeapon as Gun;
  }

  private void Reset()
  {
    target = GetComponentInChildren<Target>(true);
    inventory = GetComponentInChildren<Inventory>(true);
  }

  private void Start()
  {
    Initialize();
  }

  private void Update()
  {
    Initialize();
    Tick();
  }

  private void OnValidate()
  {
    if (target == null)
    {
      target = GetComponentInChildren<Target>(true);
    }

    if (inventory == null)
    {
      inventory = GetComponentInChildren<Inventory>(true);
    }
  }
}
