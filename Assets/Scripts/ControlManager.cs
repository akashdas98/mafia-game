using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlManager : MonoBehaviour
{
  public Controller playerController;
  private List<Controller> controllers = new List<Controller>();
  private Dictionary<Controller, bool> controllerStates = new Dictionary<Controller, bool>();

  private void EnableController(Controller controller)
  {
    if (controllerStates.ContainsKey(controller))
    {
      controllerStates[controller] = true;
    }
    else
    {
      // Add the controller with a true state if it's not in the dictionary
      controllerStates.Add(controller, true);
    }
  }

  private void DisableController(Controller controller)
  {
    if (controllerStates.ContainsKey(controller))
    {
      controllerStates[controller] = false;
    }
    else
    {
      controllerStates.Add(controller, false);
    }
    controller.ResetInputs();
  }

  private void SetEnabledControllerInputs(InputData inputData)
  {
    foreach (var controllerEntry in controllerStates)
    {
      if (controllerEntry.Value)
      {
        controllerEntry.Key.SetInputs(inputData);
      }
    }
  }

  private InputData GetInputData()
  {
    // Gather current input data
    return new InputData
    {
      HorizontalAxis = Input.GetAxis("Horizontal"),
      VerticalAxis = Input.GetAxis("Vertical"),
      AimHorizontal = Input.GetAxis("AimHorizontal"),
      AimVertical = Input.GetAxis("AimVertical"),
      Brake = Input.GetAxis("Brake"),
      Action = Input.GetAxis("Action"),
      Interact = Input.GetButtonDown("Interact") ? 1 : 0
    };
  }

  public void AddController(Controller controller)
  {
    // If not already added
    if (!controllers.Contains(controller))
    {
      // Set the delegates
      controller.SetEnableDisableActions(
        () => EnableController(controller),
        () => DisableController(controller)
      );

      // Add the controller to the list
      controllers.Add(controller);
    }
  }

  private void InitializePlayerController()
  {
    playerController.AssignControlManager(this);
    playerController.SetEnableDisableActions(
      () => EnableController(playerController),
      () => DisableController(playerController)
    );
    EnableController(playerController);
  }

  void Start()
  {
    InitializePlayerController();
  }

  void Update()
  {
    SetEnabledControllerInputs(GetInputData());
  }
}
