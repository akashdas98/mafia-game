using UnityEngine;
using System.Collections.Generic;

public class VehiclePossession : MonoBehaviour
{
  [SerializeField] private InputHandler vehicleInputHandler;
  private GameObject driver;
  private readonly Dictionary<Collider2D, bool> driverColliderStates = new Dictionary<Collider2D, bool>();

  public GameObject Driver => driver;

  public void Initialize()
  {
    if (vehicleInputHandler == null)
    {
      vehicleInputHandler = GetComponent<InputHandler>();
    }
  }

  public void Enter(GameObject player)
  {
    Initialize();

    CharacterMotor playerMotor = player != null ? player.GetComponent<CharacterMotor>() : null;
    if (playerMotor == null)
    {
      return;
    }

    InputHandler playerInputHandler = player.GetComponent<InputHandler>();
    InputManager inputManager = playerInputHandler != null ? playerInputHandler.InputManager : player.GetComponentInParent<InputManager>();
    if (inputManager == null || vehicleInputHandler == null)
    {
      return;
    }

    driver = player;
    player.transform.parent = transform;
    SetVisibility(player, false);
    SetCollisionEnabled(player, false);
    playerMotor.SetKinematic(true);
    inputManager.SetInputHandler(vehicleInputHandler);
  }

  public void Exit()
  {
    if (driver == null)
    {
      return;
    }

    GameObject currentDriver = driver;
    CharacterMotor driverMotor = currentDriver.GetComponent<CharacterMotor>();
    driver = null;

    currentDriver.transform.parent = null;
    currentDriver.transform.eulerAngles = new Vector3(currentDriver.transform.eulerAngles.x, currentDriver.transform.eulerAngles.y, 0);
    SetCollisionEnabled(currentDriver, true);
    SetVisibility(currentDriver, true);
    if (driverMotor != null)
    {
      driverMotor.SetKinematic(false);
    }

    InputHandler driverInputHandler = currentDriver.GetComponent<InputHandler>();
    InputManager inputManager = driverInputHandler != null ? driverInputHandler.InputManager : currentDriver.GetComponentInParent<InputManager>();
    if (inputManager != null && driverInputHandler != null)
    {
      inputManager.SetInputHandler(driverInputHandler);
    }
  }

  private void SetVisibility(GameObject target, bool isVisible)
  {
    if (target == null)
    {
      return;
    }

    SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
    for (int i = 0; i < renderers.Length; i++)
    {
      renderers[i].enabled = isVisible;
    }
  }

  private void SetCollisionEnabled(GameObject target, bool isEnabled)
  {
    if (target == null)
    {
      return;
    }

    Collider2D[] colliders = target.GetComponentsInChildren<Collider2D>(true);
    if (!isEnabled)
    {
      driverColliderStates.Clear();
      for (int i = 0; i < colliders.Length; i++)
      {
        Collider2D characterCollider = colliders[i];
        if (characterCollider == null)
        {
          continue;
        }

        driverColliderStates[characterCollider] = characterCollider.enabled;
        characterCollider.enabled = false;
      }

      return;
    }

    for (int i = 0; i < colliders.Length; i++)
    {
      Collider2D characterCollider = colliders[i];
      if (characterCollider == null)
      {
        continue;
      }

      if (driverColliderStates.TryGetValue(characterCollider, out bool wasEnabled))
      {
        characterCollider.enabled = wasEnabled;
      }
    }

    driverColliderStates.Clear();
  }

  private void Reset()
  {
    vehicleInputHandler = GetComponent<InputHandler>();
  }

  private void OnValidate()
  {
    if (vehicleInputHandler == null)
    {
      vehicleInputHandler = GetComponent<InputHandler>();
    }
  }
}
