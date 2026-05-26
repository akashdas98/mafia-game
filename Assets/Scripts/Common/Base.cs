using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class Base : MonoBehaviour
{
  [FormerlySerializedAs("_refs")]
  [SerializeField] private EntityRefs entityRefs;

  public EntityRefs EntityRefs
  {
    get
    {
      if (entityRefs == null)
      {
        entityRefs = GetComponentInParent<EntityRefs>();
      }

      return entityRefs;
    }
  }

  protected bool TryGetPart<T>(out T part) where T : class
  {
    EntityRefs refs = EntityRefs;
    if (refs != null)
    {
      return refs.TryGet(out part);
    }

    part = null;
    return false;
  }
}
