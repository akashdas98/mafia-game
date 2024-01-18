using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SemiAuto : Gun
{
    public override void PullTrigger()
    {
        if (isTriggerPulled)
        {
            return;
        }
        Fire();
        base.PullTrigger();
    }
    public override void ReleaseTrigger()
    {
        if (!isTriggerPulled)
        {
            return;
        }
        base.ReleaseTrigger();
    }

    protected override void Fire()
    {

    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
