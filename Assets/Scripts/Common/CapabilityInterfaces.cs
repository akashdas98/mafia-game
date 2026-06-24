using UnityEngine;

public interface IAimInputReceiver
{
  void Aim(Vector2? position);
}

public interface IMoveInputReceiver
{
  void MoveToward(Vector2 direction);
}

public interface IInteractInputReceiver
{
  void Interact(GameObject actor);
}

public interface IFireInputReceiver
{
  void PullTrigger();
  void ReleaseTrigger();
}

public interface IInventoryInputReceiver
{
  void DropEquippedWeapon();
  void CycleWeapon(int amount);
}

public interface IItemPickupReceiver
{
  void PickUpItem(Item item);
}

public interface IEquippable
{
  Item Item { get; }
  void Equip(GameObject user);
  void Unequip();
}

public interface IVehicleInputReceiver
{
  void SetDirection(Vector2 direction);
  void Forward();
  void Reverse();
  void GradualStop();
  void Brake();
  bool Moving();
  void Exit();
}
