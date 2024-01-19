using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    protected GameObject user;
    protected double baseDamage;

    public void Equip(GameObject obj)
    {
        user = obj;
    }

    public void Unequip()
    {
        user = null;
    }
}
