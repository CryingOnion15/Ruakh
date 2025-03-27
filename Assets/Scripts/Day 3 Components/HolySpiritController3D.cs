using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class HolySpiritController3D : MonoBehaviour
{
    [SerializeField]
    protected float moveSpeed = 50;

    [SerializeField]
    protected float turnSpeed = 10;

    [SerializeField]
    protected Transform gravityCenter = null;

    [SerializeField]
    protected float gravitySpeed = 9.8f;

    [SerializeField]
    protected Transform cameraTransform = null;

    protected Rigidbody rb;
    protected Vector3 forceDirection = Vector3.zero;
    protected Vector3 gravityDirection = Vector3.zero;
    protected float turnValue = 0;

    public InputAction movementAction;
    public InputAction turnAction;

    //public InputActionMap actionMap;

    void Awake()
    {
        movementAction.performed += OnMove;
        movementAction.canceled += OnMoveCanceled;
        turnAction.performed += OnTurn;
        turnAction.canceled += OnTurnCanceled;
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        movementAction.Enable();
        turnAction.Enable();
    }

    void OnDisable()
    {
        movementAction.Disable();
        turnAction.Disable();
    }

    void FixedUpdate()
    {
        // // Add Gravity
        UpdateGravityDirection();
        UpdateTurn();

        Vector3 direction = transform.TransformDirection(forceDirection);
        rb.MovePosition(rb.position + (direction * Time.fixedDeltaTime * moveSpeed));
    }

    protected void UpdateGravityDirection()
    {
        if (gravityCenter)
        {
            gravityDirection = (gravityCenter.position - transform.position).normalized;
            Vector3 localUp = transform.up;

            rb.AddForce(gravityDirection * gravitySpeed);

            Quaternion targetRotation =
                Quaternion.FromToRotation(localUp, -gravityDirection) * transform.rotation;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                50 * Time.fixedDeltaTime
            );
        }
    }

    protected void UpdateTurn()
    {
        Vector3 localUp = transform.up;

        Quaternion targetRotation =
            Quaternion.AngleAxis(turnSpeed * turnValue, localUp) * transform.rotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            50 * Time.fixedDeltaTime
        );
    }

    public void OnMove(InputAction.CallbackContext input)
    {
        Vector2 inputDirection = input.ReadValue<Vector2>();
        forceDirection.x = inputDirection.x;
        forceDirection.z = inputDirection.y;
    }

    public void OnMoveCanceled(InputAction.CallbackContext context)
    {
        forceDirection.x = 0;
        forceDirection.y = 0;
        rb.velocity = Vector3.zero;
    }

    public void OnTurn(InputAction.CallbackContext input)
    {
        Vector2 inputDirection = input.ReadValue<Vector2>();
        turnValue = inputDirection.x;
    }

    public void OnTurnCanceled(InputAction.CallbackContext context)
    {
        turnValue = 0;
    }
}
