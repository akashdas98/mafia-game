using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Base : MonoBehaviour
{
  [SerializeField]
  private Refs _refs;
  public Refs Refs => _refs;
}