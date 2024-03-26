using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : Controller
{
  [SerializeField]
  private Rigidbody2D rigidBody;

  private float speed = 9f;
  private int lastFacing = 1;
  private Vector2 movement;
  public GunController gunController { get; private set; }
  public ItemsController itemsController { get; private set; }

  public void SwitchToKinematic(bool isTrue)
  {
    rigidBody.isKinematic = isTrue;
  }

  public void EnableVisibility()
  {
    SetVisibility(true);
  }

  public void DisableVisibility()
  {
    SetVisibility(false);
  }

  public void MoveToward(Vector2 m)
  {
    movement = m;
    if (movement.x != 0)
    {
      lastFacing = movement.x > 0 ? 1 : 0;
    }
  }

  public void InteractWith()
  {
    Interactable? currentInteractable = GetCurrentInteractable();
    if (currentInteractable is Interactable interactable)
    {
      currentInteractable.Interact(gameObject);
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

  private void Animate()
  {
    Animator animator = Refs.Animator;
    animator.SetFloat("LastFacing", lastFacing);
    animator.SetFloat("Horizontal", movement.x);
    animator.SetFloat("Vertical", movement.y);
    animator.SetFloat("Magnitude", movement.magnitude);
  }

  private void Move()
  {
    rigidBody.velocity = new Vector2(movement.x, movement.y) * speed;
  }

  void Start()
  {
    gunController = new GunController(this);
    itemsController = new ItemsController(this);
  }

  // Update is called once per frame
  void Update()
  {
    Animate();

    gunController.Update();
    itemsController.Update();
  }

  void FixedUpdate()
  {
    Move();

    gunController.FixedUpdate();
  }
}
