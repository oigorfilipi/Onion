using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PoisonProjectile2D : MonoBehaviour
{
    private Rigidbody2D body;
    private Vector2 direction;
    private float speed;
    private int directDamage;
    private int poisonDamagePerTick;
    private int poisonTicks;
    private float expiresAt;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 travelDirection, float travelSpeed, int hitDamage, int damagePerTick, int ticks)
    {
        direction = travelDirection.normalized;
        speed = Mathf.Max(0f, travelSpeed);
        directDamage = Mathf.Max(0, hitDamage);
        poisonDamagePerTick = Mathf.Max(0, damagePerTick);
        poisonTicks = Mathf.Max(0, ticks);
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
            enemy.ReceiveDamage(directDamage);
            enemy.ApplyPoison(poisonDamagePerTick, poisonTicks, 1f);
        }

        Destroy(gameObject);
    }
}
