public sealed class AnimationParameterWriter
{
  private AnimatorParameterRelay relay;

  public AnimationParameterWriter(AnimatorParameterRelay relay)
  {
    this.relay = relay;
  }

  public void SetRelay(AnimatorParameterRelay relay)
  {
    this.relay = relay;
  }

  public void SetBool(string parameterName, bool value)
  {
    relay?.SetBool(parameterName, value);
  }

  public void SetInteger(string parameterName, int value)
  {
    relay?.SetInteger(parameterName, value);
  }

  public void SetFloat(string parameterName, float value)
  {
    relay?.SetFloat(parameterName, value);
  }

  public void SetTrigger(string parameterName)
  {
    relay?.SetTrigger(parameterName);
  }

  public void ResetTrigger(string parameterName)
  {
    relay?.ResetTrigger(parameterName);
  }
}
