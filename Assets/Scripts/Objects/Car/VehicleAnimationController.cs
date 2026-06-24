using UnityEngine;

public class VehicleAnimationController : MonoBehaviour
{
  [SerializeField] private VehicleMotor vehicleMotor;
  [SerializeField] private Animator animator;

  public void Initialize(VehicleMotor motor)
  {
    if (vehicleMotor == null)
    {
      vehicleMotor = motor != null ? motor : GetComponent<VehicleMotor>();
    }

    if (animator == null)
    {
      animator = GetComponentInChildren<Animator>();
    }
  }

  private void Animate()
  {
    Initialize(vehicleMotor);
    if (animator == null || vehicleMotor == null)
    {
      return;
    }

    Vector2 currentDirection = vehicleMotor.CurrentDirectionVector;
    animator.SetFloat("Horizontal", currentDirection.x);
    animator.SetFloat("Vertical", currentDirection.y);
    animator.SetBool("Driving", vehicleMotor.Speed != 0);
  }

  private void Update()
  {
    Animate();
  }

  private void Reset()
  {
    vehicleMotor = GetComponent<VehicleMotor>();
    animator = GetComponentInChildren<Animator>();
  }

  private void OnValidate()
  {
    if (vehicleMotor == null)
    {
      vehicleMotor = GetComponent<VehicleMotor>();
    }

    if (animator == null)
    {
      animator = GetComponentInChildren<Animator>();
    }
  }
}
