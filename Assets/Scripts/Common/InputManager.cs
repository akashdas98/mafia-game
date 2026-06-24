using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class InputManager : MonoBehaviour
{
  [SerializeField] private CinemachineVirtualCamera cinemachineCamera;
  [SerializeField] private bool updateCinemachineFollow = true;

  public InputHandler inputHandler;
  private List<InputHandler> inputHandlers = new List<InputHandler>();
  private bool warnedMissingInputHandler;

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
    if (this.inputHandler != null)
    {
      this.inputHandler.ResetInputs();
    }

    this.inputHandler = inputHandler;
    warnedMissingInputHandler = false;

    if (inputHandler != null)
    {
      inputHandler.AssignInputManager(this);
    }

    SyncCinemachineFollow();
  }

  private void InitializeInputHandler()
  {
    if (inputHandler == null)
    {
      inputHandler = FindDefaultInputHandler();
    }

    if (inputHandler != null)
    {
      inputHandler.AssignInputManager(this);
    }

    SyncCinemachineFollow();
  }

  private void SyncCinemachineFollow()
  {
    if (!updateCinemachineFollow || inputHandler == null)
    {
      return;
    }

    if (cinemachineCamera == null)
    {
      GameObject cameraObject = GameObject.FindWithTag("PlayerCinemachine");
      if (cameraObject != null)
      {
        cinemachineCamera = cameraObject.GetComponent<CinemachineVirtualCamera>();
      }
    }

    if (cinemachineCamera != null)
    {
      cinemachineCamera.Follow = inputHandler.transform;
    }
  }

  private InputHandler FindDefaultInputHandler()
  {
    InputHandler[] activeHandlers = FindObjectsOfType<InputHandler>();
    InputHandler fallbackHandler = null;

    for (int i = 0; i < activeHandlers.Length; i++)
    {
      InputHandler activeHandler = activeHandlers[i];
      if (activeHandler == null || !activeHandler.isActiveAndEnabled)
      {
        continue;
      }

      if (activeHandler is CharacterInputHandler)
      {
        return activeHandler;
      }

      if (fallbackHandler == null)
      {
        fallbackHandler = activeHandler;
      }
    }

    return fallbackHandler;
  }

  void Start()
  {
    InitializeInputHandler();
  }

  void Update()
  {
    if (inputHandler == null)
    {
      InitializeInputHandler();
      if (inputHandler != null)
      {
        warnedMissingInputHandler = false;
        inputHandler.SetInputs(GetInputData());
        return;
      }

      if (!warnedMissingInputHandler)
      {
        Debug.LogWarning($"{nameof(InputManager)} on {name} has no active input handler assigned.", this);
        warnedMissingInputHandler = true;
      }

      return;
    }

    inputHandler.SetInputs(GetInputData());
  }
}
