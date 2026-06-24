using UnityEngine;

public struct CharacterInputState
{
  public float HorizontalInput;
  public float VerticalInput;
  public float ActionInput;
  public float Interact;
  public float Drop;
  public float Scroll;
  public float Aim;
  public float MouseX;
  public float MouseY;

  public Vector2 MovementInput => Utilities.GetDirectionFromInput(HorizontalInput, VerticalInput);
  public Vector2 MouseScreenPosition => new Vector2(MouseX, MouseY);
  public bool HasActionInput => ActionInput > 0f;
  public bool HasInteractInput => Interact != 0f;
  public bool HasDropInput => Drop != 0f;
  public bool HasScrollInput => Scroll != 0f;
  public bool HasAimInput => Aim != 0f;
}
