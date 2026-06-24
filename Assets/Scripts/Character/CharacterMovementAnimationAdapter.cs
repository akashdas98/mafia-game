using UnityEngine;

public class CharacterMovementAnimationAdapter : MonoBehaviour, IAnimationParameterContributor
{
  private static readonly string LastFacingParameter = "LastFacing";
  private static readonly string HorizontalParameter = "Horizontal";
  private static readonly string VerticalParameter = "Vertical";
  private static readonly string MagnitudeParameter = "Magnitude";

  [SerializeField] private CharacterMotor motor;

  public void Initialize(CharacterMotor fallbackMotor)
  {
    if (motor == null)
    {
      motor = fallbackMotor != null ? fallbackMotor : GetComponent<CharacterMotor>();
    }
  }

  public void Contribute(AnimationParameterWriter writer)
  {
    Initialize(null);
    if (motor == null)
    {
      return;
    }

    Vector2 movement = motor.Movement;
    writer.SetFloat(LastFacingParameter, motor.LastFacing);
    writer.SetFloat(HorizontalParameter, movement.x);
    writer.SetFloat(VerticalParameter, movement.y);
    writer.SetFloat(MagnitudeParameter, movement.magnitude);
  }

  private void Reset()
  {
    motor = GetComponent<CharacterMotor>();
  }

  private void OnValidate()
  {
    if (motor == null)
    {
      motor = GetComponent<CharacterMotor>();
    }
  }
}

// Right       1, 0
// UpRight     0.707, 0.707
// Up          0, 1
// UpLeft     -0.707, 0.707
// Left       -1, 0
// DownLeft   -0.707, -0.707
// Down        0, -1
// DownRight   0.707, -0.707