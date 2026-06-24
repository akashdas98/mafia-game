using UnityEngine;

public class PlayerInputRouter : MonoBehaviour
{
  [SerializeField] private CharacterMotor moveReceiver;
  [SerializeField] private CharacterInteractor interactReceiver;
  [SerializeField] private WeaponUser weaponUser;
  [SerializeField] private InventoryUser inventoryUser;

  public void Initialize()
  {
    if (moveReceiver == null)
    {
      moveReceiver = GetComponent<CharacterMotor>();
    }

    if (interactReceiver == null)
    {
      interactReceiver = GetComponent<CharacterInteractor>();
    }

    if (weaponUser == null)
    {
      weaponUser = GetComponent<WeaponUser>();
    }

    if (inventoryUser == null)
    {
      inventoryUser = GetComponent<InventoryUser>();
    }
  }

  public void Route(CharacterInputState inputState)
  {
    Initialize();

    RouteMovement(inputState);
    RouteInteraction(inputState);
    RouteAim(inputState);
    RouteFire(inputState);
    RouteInventory(inputState);
  }

  private void RouteMovement(CharacterInputState inputState)
  {
    if (moveReceiver != null)
    {
      moveReceiver.MoveToward(inputState.MovementInput);
    }
  }

  private void RouteInteraction(CharacterInputState inputState)
  {
    if (inputState.HasInteractInput && interactReceiver != null)
    {
      interactReceiver.Interact(gameObject);
    }
  }

  private void RouteAim(CharacterInputState inputState)
  {
    if (weaponUser == null)
    {
      return;
    }

    Vector2? aimPosition = inputState.HasAimInput && Camera.main != null
      ? Camera.main.ScreenToWorldPoint(inputState.MouseScreenPosition)
      : null;

    weaponUser.Aim(aimPosition);
  }

  private void RouteFire(CharacterInputState inputState)
  {
    if (weaponUser == null)
    {
      return;
    }

    if (inputState.HasAimInput && inputState.HasActionInput)
    {
      weaponUser.PullTrigger();
    }
    else
    {
      weaponUser.ReleaseTrigger();
    }
  }

  private void RouteInventory(CharacterInputState inputState)
  {
    if (inventoryUser == null)
    {
      return;
    }

    if (inputState.HasScrollInput)
    {
      inventoryUser.CycleWeapon(inputState.Scroll > 0 ? -1 : 1);
    }

    if (inputState.HasDropInput)
    {
      inventoryUser.DropEquippedWeapon();
    }
  }

  private void Reset()
  {
    Initialize();
  }

  private void OnValidate()
  {
    Initialize();
  }
}
