using UnityEngine;

public struct CarInputState
{
  public float BrakeInput;
  public float HorizontalInput;
  public float VerticalInput;
  public float Interact;

  public Vector2 DirectionInput => new Vector2(HorizontalInput, VerticalInput);
  public bool HasDirectionInput => HorizontalInput != 0f || VerticalInput != 0f;
  public bool HasInteractInput => Interact != 0f;
  public bool IsBraking => BrakeInput != 0f;
}
