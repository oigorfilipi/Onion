using UnityEngine;

public sealed class EnemyFireShooter2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Sprite projectileSprite;
    [SerializeField] private string projectileName = "Bola de fogo";
    [SerializeField] private Color projectileColor = new Color(1f, 0.35f, 0.08f);
    [SerializeField, Min(1f)] private float attackRange = 7f;
    [SerializeField, Min(0.1f)] private float secondsBetweenShots = 2.5f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 4f;
    [SerializeField, Min(0)] private int projectileDamage = 8;

    private float nextShotTime;

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        nextShotTime = Time.time + 1.5f;
    }

    private void Update()
    {
        if (target == null || projectileSprite == null || Time.time < nextShotTime)
        {
            return;
        }

        Vector2 offset = (Vector2)target.position - (Vector2)transform.position;
        if (offset.sqrMagnitude > attackRange * attackRange || offset.sqrMagnitude < 1f)
        {
            return;
        }

        Shoot(offset.normalized);
        nextShotTime = Time.time + secondsBetweenShots;
    }

    private void Shoot(Vector2 direction)
    {
        GameObject projectile = new GameObject(projectileName);
        projectile.transform.position = transform.position + (Vector3)(direction * 0.8f);
        projectile.transform.localScale = Vector3.one * 0.32f;

        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = projectileSprite;
        renderer.color = projectileColor;
        renderer.sortingOrder = 20;

        Rigidbody2D body = projectile.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;

        FireProjectile2D fireball = projectile.AddComponent<FireProjectile2D>();
        fireball.Initialize(direction, projectileSpeed, projectileDamage);
    }
}
