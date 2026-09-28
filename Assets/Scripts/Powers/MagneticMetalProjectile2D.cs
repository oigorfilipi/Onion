using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class MagneticMetalProjectile2D : MonoBehaviour
{
    private Rigidbody2D body;
    private Vector2 direction;
    private float speed;
    private int damage;
    private float expiresAt;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 travelDirection, float travelSpeed, int hitDamage)
    {
        direction = travelDirection.normalized;
        speed = Mathf.Max(0f, travelSpeed);
        damage = Mathf.Max(0, hitDamage);
        expiresAt = Time.time + 4f;
    }

    private void FixedUpdate()
    {
        body.MovePosition(body.position + direction * speed * Time.fixedDeltaTime);
        if (Time.time >= expiresAt)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyHealth2D enemy = other.GetComponentInParent<EnemyHealth2D>();
        if (enemy != null)
        {
            enemy.ReceiveDamage(damage);
        }

        Destroy(gameObject);
    }
}
