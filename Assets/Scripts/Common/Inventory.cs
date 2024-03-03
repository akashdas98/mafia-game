using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private List<Item> misc = new List<Item>();
    private List<Weapon> weapons = new List<Weapon>();

    private int? equippedWeaponIndex;

    public int GetWeaponsCount()
    {
        return weapons.Count;
    }

    public bool AddItemToInventory(Item item)
    {
        if (item == null)
        {
            return false;
        }

        string inventoryType;

        if (item is Weapon weapon)
        {
            if (weapons.Contains(weapon))
            {
                return false;
            }
            weapons.Add(weapon);
            inventoryType = InventoryType.Weapons.ToString();
        }
        else
        {
            if (misc.Contains(item))
            {
                return false;
            }
            misc.Add(item);
            inventoryType = InventoryType.Misc.ToString();
        }

        Transform inventoryTransform = gameObject.transform.Find("Inventory")?.Find(inventoryType);
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
            (item is Weapon weapon && weapons.Remove(weapon)) ||
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
        if (equippedWeaponIndex < 0 || equippedWeaponIndex > weapons.Count - 1)
        {
            equippedWeaponIndex = null;
            return null;
        }
        return weapons[equippedWeaponIndex.Value];
    }

    public void SetEquippedWeaponIndex(int index)
    {
        if (index < 0 || index > weapons.Count)
        {
            return;
        }

        GetEquippedWeapon()?.gameObject.SetActive(false);

        equippedWeaponIndex = index;

        GetEquippedWeapon().gameObject.SetActive(true);
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
        int n = weapons.Count;
        if (n == 0)
        {
            return;
        }

        SetEquippedWeaponIndex(Utilities.CircularShift(equippedWeaponIndex ?? 0, amount, n));
    }
}

public enum InventoryType
{
    Misc,
    Weapons
}