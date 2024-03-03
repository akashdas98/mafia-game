using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemsController : CharacterControllerHelper
{
  public ItemsController(CharacterController controller, Inventory inventory) : base(controller, inventory) { }

  public void PickUpItem(Item item)
  {
    if (item == null)
    {
      return;
    }

    if (inventory.AddItemToInventory(item))
    {
      if (item is Weapon weapon)
      {
        inventory.SetEquippedWeaponIndex(inventory.GetWeaponsCount() - 1);
        item.transform.localPosition = Vector2.zero;
      }
    }
  }

  public void DropItem(Item item)
  {
    if (item == null)
    {
      return;
    }

    if (item is Weapon weapon)
    {
      if (inventory.GetEquippedWeapon() == weapon)
      {
        inventory.ResetEquippedWeaponIndex();
      }
    }

    if (inventory.RemoveItemFromInventory(item))
    {
      item.gameObject.SetActive(true);

      SceneDetails currentSceneDetails = controller.GetCurrentSceneDetails();
      Transform itemsTransform = currentSceneDetails
        .GetTerrain()
        .transform.Find("TileGrid").gameObject
        .transform.Find("Items");

      item.transform.SetParent(itemsTransform);
    };
  }

  private void OnDropWeapon()
  {
    if (inputs["drop"] != 0 && inventory.GetEquippedWeapon())
    {
      DropItem(inventory.GetEquippedWeapon());
    }
  }

  private void OnScrollInput()
  {
    if (inputs["scroll"] != 0)
    {
      inventory.CycleWeapon(inputs["scroll"] > 0 ? -1 : 1);
    }
  }

  public override void Update()
  {
    base.Update();

    OnScrollInput();
    OnDropWeapon();
  }
}