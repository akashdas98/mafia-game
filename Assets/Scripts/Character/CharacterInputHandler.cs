public class CharacterInputHandler : InputHandler
{
  private CharacterInputState inputState;
  private PlayerInputRouter inputRouter;

  public CharacterInputState InputState => inputState;

  public override void SetInputs(InputData input)
  {
    inputState = new CharacterInputState
    {
      HorizontalInput = input.HorizontalAxis,
      VerticalInput = input.VerticalAxis,
      ActionInput = input.Action,
      Interact = input.Interact,
      Drop = input.Drop,
      Scroll = input.Scroll,
      Aim = input.Aim,
      MouseX = input.MouseX,
      MouseY = input.MouseY
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

    inputRouter = GetComponent<PlayerInputRouter>();
    if (inputRouter == null)
    {
      inputRouter = gameObject.AddComponent<PlayerInputRouter>();
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
    inputRouter.Route(inputState);
  }
}
