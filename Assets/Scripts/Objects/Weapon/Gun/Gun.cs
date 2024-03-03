using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Gun : Weapon
{
    protected int magSize, ammoLeft;
    protected float firingSpeed, firingRate, distanceDamageFactor;
    protected bool isTriggerPulled = false;
    public virtual double GetDamage(double distanceTravelled)
    {
        if (distanceTravelled == 0)
        {
            return baseDamage;
        }
        else
        {
            return baseDamage - (distanceTravelled * distanceDamageFactor);
        }
    }

    public bool IsTriggerPulled()
    {
        return isTriggerPulled;
    }

    public virtual void PullTrigger()
    {
        if (isTriggerPulled)
        {
            return;
        }
        isTriggerPulled = true;
    }

    public virtual void ReleaseTrigger()
    {
        if (!isTriggerPulled)
        {
            return;
        }
        isTriggerPulled = false;
    }

    public virtual void Reload()
    {

    }

    protected abstract void Fire();

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
