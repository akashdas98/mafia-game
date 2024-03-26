using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
  public InputHandler inputHandler;
  private List<InputHandler> inputHandlers = new List<InputHandler>();

  private InputData GetInputData()
  {
    // Gather current input data
    return new InputData
    {
      HorizontalAxis = Input.GetAxis("Horizontal"),
      VerticalAxis = Input.GetAxis("Vertical"),
      Brake = Input.GetAxis("Brake"),
      Action = Input.GetMouseButton(0) ? 1 : 0,
      Interact = Input.GetButtonDown("Interact") ? 1 : 0,
      Drop = Input.GetButtonDown("Drop") ? 1 : 0,
      Scroll = Input.GetAxis("Mouse ScrollWheel"),
      MouseX = Input.mousePosition.x,
      MouseY = Input.mousePosition.y,
      Aim = Input.GetMouseButton(1) ? 1 : 0
    };
  }

  public void SetInputHandler(InputHandler inputHandler)
  {
    this.inputHandler.ResetInputs();
    this.inputHandler = inputHandler;
    inputHandler.Refs.AssignInputManager(this);
  }

  private void InitializeInputHandler()
  {
    if (inputHandler != null)
    {
      inputHandler.Refs.AssignInputManager(this);
    }
  }

  void Start()
  {
    InitializeInputHandler();
  }

  void Update()
  {
    inputHandler.SetInputs(GetInputData());
  }
}
