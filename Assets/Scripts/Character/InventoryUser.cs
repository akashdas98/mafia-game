using UnityEngine;

public class InventoryUser : MonoBehaviour, IInventoryInputReceiver, IItemPickupReceiver
{
  [SerializeField] private Inventory inventory;

  private SceneDetails currentSceneDetails;

  public void Initialize()
  {
    if (inventory == null)
    {
      inventory = GetComponentInChildren<Inventory>(true);
    }
  }

  public void PickUpItem(Item item)
  {
    if (item == null)
    {
      return;
    }

    if (inventory != null && inventory.AddItemToInventory(item))
    {
      if (item is IEquippable)
      {
        inventory.SetEquippedWeaponIndex(inventory.GetWeaponsCount() - 1);
        item.transform.localPosition = Vector2.zero;
      }
    }
  }

  public void DropItem(Item item)
  {
    if (item == null || inventory == null)
    {
      return;
    }

    if (item is IEquippable equippable && inventory.GetEquippedItem() == equippable)
    {
      inventory.ResetEquippedWeaponIndex();
    }

    if (inventory.RemoveItemFromInventory(item))
    {
      item.gameObject.SetActive(true);

      if (currentSceneDetails == null)
      {
        return;
      }

      Transform itemsTransform = currentSceneDetails
        .GetTerrain()
        .transform.Find("TileGrid").gameObject
        .transform.Find("Items");

      item.transform.SetParent(itemsTransform);
    }
  }

  public void DropEquippedWeapon()
  {
    if (inventory != null && inventory.GetEquippedWeapon())
    {
      DropItem(inventory.GetEquippedWeapon());
    }
  }

  public void CycleWeapon(int amount)
  {
    if (inventory != null)
    {
      inventory.CycleWeapon(amount);
    }
  }

  private void Reset()
  {
    inventory = GetComponentInChildren<Inventory>(true);
  }

  private void Start()
  {
    Initialize();
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (!other.CompareTag("SceneManager"))
    {
      return;
    }

    SceneDetails sceneDetails = other.GetComponent<SceneDetails>();
    if (sceneDetails != null)
    {
      currentSceneDetails = sceneDetails;
      Debug.Log("Current Scene: " + currentSceneDetails.name);
    }
  }

  private void OnValidate()
  {
    if (inventory == null)
    {
      inventory = GetComponentInChildren<Inventory>(true);
    }
  }
}
