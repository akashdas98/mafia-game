using UnityEngine;

public class VehicleInputRouter : MonoBehaviour
{
  [SerializeField] private VehicleMotor vehicle;

  public void Initialize()
  {
    if (vehicle == null)
    {
      vehicle = GetComponent<VehicleMotor>();
    }

    if (vehicle != null)
    {
      vehicle.Initialize(null);
    }
  }

  public void RouteFrameInput(CarInputState inputState)
  {
    IVehicleInputReceiver vehicle = GetVehicle();
    if (vehicle == null)
    {
      return;
    }

    vehicle.SetDirection(inputState.DirectionInput);
    if (inputState.HasInteractInput)
    {
      vehicle.Exit();
    }
  }

  public void RoutePhysicsInput(CarInputState inputState)
  {
    IVehicleInputReceiver vehicle = GetVehicle();
    if (vehicle == null)
    {
      return;
    }

    if (!inputState.IsBraking)
    {
      if (inputState.HasDirectionInput)
      {
        vehicle.Forward();
      }
      else
      {
        vehicle.GradualStop();
      }

      return;
    }

    if (vehicle.Moving())
    {
      vehicle.Brake();
    }
    else if (inputState.HasDirectionInput)
    {
      vehicle.Reverse();
    }
    else
    {
      vehicle.GradualStop();
    }
  }

  private IVehicleInputReceiver GetVehicle()
  {
    Initialize();
    return vehicle;
  }

  private void Reset()
  {
    Initialize();
  }

  private void OnValidate()
  {
    Initialize();
  }
}
