using UnityEngine;

public class VehicleMotor : MonoBehaviour, IVehicleInputReceiver
{
  private const int DirectionCount = 16;
  private const float DegreesPerDirection = 360f / DirectionCount;

  public float
      forward = 0.2f,
      reverse = 0.05f,
      speed = 0f,
      rotationSpeed = 3f,
      maxSpeed = 50f, minSpeed = -10f,
      brake = 0.05f,
      friction = 0.01f,
      turnFactorMultiplier = 0.1f,
      speedFactorThreshold = 20,
      speedFactorMultiplier = 1;

  [SerializeField] private Rigidbody2D rigidBody;
  [SerializeField] private VehiclePossession vehiclePossession;
  private Vector2 direction = new Vector2(1, 0);
  private int currentDirectionIndex = 0;
  private float steeringProgressDegrees = 0f;

  public float Speed => speed;
  public Vector2 CurrentDirectionVector => DirectionFromIndex(currentDirectionIndex);

  public void Initialize(Rigidbody2D fallbackRigidBody)
  {
    if (rigidBody == null)
    {
      rigidBody = fallbackRigidBody != null ? fallbackRigidBody : GetComponent<Rigidbody2D>();
    }

    if (vehiclePossession == null)
    {
      vehiclePossession = GetComponent<VehiclePossession>();
    }

    if (vehiclePossession != null)
    {
      vehiclePossession.Initialize();
    }
  }

  public void SetDirection(Vector2 direction)
  {
    this.direction = direction;
  }

  public void Forward()
  {
    if (speed <= 10)
    {
      speed += forward;
      speed = Mathf.Clamp(speed, minSpeed, maxSpeed);
      return;
    }

    float currentDirection = GetCurrentDirection();
    float targetAngle = GetTargetAngle();
    float angleDifference = AngleDifference(currentDirection, targetAngle);
    float turnFactor = Mathf.Clamp01(angleDifference / 180f) * turnFactorMultiplier;

    speed += forward - (speed * turnFactor);
    speed = Mathf.Clamp(speed, minSpeed, maxSpeed);
  }

  public void Reverse()
  {
    speed -= reverse;
    if (speed < minSpeed)
    {
      speed = minSpeed;
    }
  }

  public void GradualStop()
  {
    if (Mathf.Abs(speed) <= 2)
    {
      speed = 0;
      return;
    }

    speed = speed - (speed * friction);
  }

  public void Brake()
  {
    if (speed <= 3)
    {
      speed = 0;
      return;
    }

    speed = speed - (speed * brake);
  }

  public bool Moving()
  {
    return speed > 0;
  }

  public void Exit()
  {
    Initialize(null);
    if (vehiclePossession != null)
    {
      vehiclePossession.Exit();
    }
  }

  public void Drive()
  {
    transform.localRotation = Quaternion.identity;
    Move();
    if (speed != 0)
    {
      Turn();
    }
  }

  private float RoundTo2(float value)
  {
    return Mathf.Round(value * 100.0f) / 100.0f;
  }

  private float NormalizeAngle(float degrees)
  {
    degrees = RoundTo2(degrees);
    float result = degrees % 360;
    if (result < 0)
    {
      result += 360;
    }

    return result;
  }

  private float GetCurrentDirection()
  {
    return DirectionAngle(currentDirectionIndex);
  }

  private float GetTargetAngle()
  {
    return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
  }

  private float DirectionAngle(int directionIndex)
  {
    return NormalizeAngle(directionIndex * DegreesPerDirection);
  }

  private Vector2 DirectionFromIndex(int directionIndex)
  {
    float angleInRadians = Mathf.Deg2Rad * DirectionAngle(directionIndex);
    return new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians));
  }

  private int DirectionIndexFromAngle(float angle)
  {
    float normalizedAngle = NormalizeAngle(angle);
    int directionIndex = Mathf.RoundToInt(normalizedAngle / DegreesPerDirection) % DirectionCount;
    return directionIndex < 0 ? directionIndex + DirectionCount : directionIndex;
  }

  private int OppositeDirectionIndex(int directionIndex)
  {
    return (directionIndex + (DirectionCount / 2)) % DirectionCount;
  }

  private int ShortestDirectionDelta(int fromDirectionIndex, int toDirectionIndex)
  {
    int delta = (toDirectionIndex - fromDirectionIndex + DirectionCount) % DirectionCount;
    if (delta > DirectionCount / 2)
    {
      delta -= DirectionCount;
    }

    return delta;
  }

  private float AngleDifference(float a, float b)
  {
    return RoundTo2(Mathf.Abs(Mathf.DeltaAngle(a, b)));
  }

  private void Move()
  {
    if (rigidBody == null)
    {
      rigidBody = GetComponent<Rigidbody2D>();
    }

    if (rigidBody != null)
    {
      rigidBody.linearVelocity = CurrentDirectionVector * speed;
    }
  }

  private void Turn()
  {
    if ((direction.x == 0 && direction.y == 0) || speed == 0)
    {
      steeringProgressDegrees = 0;
      return;
    }

    int inputDirectionIndex = DirectionIndexFromAngle(GetTargetAngle());
    int targetDirectionIndex = speed >= 0 ? inputDirectionIndex : OppositeDirectionIndex(inputDirectionIndex);
    int directionDelta = ShortestDirectionDelta(currentDirectionIndex, targetDirectionIndex);

    if (directionDelta == 0)
    {
      steeringProgressDegrees = 0;
      return;
    }

    int turnDirection = directionDelta < 0 ? -1 : 1;
    float absSpeed = Mathf.Abs(speed);
    float speedFactor = absSpeed < speedFactorThreshold ? 0 : Mathf.Clamp01(absSpeed / maxSpeed) * speedFactorMultiplier;
    float increment = rotationSpeed * (1 - speedFactor);

    if (increment <= 0)
    {
      return;
    }

    steeringProgressDegrees += increment;

    while (steeringProgressDegrees >= DegreesPerDirection && currentDirectionIndex != targetDirectionIndex)
    {
      currentDirectionIndex = (currentDirectionIndex + turnDirection + DirectionCount) % DirectionCount;
      steeringProgressDegrees -= DegreesPerDirection;

      directionDelta = ShortestDirectionDelta(currentDirectionIndex, targetDirectionIndex);
      if (directionDelta == 0)
      {
        steeringProgressDegrees = 0;
        break;
      }

      turnDirection = directionDelta < 0 ? -1 : 1;
    }
  }

  private void Reset()
  {
    rigidBody = GetComponent<Rigidbody2D>();
    vehiclePossession = GetComponent<VehiclePossession>();
  }

  private void Start()
  {
    Initialize(null);
  }

  private void Update()
  {
    transform.localRotation = Quaternion.identity;
  }

  private void FixedUpdate()
  {
    Initialize(null);
    Drive();
  }

  private void OnValidate()
  {
    if (rigidBody == null)
    {
      rigidBody = GetComponent<Rigidbody2D>();
    }

    if (vehiclePossession == null)
    {
      vehiclePossession = GetComponent<VehiclePossession>();
    }
  }
}
