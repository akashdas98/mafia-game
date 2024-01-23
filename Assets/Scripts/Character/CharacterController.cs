using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : Controller
{
  [SerializeField]
  private Rigidbody2D rigidBody;

  [SerializeField]
  private Inventory inventory;

  private Shooting shooting;
  private float speed = 9f;
  private int lastFacing = 1;
  private Vector3 movement;

  //inputs
  private float
    horizontalInput,
    verticalInput,
    aimHorizontalInput,
    aimVerticalInput,
    actionInput,
    interact;

  private Controller currentInteractable = null;

  private void SetMovement()
  {
    movement = Utilities.Get8DirectionFromInput(horizontalInput, verticalInput);
  }

  public bool HasActionInput()
  {
    return actionInput > 0 ? true : false;
  }

  public override void SetInputs(InputData input)
  {
    horizontalInput = input.HorizontalAxis;
    verticalInput = input.VerticalAxis;
    aimHorizontalInput = input.AimHorizontal;
    aimVerticalInput = input.AimVertical;
    actionInput = input.Action;
    interact = input.Interact;
  }

  public Dictionary<string, float> GetInputs()
  {
    return new Dictionary<string, float> {
      {"horizontalInput", horizontalInput},
      {"verticalInput", verticalInput},
      {"aimHorizontalInput", aimHorizontalInput},
      {"aimVerticalInput", aimVerticalInput},
      {"actionInput", actionInput},
      {"interact", interact}
    };
  }

  public override void ResetInputs()
  {
    horizontalInput = 0;
    verticalInput = 0;
    aimHorizontalInput = 0;
    aimVerticalInput = 0;
    actionInput = 0;
    interact = 0;
  }

  public override void Interact(GameObject other, ControlManager cm) { }

  public void SwitchToKinematic(bool isTrue)
  {
    rigidBody.isKinematic = isTrue;
  }

  private void SetVisibility(bool isVisible)
  {
    // Assuming all SpriteRenderers are children of this GameObject
    foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
    {
      renderer.enabled = isVisible;
    }
  }

  public void EnableVisibility()
  {
    SetVisibility(true);
  }

  public void DisableVisibility()
  {
    SetVisibility(false);
  }

  private void Animate()
  {
    if (Input.GetButtonDown("Horizontal"))
    {
      lastFacing = horizontalInput > 0 ? 1 : 0;
    }

    animator.SetFloat("LastFacing", lastFacing);
    animator.SetFloat("Horizontal", movement.x);
    animator.SetFloat("Vertical", movement.y);
    animator.SetFloat("AimHorizontal", shooting.GetAimDirection().x);
    animator.SetFloat("AimVertical", shooting.GetAimDirection().y);
    animator.SetFloat("Magnitude", movement.magnitude);
  }

  private void Move()
  {
    rigidBody.velocity = new Vector3(movement.x, movement.y) * speed;
  }

  private void InteractWith()
  {
    if (currentInteractable != null && interact != 0)
    {
      currentInteractable.Interact(gameObject, controlManager);
    }
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    Controller interactable = other.GetComponent<Controller>();
    currentInteractable = interactable;
  }

  private void OnTriggerExit2D(Collider2D other)
  {
    currentInteractable = null;
  }

  void Start()
  {
    shooting = new Shooting(this, inventory);
  }

  // Update is called once per frame
  void Update()
  {
    Animate();
    InteractWith();
    SetMovement();
    shooting.Update();
  }

  void FixedUpdate()
  {
    Move();
  }
}
