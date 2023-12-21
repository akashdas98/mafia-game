using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicMovement : MonoBehaviour
{
    public float speed = 9f;
    public int lastFacing = 1;
    public Animator animator;
    public Rigidbody2D rigidBody;

    private Vector3 movement;
    private float horizontalInput, verticalInput;

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
        rigidBody.velocity = new Vector3(movement.x, movement.y) * speed;
    }

    // Update is called once per frame
    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        movement = new Vector3(horizontalInput, verticalInput);
        Move();
        Animate();
    }
}
