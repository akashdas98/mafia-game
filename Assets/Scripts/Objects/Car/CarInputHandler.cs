using UnityEngine;

public class CarInputHandler : InputHandler
{
  private CarInputState inputState;
  private VehicleInputRouter inputRouter;

  public CarInputState InputState => inputState;

  public override void SetInputs(InputData input)
  {
    inputState = new CarInputState
    {
      BrakeInput = input.Brake > 0 ? 1 : 0,
      HorizontalInput = input.HorizontalAxis,
      VerticalInput = input.VerticalAxis,
      Interact = input.Interact
    };
  }

  public override void ResetInputs()
  {
    inputState = default;
  }

  private void EnsureInputRouter()
  {
    if (inputRouter != null)
    {
      inputRouter.Initialize();
      return;
    }

    inputRouter = GetComponent<VehicleInputRouter>();
    if (inputRouter == null)
    {
      inputRouter = gameObject.AddComponent<VehicleInputRouter>();
    }

    inputRouter.Initialize();
  }

  void Start()
  {
    EnsureInputRouter();
  }

  void Update()
  {
    EnsureInputRouter();
    inputRouter.RouteFrameInput(inputState);
  }

  void FixedUpdate()
  {
    EnsureInputRouter();
    inputRouter.RoutePhysicsInput(inputState);
  }
}
