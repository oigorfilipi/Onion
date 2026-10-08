using UnityEngine;

/// <summary>
/// Escolhe patrulha, perseguição e ataque dos slimes conforme a distância do jogador.
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float roamChangeDirFloat = 2f;
    [SerializeField, Min(0.1f)] private float detectionRange = 4f;
    [SerializeField, Min(0.1f)] private float loseTargetRange = 6f;
    [SerializeField, Min(0.1f)] private float attackRange = 1.4f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 1.2f;
    [SerializeField, Min(1)] private int attackDamage = 8;
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private bool canShootProjectiles;
    [SerializeField, Range(0f, 1f)] private float rangedAttackChance = 0.27f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 5f;
    [SerializeField, Min(0f)] private float projectileSpreadDegrees = 18f;

    private EnemyPathfinding enemyPathfinding;
    private PlayerVitals playerVitals;
    private EnemyHealth enemyHealth;
    private Animator animator;
    private bool hasAttackTrigger;
    private bool chasing;
    private float nextRoamChangeTime;
    private float nextAttackTime;
    private int scaledForLevel = 1;
    private bool isBoss;
    private float nextTeleportTime;
    private float nextMinionTime;
    private bool nextBossProjectileFire = true;

    public bool IsBoss => isBoss;

    public void ConfigureRangedAttacks(bool enabled)
    {
        canShootProjectiles = enabled;
    }

    public void ConfigureRuntimeDamage(float multiplier)
    {
        attackDamage = Mathf.Max(1, Mathf.RoundToInt(attackDamage * multiplier));
    }

    public void ConfigureAsBoss(float damageMultiplier = 3f)
    {
        isBoss = true;
        attackDamage = Mathf.Max(1, Mathf.RoundToInt(attackDamage * damageMultiplier));
        attackCooldown *= 0.85f;
        detectionRange *= 1.8f;
        loseTargetRange *= 1.8f;
        enemyPathfinding?.ConfigureRuntimeSpeed(0.65f);
        nextTeleportTime = Time.time + 15f;
        nextMinionTime = Time.time + 8f;
    }

    private void Awake()
    {
        enemyPathfinding = GetComponent<EnemyPathfinding>();
        enemyHealth = GetComponent<EnemyHealth>();
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == attackTrigger && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    hasAttackTrigger = true;
                    break;
                }
            }
        }
    }

    // Muda entre patrulha, perseguição e ataque conforme visão, distância e recarga do slime.
    private void Update()
    {
        if (Time.timeScale <= 0f || enemyPathfinding == null ||
            (enemyHealth != null && enemyHealth.IsDead))
        {
            return;
        }

        if (playerVitals == null)
        {
            playerVitals = FindAnyObjectByType<PlayerVitals>();
        }

        if (playerVitals == null || playerVitals.IsDead)
        {
            chasing = false;
            Roam();
            return;
        }

        ApplyLevelScaling();

        Vector2 toPlayer = playerVitals.transform.position - transform.position;
        float distance = toPlayer.magnitude;
        if (isBoss) UpdateBossSupport(distance);
        if (!chasing && distance <= detectionRange)
        {
            chasing = true;
        }
        else if (chasing && distance > Mathf.Max(detectionRange, loseTargetRange))
        {
            chasing = false;
            nextRoamChangeTime = 0f;
        }

        if (!chasing)
        {
            Roam();
            return;
        }

        if (distance > attackRange)
        {
            bool withinRangedRange = distance <= (isBoss ? detectionRange : 5f);
            if (withinRangedRange && Time.time >= nextAttackTime &&
                (isBoss || (canShootProjectiles && Random.value < rangedAttackChance)))
            {
                enemyPathfinding.MoveTo(isBoss ? Vector2.zero : toPlayer.normalized * 0.4f);
                ShootStatusProjectile(toPlayer.normalized);
                nextAttackTime = Time.time + attackCooldown;
            }
            else enemyPathfinding.MoveTo(toPlayer.normalized);
            return;
        }

        enemyPathfinding.MoveTo(Vector2.zero);
        if (Time.time >= nextAttackTime)
        {
            if (isBoss)
            {
                ShootStatusProjectile(toPlayer.normalized);
                nextAttackTime = Time.time + attackCooldown;
                return;
            }

            if (hasAttackTrigger)
            {
                animator.SetTrigger(attackTrigger);
            }

            playerVitals.TakeDamage(attackDamage);
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void ApplyLevelScaling()
    {
        int level = playerVitals != null ? playerVitals.Level : 1;
        int tier = Mathf.Max(0, level / 10);
        if (tier == scaledForLevel - 1) return;
        int gainedTiers = tier - (scaledForLevel - 1);
        if (gainedTiers <= 0) return;
        attackDamage = Mathf.Max(1, Mathf.RoundToInt(attackDamage * Mathf.Pow(1.2f, gainedTiers)));
        enemyPathfinding?.ConfigureRuntimeSpeed(Mathf.Pow(1.15f, gainedTiers));
        detectionRange *= Mathf.Pow(1.2f, gainedTiers);
        loseTargetRange *= Mathf.Pow(1.2f, gainedTiers);
        projectileSpreadDegrees = Mathf.Max(0f, projectileSpreadDegrees - gainedTiers * 5f);
        scaledForLevel = tier + 1;
    }

    private void ShootStatusProjectile(Vector2 direction)
    {
        bool fire;
        if (isBoss)
        {
            fire = nextBossProjectileFire;
            nextBossProjectileFire = !nextBossProjectileFire;
        }
        else fire = enemyHealth != null && enemyHealth.SlimeKind == "Fire";

        float spread = Random.Range(-projectileSpreadDegrees, projectileSpreadDegrees);
        Vector2 accurateDirection = Quaternion.Euler(0f, 0f, spread) * direction;
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        EnemyStatusProjectile.Spawn(transform.position, accurateDirection, projectileSpeed,
            isBoss ? attackDamage : Mathf.Max(1, attackDamage / 2),
            fire ? EnemyStatusProjectile.Status.Fire : EnemyStatusProjectile.Status.Poison,
            spriteRenderer != null ? spriteRenderer.sprite : null,
            isBoss ? 0.3f : 0.2f);
        GameAudio.PlaySlimeShot(fire);
    }

    private void UpdateBossSupport(float distance)
    {
        if (Time.time >= nextTeleportTime && distance > 8f)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(2.5f, 4f);
            transform.position = playerVitals.transform.position + (Vector3)offset;
            nextTeleportTime = Time.time + 15f;
            ShootStatusProjectile(((Vector2)playerVitals.transform.position - (Vector2)transform.position).normalized);
            GameAudio.PlayTeleport();
        }

        if (Time.time >= nextMinionTime)
        {
            SurvivalSpawner spawner = FindAnyObjectByType<SurvivalSpawner>();
            if (spawner != null) spawner.SpawnBossMinion(transform.position);
            nextMinionTime = Time.time + 8f;
        }
    }

    private void OnDisable()
    {
        if (enemyPathfinding != null)
        {
            enemyPathfinding.MoveTo(Vector2.zero);
        }
    }

    private void Roam()
    {
        if (Time.time < nextRoamChangeTime)
        {
            return;
        }

        Vector2 direction = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        enemyPathfinding.MoveTo(direction.normalized);
        nextRoamChangeTime = Time.time + roamChangeDirFloat;
    }
}
