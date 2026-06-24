using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Gun : Weapon
{
    [SerializeField] private GunStats stats = new GunStats();
    [SerializeField] private GunFireMode fireMode;

    protected int magSize, ammoLeft;
    protected float firingSpeed, firingRate, distanceDamageFactor;
    protected bool isTriggerPulled = false;

    protected virtual void Awake()
    {
        ApplyStats();
        EnsureFireMode();
    }

    protected virtual void ApplyStats()
    {
        if (stats == null)
        {
            stats = new GunStats();
        }

        baseDamage = stats.baseDamage;
        distanceDamageFactor = stats.distanceDamageFactor;
        magSize = stats.magSize;
        ammoLeft = stats.ammoLeft;
        firingSpeed = stats.firingSpeed;
        firingRate = stats.firingRate;
    }

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
        GunFireMode mode = EnsureFireMode();
        if (mode != null)
        {
            mode.PullTrigger();
        }
    }

    public virtual void ReleaseTrigger()
    {
        GunFireMode mode = EnsureFireMode();
        if (mode != null)
        {
            mode.ReleaseTrigger();
        }
    }

    public virtual void Reload()
    {

    }

    internal bool TryBeginTrigger()
    {
        if (isTriggerPulled)
        {
            return false;
        }

        isTriggerPulled = true;
        return true;
    }

    internal void EndTrigger()
    {
        isTriggerPulled = false;
    }

    internal void FireOnce()
    {
        Fire();
    }

    protected T GetOrCreateFireMode<T>() where T : GunFireMode
    {
        T mode = fireMode as T;
        if (mode == null)
        {
            mode = GetComponent<T>();
        }

        if (mode == null)
        {
            mode = gameObject.AddComponent<T>();
        }

        fireMode = mode;
        fireMode.Initialize(this);
        return mode;
    }

    protected abstract GunFireMode EnsureFireMode();

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
