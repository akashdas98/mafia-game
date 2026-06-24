using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class FullAuto : Gun
{
    protected override GunFireMode EnsureFireMode()
    {
        return GetOrCreateFireMode<FullAutoFireMode>();
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
