using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private Transform weaponsContainer;
    [SerializeField] private Transform miscContainer;

    private List<Item> misc = new List<Item>();
    private List<IEquippable> equippedItems = new List<IEquippable>();

    private int? equippedWeaponIndex;

    public int GetWeaponsCount()
    {
        return equippedItems.Count;
    }

    public bool AddItemToInventory(Item item)
    {
        if (item == null)
        {
            return false;
        }

        Transform inventoryTransform;

        if (item is IEquippable equippable)
        {
            if (equippedItems.Contains(equippable))
            {
                return false;
            }
            equippedItems.Add(equippable);
            inventoryTransform = weaponsContainer;
        }
        else
        {
            if (misc.Contains(item))
            {
                return false;
            }
            misc.Add(item);
            inventoryTransform = miscContainer;
        }

        item.transform.SetParent(inventoryTransform, false);

        return true;
    }

    public bool RemoveItemFromInventory(Item item)
    {
        if (item == null)
        {
            return false;
        }

        if (
            (item is IEquippable equippable && equippedItems.Remove(equippable)) ||
            misc.Remove(item)
        )
        {
            return true;
        }

        return false;
    }

    public Weapon GetEquippedWeapon()
    {
        if (equippedWeaponIndex == null)
        {
            return null;
        }
        if (equippedWeaponIndex < 0 || equippedWeaponIndex > equippedItems.Count - 1)
        {
            equippedWeaponIndex = null;
            return null;
        }
        return equippedItems[equippedWeaponIndex.Value].Item as Weapon;
    }

    public IEquippable GetEquippedItem()
    {
        if (equippedWeaponIndex == null)
        {
            return null;
        }

        if (equippedWeaponIndex < 0 || equippedWeaponIndex > equippedItems.Count - 1)
        {
            equippedWeaponIndex = null;
            return null;
        }

        return equippedItems[equippedWeaponIndex.Value];
    }

    public void SetEquippedWeaponIndex(int index)
    {
        if (index < 0 || index >= equippedItems.Count)
        {
            return;
        }

        GetEquippedItem()?.Item.gameObject.SetActive(false);

        equippedWeaponIndex = index;

        GetEquippedItem()?.Item.gameObject.SetActive(true);
    }

    public void ResetEquippedWeaponIndex()
    {
        if (equippedWeaponIndex == null)
        {
            return;
        }
        equippedWeaponIndex = null;
    }

    public void CycleWeapon(int amount)
    {
        int n = equippedItems.Count;
        if (n == 0)
        {
            return;
        }

        SetEquippedWeaponIndex(Utilities.CircularShift(equippedWeaponIndex ?? 0, amount, n));
    }

    private void Awake()
    {
        ResolveContainers();
    }

    private void Reset()
    {
        ResolveContainers();
    }

    private void OnValidate()
    {
        ResolveContainers();
    }

    private void ResolveContainers()
    {
        if (weaponsContainer == null)
        {
            weaponsContainer = transform.Find(InventoryType.Weapons.ToString());
        }

        if (miscContainer == null)
        {
            miscContainer = transform.Find(InventoryType.Misc.ToString());
        }
    }
}

public enum InventoryType
{
    Misc,
    Weapons
}
