using UnityEngine;

public abstract class InputHandler : MonoBehaviour
{
  public InputManager InputManager { get; private set; }

  public void AssignInputManager(InputManager inputManager)
  {
    InputManager = inputManager;
  }

  public abstract void SetInputs(InputData input);

  public abstract void ResetInputs();
}
