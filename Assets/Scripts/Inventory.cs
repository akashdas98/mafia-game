using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField]
    private GameObject owner;
    private List<Weapon> weapons = new List<Weapon>();

    private Weapon? equippedWeapon;

    public void SetEquippedWeapon(Weapon weapon)
    {
        equippedWeapon = weapon;
    }

    public Weapon GetEquippedWeapon()
    {
        return equippedWeapon;
    }

    public void ResetEquippedWeapon()
    {
        if (equippedWeapon == null)
        {
            return;
        }
        equippedWeapon = null;
    }
}
