public class FullAutoFireMode : GunFireMode
{
  public override void PullTrigger()
  {
    gun?.TryBeginTrigger();
  }

  public override void ReleaseTrigger()
  {
    gun?.EndTrigger();
  }
}
