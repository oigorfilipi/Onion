using UnityEngine;

/// <summary>Projétil visual leve que aplica dano contínuo de fogo ou veneno ao jogador.</summary>
public class EnemyStatusProjectile : MonoBehaviour
{
    public enum Status { Fire, Poison }
    private Status status;
    private int contactDamage;
    private Vector2 direction;
    private float speed;
    private float expiresAt;
    private bool consumed;

    public static void Spawn(Vector3 position, Vector2 direction, float speed, int damage,
        Status status, Sprite sourceSprite, float scale)
    {
        GameObject projectile = new GameObject(status + " Slime Projectile");
        projectile.transform.position = position;
        projectile.transform.localScale = Vector3.one * scale;
        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = sourceSprite;
        renderer.color = status == Status.Fire ? new Color(1f, 0.32f, 0.12f) : new Color(0.35f, 1f, 0.38f);
        renderer.sortingLayerID = SortingLayer.NameToID("Player");
        renderer.sortingOrder = 2;
        CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        Rigidbody2D body = projectile.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        EnemyStatusProjectile script = projectile.AddComponent<EnemyStatusProjectile>();
        script.Initialize(direction, speed, damage, status);
    }

    private void Initialize(Vector2 moveDirection, float moveSpeed, int damage, Status kind)
    {
        direction = moveDirection.normalized;
        speed = moveSpeed;
        contactDamage = Mathf.Max(1, damage);
        status = kind;
        expiresAt = Time.time + 6f;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        if (Time.time >= expiresAt) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;
        PlayerVitals player = other.GetComponentInParent<PlayerVitals>();
        if (player == null) return;
        consumed = true;
        player.TakeDamage(contactDamage);
        if (!player.ShieldProtectionActive)
            player.ApplyStatusDamage(status == Status.Fire, status == Status.Fire ? 2 : 1,
                status == Status.Fire ? 5f : 10f,
                status == Status.Fire ? new Color(1f, 0.15f, 0.08f) : new Color(0.15f, 1f, 0.2f));
        Destroy(gameObject);
    }
}
