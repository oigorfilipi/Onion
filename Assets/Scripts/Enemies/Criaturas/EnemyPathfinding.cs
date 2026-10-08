using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Move o Rigidbody2D do slime na direção recebida; não calcula caminhos ao redor de obstáculos.
/// </summary>
public class EnemyPathfinding : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;

    private Rigidbody2D rb;
    private Vector2 moveDir;
    private Knockback knockback;

    public void ConfigureRuntimeSpeed(float multiplier)
    {
        moveSpeed = Mathf.Max(0.1f, moveSpeed * multiplier);
    }

    private void Awake() {
        knockback = GetComponent<Knockback>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate() {
        if (rb == null || (knockback != null && knockback.GettingKnockedBack)) { return; }

        rb.MovePosition(rb.position + moveDir * (moveSpeed * Time.fixedDeltaTime));
    }

    public void MoveTo(Vector2 targetPosition) {
        moveDir = Vector2.ClampMagnitude(targetPosition, 1f);
    }
}
