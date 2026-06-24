using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SemiAuto : Gun
{
    protected override GunFireMode EnsureFireMode()
    {
        return GetOrCreateFireMode<SemiAutoFireMode>();
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
