using UnityEngine;

public class CharacterMotor : MonoBehaviour, IMoveInputReceiver
{
  [SerializeField] private Rigidbody2D rigidBody;
  [SerializeField] private float speed = 5f;

  private Vector2 movement;
  private int lastFacing;

  public Vector2 Movement => movement;
  public int LastFacing => lastFacing;
  public float Speed => speed;

  public void Initialize(Rigidbody2D fallbackRigidbody)
  {
    if (rigidBody == null)
    {
      rigidBody = fallbackRigidbody != null ? fallbackRigidbody : GetComponent<Rigidbody2D>();
    }
  }

  public void MoveToward(Vector2 direction)
  {
    movement = direction;
    if (movement.x != 0f)
    {
      lastFacing = movement.x > 0f ? 1 : 0;
    }
  }

  public void SetKinematic(bool isKinematic)
  {
    if (rigidBody == null)
    {
      return;
    }

    rigidBody.isKinematic = isKinematic;
    if (isKinematic)
    {
      Stop();
    }
  }

  public void Stop()
  {
    movement = Vector2.zero;
    if (rigidBody != null)
    {
      rigidBody.linearVelocity = Vector2.zero;
    }
  }

  public void FixedTick()
  {
    if (rigidBody == null)
    {
      return;
    }

    rigidBody.linearVelocity = movement * speed;
  }

  private void Reset()
  {
    rigidBody = GetComponent<Rigidbody2D>();
  }

  private void Start()
  {
    Initialize(null);
  }

  private void FixedUpdate()
  {
    Initialize(null);
    FixedTick();
  }

  private void OnValidate()
  {
    if (rigidBody == null)
    {
      rigidBody = GetComponent<Rigidbody2D>();
    }
  }
}
