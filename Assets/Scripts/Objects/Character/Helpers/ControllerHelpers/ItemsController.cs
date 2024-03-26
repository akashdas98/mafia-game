using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemsController : CharacterControllerHelper
{
  private Inventory inventory;
  public ItemsController(CharacterController controller) : base(controller)
  {
    inventory = controller.Refs.Inventory;
  }

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

      SceneDetails currentSceneDetails = controller.Refs.CurrentSceneDetails;
      Transform itemsTransform = currentSceneDetails
        .GetTerrain()
        .transform.Find("TileGrid").gameObject
        .transform.Find("Items");

      item.transform.SetParent(itemsTransform);
    };
  }

  public void DropEquippedWeapon()
  {
    if (inventory.GetEquippedWeapon())
    {
      DropItem(inventory.GetEquippedWeapon());
    }
  }

  public void CycleWeapon(int n)
  {
    inventory.CycleWeapon(n);
  }

  public override void Update()
  {
    base.Update();
  }
}