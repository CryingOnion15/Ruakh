using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;

[RequireComponent(typeof(Rigidbody))]
public class HolySpiritController2D : MonoBehaviour
{
    [SerializeField]
    protected float moveSpeed = 50;

    [SerializeField]
    protected float maxSpeed = 15;

    [SerializeField]
    protected float minSpeed = 1;

    [SerializeField]
    protected float slowdownRate = .5f;

    protected Rigidbody rb;
    protected Vector3 forceDirection = Vector3.zero;

    public InputAction movementAction;

    //public InputActionMap actionMap;

    void Awake()
    {
        movementAction.performed += OnMove;
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        movementAction.Enable();
    }

    void OnDisable()
    {
        movementAction.Disable();
    }

    void FixedUpdate()
    {
        // Add force
        if (forceDirection != Vector3.zero)
        {
            rb.AddForce(forceDirection * moveSpeed * Time.fixedDeltaTime, ForceMode.Impulse);

            if (rb.velocity.magnitude > maxSpeed)
            {
                rb.velocity = rb.velocity.normalized * maxSpeed;
            }
        }
        // Slowdown to stop.
        else
        {
            if (rb.velocity.magnitude > 0)
            {
                rb.velocity = rb.velocity * (1 - (slowdownRate * Time.fixedDeltaTime));
            }

            if (rb.velocity.magnitude < minSpeed)
            {
                rb.velocity = Vector3.zero;
            }
        }
    }

    public void OnMove(InputAction.CallbackContext input)
    {
        Vector2 inputDirection = input.ReadValue<Vector2>();
        forceDirection.x = inputDirection.x;
        forceDirection.z = inputDirection.y;
    }
}
