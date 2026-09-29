using System.Collections.Generic;
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
    [SerializeField, Min(0.1f)] private float projectileMaxDistance = 9f;
    [SerializeField, Min(0f)] private float aimLeadTime = 0.12f;
    [SerializeField] private EnemyDashPunch2D dashPunch;

    private float nextShotTime;
    private bool attackWindowOpen = true;
    private Rigidbody2D targetBody;
    private readonly HashSet<FireProjectile2D> activeProjectiles = new HashSet<FireProjectile2D>();

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

        if (target != null)
        {
            targetBody = target.GetComponent<Rigidbody2D>();
        }

        nextShotTime = Time.time + 1.5f;
    }

    private void Update()
    {
        if (Time.timeScale == 0f || !attackWindowOpen || target == null || projectileSprite == null || Time.time < nextShotTime ||
            (dashPunch != null && dashPunch.IsAttacking))
        {
            return;
        }

        Vector2 offset = (Vector2)target.position - (Vector2)transform.position;
        if (offset.sqrMagnitude > attackRange * attackRange || offset.sqrMagnitude < 1f)
        {
            return;
        }

        Vector2 aimOffset = offset;
        if (targetBody != null && aimLeadTime > 0f)
        {
            aimOffset += targetBody.linearVelocity * aimLeadTime;
        }

        Shoot(aimOffset.sqrMagnitude > 0.01f ? aimOffset.normalized : offset.normalized);
        nextShotTime = Time.time + secondsBetweenShots;
    }

    private void OnDisable()
    {
        foreach (FireProjectile2D projectile in activeProjectiles)
        {
            if (projectile != null)
            {
                Destroy(projectile.gameObject);
            }
        }

        activeProjectiles.Clear();
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
        fireball.Finished += RemoveProjectile;
        activeProjectiles.Add(fireball);
        fireball.Initialize(direction, projectileSpeed, projectileDamage, projectileMaxDistance);
    }

    public void ConfigureDashPunch(EnemyDashPunch2D punch)
    {
        dashPunch = punch;
    }

    public void SetAttackWindowOpen(bool open)
    {
        attackWindowOpen = open;
    }

    private void RemoveProjectile(FireProjectile2D projectile)
    {
        activeProjectiles.Remove(projectile);
    }
}
