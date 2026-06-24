using UnityEngine;

public abstract class GunFireMode : MonoBehaviour
{
  protected Gun gun;

  public virtual void Initialize(Gun owner)
  {
    gun = owner != null ? owner : GetComponent<Gun>();
  }

  public abstract void PullTrigger();
  public abstract void ReleaseTrigger();
}
