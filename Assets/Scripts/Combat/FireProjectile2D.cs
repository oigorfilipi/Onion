using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class FireProjectile2D : MonoBehaviour
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
        expiresAt = Time.time + 5f;
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
        PlayerVitals playerVitals = other.GetComponentInParent<PlayerVitals>();
        if (playerVitals != null)
        {
            playerVitals.ReceiveDamage(damage);
        }

        Destroy(gameObject);
    }
}
