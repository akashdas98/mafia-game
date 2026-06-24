public class SemiAutoFireMode : GunFireMode
{
  public override void PullTrigger()
  {
    if (gun == null || !gun.TryBeginTrigger())
    {
      return;
    }

    gun.FireOnce();
  }

  public override void ReleaseTrigger()
  {
    gun?.EndTrigger();
  }
}
