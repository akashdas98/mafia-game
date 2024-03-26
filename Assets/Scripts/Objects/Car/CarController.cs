using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CarController : Controller
{
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
      driver = player;

      InputManager cm = player.GetComponent<Refs>().InputManager;

      player.transform.parent = transform;
      playerController.DisableVisibility();
      playerController.SwitchToKinematic(true);
      cm.SetInputHandler(Refs.InputHandler);
    }
  }
  public void Exit()
  {
    if (driver == null) return;

    Refs driverRefs = driver.GetComponent<Refs>();
    CharacterController driverController = (CharacterController)driverRefs.Controller;
    if (driverController != null)
    {
      // Reset the parent of the driver, reset the rotation and make visible
      driver.transform.parent = null;
      driver.transform.eulerAngles = new Vector3(driver.transform.eulerAngles.x, driver.transform.eulerAngles.y, 0);
      driverController.EnableVisibility();
      driverController.SwitchToKinematic(false);
      Refs.InputManager.SetInputHandler(driverRefs.InputHandler);
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
    Vector3 rotation = transform.rotation.eulerAngles;
    float currentDirection = NormalizeAngle(rotation.z);

    return currentDirection;
  }

  private float GetTargetAngle()
  {
    return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
  }

  private int DetermineTurnDirection(float currentDirection, float targetAngle)
  {
    float shortestDistance = Mathf.DeltaAngle(currentDirection, targetAngle);

    if (shortestDistance < 0)
    {
      return -1;
    }
    else if (shortestDistance > 0)
    {
      return 1;
    }
    else
    {
      return 0;
    }
  }

  private float OppositeAngle(float angle)
  {
    float opposite = (RoundTo2(NormalizeAngle(angle)) + 180) % 360;
    return opposite < 0 ? 360 + opposite : opposite;
  }

  private float AngleDifference(float a, float b)
  {
    return RoundTo2(Mathf.Abs(Mathf.DeltaAngle(a, b)));
  }

  private void Animate()
  {
    Animator animator = Refs.Animator;
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
    Vector2 forwardDirection = transform.right;
    rigidBody.velocity = forwardDirection * speed;
  }

  private void Turn()
  {
    float currentDirection = GetCurrentDirection();
    float targetAngle = GetTargetAngle();
    float reverseAngle = OppositeAngle(targetAngle);
    float angleDifference = AngleDifference(currentDirection, targetAngle);
    float reverseDifference = AngleDifference(currentDirection, reverseAngle);
    float turnDirection = DetermineTurnDirection(currentDirection, targetAngle) * speed < 0 ? -1 : 1;
    float absSpeed = Mathf.Abs(speed);

    float speedFactor = absSpeed < speedFactorThreshold ? 0 : Mathf.Clamp01(absSpeed / maxSpeed) * speedFactorMultiplier;
    float increment = turnDirection * rotationSpeed * (1 - speedFactor);

    float difference = speed >= 0 ? angleDifference : reverseDifference;

    if (difference < 3)
    {
      transform.Rotate(new Vector3(0, 0, difference * turnDirection));
    }
    else if ((direction.x != 0 || direction.y != 0) && speed != 0)
    {
      transform.Rotate(new Vector3(0, 0, increment));
    }
  }

  private void Drive()
  {
    Move();
    if (speed != 0)
    {
      Turn();
    }
  }

  // Update is called once per frame
  void Update()
  {
    Animate();
  }

  void FixedUpdate()
  {
    Drive();
  }
}
