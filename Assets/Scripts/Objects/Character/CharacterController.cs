using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : Controller
{
  public Inventory inventory;
  public GunController gunController;
  public ItemsController itemsController;

  [SerializeField]
  private Rigidbody2D rigidBody;
  private float speed = 9f;
  private int lastFacing = 1;
  private Vector2 movement;

  [SerializeField]
  private GameObject aimTarget;

  //inputs
  private float
    horizontalInput,
    verticalInput,
    actionInput,
    interact,
    drop,
    scroll,
    aim,
    mouseX,
    mouseY;

  public override void SetInputs(InputData input)
  {
    horizontalInput = input.HorizontalAxis;
    verticalInput = input.VerticalAxis;
    actionInput = input.Action;
    interact = input.Interact;
    drop = input.Drop;
    scroll = input.Scroll;
    aim = input.Aim;
    mouseX = input.MouseX;
    mouseY = input.MouseY;
  }

  public override Dictionary<string, float> GetInputs()
  {
    return new Dictionary<string, float> {
      {"horizontalInput", horizontalInput},
      {"verticalInput", verticalInput},
      {"actionInput", actionInput},
      {"interact", interact},
      {"drop", drop},
      {"scroll", scroll},
      {"aim", aim},
      {"mouseX", mouseX},
      {"mouseY", mouseY}
    };
  }

  public override void ResetInputs()
  {
    horizontalInput = 0;
    verticalInput = 0;
    actionInput = 0;
    interact = 0;
    drop = 0;
    scroll = 0;
    aim = 0;
  }

  public override void Interact(GameObject other, ControlManager cm) { }

  public GameObject GetAimTarget()
  {
    return aimTarget;
  }

  public void SwitchToKinematic(bool isTrue)
  {
    rigidBody.isKinematic = isTrue;
  }

  private void SetMovement()
  {
    movement = Utilities.GetDirectionFromInput(horizontalInput, verticalInput);
  }

  private void InteractWith()
  {
    if (currentInteractable != null && interact != 0)
    {
      if (currentInteractable is Controller controller)
      {
        controller.Interact(gameObject, controlManager);
      }
      else
      {
        currentInteractable.Interact(gameObject);
      }
    }
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
    animator.SetFloat("Magnitude", movement.magnitude);
  }

  private void Move()
  {
    rigidBody.velocity = new Vector2(movement.x, movement.y) * speed;
  }

  private void HandleMiscInputs()
  {
    InteractWith();
    SetMovement();
  }

  void Start()
  {
    gunController = new GunController(this, inventory, aimTarget.GetComponent<Target>());
    itemsController = new ItemsController(this, inventory);
  }

  // Update is called once per frame
  void Update()
  {
    Animate();
    HandleMiscInputs();

    gunController.Update();
    itemsController.Update();
  }

  void FixedUpdate()
  {
    Move();

    gunController.FixedUpdate();
  }
}
