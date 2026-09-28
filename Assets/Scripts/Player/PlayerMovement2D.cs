using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerMovement2D : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField, Min(1f)] private float sprintSpeedMultiplier = 1.6f;
    [SerializeField, Min(0.1f)] private float dashSpeed = 10f;
    [SerializeField, Min(0.01f)] private float dashDuration = 0.16f;
    [SerializeField, Min(0f)] private float dashCooldown = 0.8f;

    private Rigidbody2D body;
    private PlayerInventory2D inventory;
    private PlayerInteractor2D interactor;
    private Vector2 moveInput;
    private Vector2 dashDirection;
    private float dashEndsAt;
    private float nextDashAvailableAt;
    private bool sprintRequested;

    public Vector2 FacingDirection { get; private set; } = Vector2.down;
    public bool IsSprinting => sprintRequested && moveInput.sqrMagnitude > 0.001f && !IsDashing;
    public bool IsDashing => Time.time < dashEndsAt;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        inventory = GetComponent<PlayerInventory2D>();
        interactor = GetComponent<PlayerInteractor2D>();
    }

    private void Update()
    {
        bool dialogueOpen = interactor != null && interactor.DialogueManager != null && interactor.DialogueManager.BlocksWorldInput;
        bool inventoryOpen = inventory != null && inventory.IsOpen;
        bool controlsBlocked = dialogueOpen || inventoryOpen;
        Keyboard keyboard = Keyboard.current;

        if (controlsBlocked)
        {
            moveInput = Vector2.zero;
            sprintRequested = false;
            dashEndsAt = 0f;
            return;
        }

        moveInput = ReadMovementInput();

        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        if (moveInput.sqrMagnitude > 0.001f)
        {
            FacingDirection = moveInput.normalized;
        }

        sprintRequested = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);

        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && Time.time >= nextDashAvailableAt)
        {
            Vector2 requestedDirection = moveInput.sqrMagnitude > 0.001f ? moveInput : FacingDirection;
            if (requestedDirection.sqrMagnitude > 0.001f)
            {
                dashDirection = requestedDirection.normalized;
                dashEndsAt = Time.time + dashDuration;
                nextDashAvailableAt = Time.time + dashCooldown;
                sprintRequested = false;
            }
        }
    }

    private void FixedUpdate()
    {
        float speed = IsDashing
            ? dashSpeed
            : IsSprinting ? moveSpeed * sprintSpeedMultiplier : moveSpeed;
        Vector2 direction = IsDashing ? dashDirection : moveInput;
        Vector2 nextPosition = body.position + direction * speed * Time.fixedDeltaTime;
        body.MovePosition(nextPosition);
    }

    private static Vector2 ReadMovementInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;

        return new Vector2(horizontal, vertical);
    }
}
