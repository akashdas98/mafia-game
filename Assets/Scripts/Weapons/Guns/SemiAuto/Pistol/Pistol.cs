using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pistol : SemiAuto
{
    void Awake()
    {
        this.distanceDamageFactor = 0.2f;
        this.baseDamage = 25;
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
