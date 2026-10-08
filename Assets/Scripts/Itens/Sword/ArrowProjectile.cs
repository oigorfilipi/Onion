using UnityEngine;

/// <summary>
/// Move a flecha 2D, aplica dano ao primeiro alvo e a destrói no impacto ou fim da vida útil.
/// </summary>
public class ArrowProjectile : MonoBehaviour
{
    private Rigidbody2D projectileBody;
    private int damageAmount;
    private float lifeTime = 4f;
    private bool hasHit;
    private PlayerVitals owner;

    // Recebe direção, velocidade, dano e dono; a flecha se autodestrói quando sua vida útil termina.
    public void Launch(Vector2 direction, float speed, int damage, float duration, PlayerVitals attacker)
    {
        owner = attacker;
        projectileBody = GetComponent<Rigidbody2D>();
        damageAmount = Mathf.Max(0, damage);
        lifeTime = Mathf.Max(0.1f, duration);

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.right;
        }

        projectileBody.gravityScale = 0f;
        projectileBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        projectileBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        projectileBody.linearVelocity = direction.normalized * speed;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || other.GetComponentInParent<PlayerController>() != null)
        {
            return;
        }

        hasHit = true;
        EnemyHealth enemyHealth = other.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageAmount, owner);
        }

        Destroy(gameObject);
    }
}
