using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lê o movimento e o dash do Input System, move o Rigidbody2D e orienta o sprite para o mouse.
/// </summary>
public class PlayerController : MonoBehaviour
{
    public bool FacingLeft { get { return facingLeft; } }
    public static PlayerController Instance;

    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float dashSpeed = 4f;
    [SerializeField, Min(0)] private int dashStaminaCost = 10;
    [SerializeField] private TrailRenderer myTrailRenderer;

    private PlayerControls playerControls;
    private Vector2 movement;
    private Rigidbody2D rb;
    private Animator myAnimator;
    private SpriteRenderer mySpriteRender;
    private PlayerVitals playerVitals;
    private float speedBoostUntil;
    private float temporarySpeedMultiplier = 1f;

    private bool facingLeft = false;
    private bool isDashing = false;

    private void Awake() {
        Instance = this;
        playerControls = new PlayerControls();
        rb = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        mySpriteRender = GetComponent<SpriteRenderer>();
        playerVitals = GetComponent<PlayerVitals>();
    }

    private void Start() {
        playerControls.Combat.Dash.performed += _ => Dash();

    }

    private void OnEnable() {
        playerControls.Enable();
    }

    private void OnDisable() {
        movement = Vector2.zero;
        playerControls?.Disable();
        if (myAnimator != null)
        {
            myAnimator.SetFloat("moveX", 0f);
            myAnimator.SetFloat("moveY", 0f);
        }
    }

    private void Update() {
        PlayerInput();
        if (temporarySpeedMultiplier > 1f && Time.time >= speedBoostUntil)
            temporarySpeedMultiplier = 1f;
    }

    private void FixedUpdate() {
        AdjustPlayerFacingDirection();
        Move();
    }

    private void PlayerInput() {
        movement = playerControls.Movement.Move.ReadValue<Vector2>();

        myAnimator.SetFloat("moveX", movement.x);
        myAnimator.SetFloat("moveY", movement.y);
    }

    private void Move() {
        float dashMultiplier = isDashing ? dashSpeed : 1f;
        rb.MovePosition(rb.position + movement * (moveSpeed * temporarySpeedMultiplier * dashMultiplier * Time.fixedDeltaTime));
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (multiplier <= 1f || duration <= 0f) return;
        temporarySpeedMultiplier = Mathf.Max(temporarySpeedMultiplier, multiplier);
        speedBoostUntil = Mathf.Max(speedBoostUntil, Time.time + duration);
    }

    private void AdjustPlayerFacingDirection() {
        Vector3 mousePos = Input.mousePosition;
        Vector3 playerScreenPoint = Camera.main.WorldToScreenPoint(transform.position);

        if (mousePos.x < playerScreenPoint.x) {
            mySpriteRender.flipX = true;
            facingLeft = true;
        } else {
            mySpriteRender.flipX = false;
            facingLeft = false;
        }
    }

    // Acelera o movimento por 0,2 s, consome estamina e controla a recarga do dash.
    private void Dash() {
        if (Time.timeScale > 0f && !isDashing &&
            (playerVitals == null || playerVitals.ConsumeStamina(dashStaminaCost))) {
            isDashing = true;
            GameAudio.PlayDash();
            if (myTrailRenderer != null) myTrailRenderer.emitting = true;
            StartCoroutine(EndDashRoutine());
        }
    }

    private IEnumerator EndDashRoutine() {
        float dashTime = .2f;
        float dashCD = .25f;
        yield return new WaitForSeconds(dashTime);
        isDashing = false;
        if (myTrailRenderer != null) myTrailRenderer.emitting = false;
        yield return new WaitForSeconds(dashCD);
    }
}
