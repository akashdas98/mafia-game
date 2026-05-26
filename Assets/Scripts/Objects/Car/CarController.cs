using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CarController : Controller
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

  public Rigidbody2D rigidBody;
  public CinemachineVirtualCamera cinemachineCamera;
  private Vector2 direction = new Vector2(1, 0);
  private GameObject? driver;
  private int currentDirectionIndex = 0;
  private float steeringProgressDegrees = 0f;

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

    // Get the current direction and target angle
    float currentDirection = GetCurrentDirection();
    float targetAngle = GetTargetAngle();
    float angleDifference = AngleDifference(currentDirection, targetAngle);

    // Adjust acceleration based on the angle difference
    float turnFactor = Mathf.Clamp01(angleDifference / 180f) * turnFactorMultiplier;

    // Apply acceleration
    speed += forward - (speed * turnFactor);

    // Clamp speed within the specified range
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

  public void Enter(GameObject player)
  {
    CharacterController playerController = player.GetComponent<CharacterController>();
    if (playerController != null)
    {
      InputHandler playerInputHandler = playerController.EntityRefs?.Get<InputHandler>();
      InputManager inputManager = playerInputHandler != null ? playerInputHandler.InputManager : player.GetComponentInParent<InputManager>();
      InputHandler carInputHandler = EntityRefs?.Get<InputHandler>();
      if (inputManager == null || carInputHandler == null)
      {
        return;
      }

      driver = player;
      player.transform.parent = transform;
      playerController.DisableVisibility();
      playerController.SwitchToKinematic(true);
      inputManager.SetInputHandler(carInputHandler);
    }
  }
  public void Exit()
  {
    if (driver == null) return;

    CharacterController driverController = driver.GetComponent<CharacterController>();
    if (driverController != null)
    {
      // Reset the parent of the driver, reset the rotation and make visible
      driver.transform.parent = null;
      driver.transform.eulerAngles = new Vector3(driver.transform.eulerAngles.x, driver.transform.eulerAngles.y, 0);
      driverController.EnableVisibility();
      driverController.SwitchToKinematic(false);
      InputHandler driverInputHandler = driverController.EntityRefs?.Get<InputHandler>();
      InputManager inputManager = driverInputHandler != null ? driverInputHandler.InputManager : driver.GetComponentInParent<InputManager>();
      if (inputManager != null && driverInputHandler != null)
      {
        inputManager.SetInputHandler(driverInputHandler);
      }
    }
  }

  private float RoundTo2(float value)
  {
    return Mathf.Round(value * 100.0f) / 100.0f;
  }

  private float NormalizeAngle(float degrees)
  {
    degrees = RoundTo2(degrees);
    // Use modulo 360 to ensure the result is in the range [0, 360)
    float result = degrees % 360;

    // Ensure the result is positive
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

  private void Animate()
  {
    Animator animator = EntityRefs?.Get<Animator>();
    if (animator == null)
    {
      return;
    }

    float currentDirection = GetCurrentDirection();
    float angleInRadians = Mathf.Deg2Rad * currentDirection; // Convert degrees to radians
    float posX = Mathf.Cos(angleInRadians);
    float posY = Mathf.Sin(angleInRadians);
    animator.SetFloat("Horizontal", posX);
    animator.SetFloat("Vertical", posY);
    animator.SetBool("Driving", speed == 0 ? false : true);
  }

  private void Move()
  {
    Vector2 forwardDirection = DirectionFromIndex(currentDirectionIndex);
    rigidBody.velocity = forwardDirection * speed;
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

  private void Drive()
  {
    transform.localRotation = Quaternion.identity;
    Move();
    if (speed != 0)
    {
      Turn();
    }
  }

  // Update is called once per frame
  void Update()
  {
    transform.localRotation = Quaternion.identity;
    Animate();
  }

  void FixedUpdate()
  {
    Drive();
  }
}
