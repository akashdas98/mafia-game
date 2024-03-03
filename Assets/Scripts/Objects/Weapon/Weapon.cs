using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : Item
{
    protected GameObject user;
    protected double baseDamage;
    protected Vector2 position = new Vector2(0, 0);

    public void Equip(GameObject obj)
    {
        user = obj;
    }

    public void Unequip()
    {
        user = null;
    }

    public void SetPosition(Vector2 position)
    {
        this.transform.position = position;
        this.position = position;
    }

    public Vector2 GetPosition()
    {
        return this.position;
    }
}
