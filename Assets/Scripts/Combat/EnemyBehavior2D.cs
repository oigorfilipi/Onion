using UnityEngine;

public enum EnemyCombatStyle2D
{
    Ranged,
    Melee,
    Hybrid
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth2D))]
public sealed class EnemyBehavior2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private EnemyCombatStyle2D combatStyle = EnemyCombatStyle2D.Hybrid;
    [SerializeField, Tooltip("Ativa a deteccao roteirizada: o inimigo inicia a perseguicao quando o jogador esta dentro do limite de perseguicao, mesmo fora do alcance normal de deteccao.")]
    private bool missionEnemy;

    [Header("Deteccao e limites")]
    [SerializeField, Min(0.1f)] private float detectionRange = 7f;
    [SerializeField, Min(0.1f)] private float maximumChaseDistance = 8f;
    [SerializeField, Min(0.1f)] private float homeLeashRadius = 9f;

    [Header("Movimento e patrulha")]
    [SerializeField, Min(0.1f)] private float movementSpeed = 2.2f;
    [SerializeField, Min(0.1f)] private float patrolSpeed = 0.7f;
    [SerializeField, Min(0.1f)] private float patrolRadius = 1.4f;
    [SerializeField, Min(0.05f)] private float waypointArrivalDistance = 0.25f;
    [SerializeField, Min(0f)] private float patrolPauseMinimum = 0.5f;
    [SerializeField, Min(0f)] private float patrolPauseMaximum = 1.5f;

    [Header("Combate")]
    [SerializeField, Min(0.1f)] private float preferredRange = 4.2f;
    [SerializeField, Min(0.1f)] private float meleeAttackRange = 1.3f;
    [SerializeField, Min(0.1f)] private float secondsBetweenMeleeAttacks = 1.4f;
    [SerializeField, Min(1)] private int meleeDamage = 10;
    [SerializeField, Range(0.05f, 0.9f)] private float retreatHealthRatio = 0.25f;

    private Rigidbody2D body;
    private EnemyHealth2D enemyHealth;
    private EnemyFireShooter2D shooter;
    private PlayerVitals targetVitals;
    private Vector2 homePosition;
    private Vector2 patrolWaypoint;
    private Vector2 desiredVelocity;
    private float patrolPauseRemaining;
    private float nextMeleeAttackAt;
    private bool hasPatrolWaypoint;
    private bool isAggro;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<EnemyHealth2D>();
        shooter = GetComponent<EnemyFireShooter2D>();
        homePosition = body.position;
        ConfigureBody();
        ResolveTarget();
    }

    private void Start()
    {
        ResolveTarget();
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            desiredVelocity = Vector2.zero;
            return;
        }

        if (target == null || enemyHealth == null || enemyHealth.CurrentHealth <= 0)
        {
            desiredVelocity = Vector2.zero;
            SetShooterActive(false);
            return;
        }

        float distanceFromHomeToPlayer = Vector2.Distance(homePosition, target.position);
        float distanceToPlayer = Vector2.Distance(body.position, target.position);

        if (isAggro && distanceFromHomeToPlayer > maximumChaseDistance)
        {
            isAggro = false;
            hasPatrolWaypoint = false;
        }
        else if (!isAggro && (missionEnemy || distanceToPlayer <= detectionRange) && distanceFromHomeToPlayer <= maximumChaseDistance)
        {
            isAggro = true;
            hasPatrolWaypoint = false;
        }

        if (!isAggro)
        {
            SetShooterActive(false);
            ReturnHomeOrPatrol();
            return;
        }

        bool lowHealth = (float)enemyHealth.CurrentHealth / enemyHealth.MaxHealth <= retreatHealthRatio;
        if (lowHealth)
        {
            if (distanceToPlayer < preferredRange + 1.5f)
            {
                SetShooterActive(false);
                MoveAwayFromPlayer();
                return;
            }

            SetShooterActive(combatStyle != EnemyCombatStyle2D.Melee);
            desiredVelocity = Vector2.zero;
            return;
        }

        switch (combatStyle)
        {
            case EnemyCombatStyle2D.Ranged:
                UpdateRangedCombat(distanceToPlayer);
                break;
            case EnemyCombatStyle2D.Melee:
                SetShooterActive(false);
                UpdateMeleeCombat(distanceToPlayer);
                break;
            default:
                UpdateHybridCombat(distanceToPlayer);
                break;
        }
    }

    private void FixedUpdate()
    {
        if (body == null || desiredVelocity.sqrMagnitude < 0.0001f || Time.timeScale == 0f)
        {
            return;
        }

        Vector2 nextPosition = body.position + desiredVelocity * Time.fixedDeltaTime;
        Vector2 fromHome = nextPosition - homePosition;
        if (fromHome.sqrMagnitude > homeLeashRadius * homeLeashRadius)
        {
            nextPosition = homePosition + fromHome.normalized * homeLeashRadius;
        }

        body.MovePosition(nextPosition);
    }

    public void ConfigureTarget(Transform newTarget)
    {
        target = newTarget;
        ResolveTarget();
    }

    public void ConfigureHomePosition(Vector2 position)
    {
        homePosition = position;
        hasPatrolWaypoint = false;
    }

    private void UpdateRangedCombat(float distanceToPlayer)
    {
        SetShooterActive(true);

        float keepAwayDistance = preferredRange * 0.75f;
        if (distanceToPlayer < keepAwayDistance)
        {
            MoveAwayFromPlayer();
        }
        else if (distanceToPlayer > preferredRange)
        {
            MoveTowardPlayer();
        }
        else
        {
            desiredVelocity = Vector2.zero;
        }
    }

    private void UpdateMeleeCombat(float distanceToPlayer)
    {
        if (distanceToPlayer > meleeAttackRange)
        {
            MoveTowardPlayer();
            return;
        }

        desiredVelocity = Vector2.zero;
        TryMeleeAttack();
    }

    private void UpdateHybridCombat(float distanceToPlayer)
    {
        if (distanceToPlayer <= meleeAttackRange)
        {
            SetShooterActive(false);
            desiredVelocity = Vector2.zero;
            TryMeleeAttack();
            return;
        }

        SetShooterActive(true);
        if (distanceToPlayer > preferredRange)
        {
            MoveTowardPlayer();
        }
        else
        {
            desiredVelocity = Vector2.zero;
        }
    }

    private void TryMeleeAttack()
    {
        if (targetVitals == null || Time.time < nextMeleeAttackAt)
        {
            return;
        }

        targetVitals.ReceiveDamage(meleeDamage);
        nextMeleeAttackAt = Time.time + secondsBetweenMeleeAttacks;
    }

    private void MoveTowardPlayer()
    {
        Vector2 direction = (Vector2)target.position - body.position;
        desiredVelocity = direction.sqrMagnitude > 0.0001f ? direction.normalized * movementSpeed : Vector2.zero;
    }

    private void MoveAwayFromPlayer()
    {
        Vector2 direction = body.position - (Vector2)target.position;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Random.insideUnitCircle;
        }

        desiredVelocity = direction.sqrMagnitude > 0.0001f ? direction.normalized * movementSpeed : Vector2.zero;
    }

    private void ReturnHomeOrPatrol()
    {
        Vector2 toHome = homePosition - body.position;
        if (toHome.sqrMagnitude > waypointArrivalDistance * waypointArrivalDistance)
        {
            hasPatrolWaypoint = false;
            desiredVelocity = toHome.normalized * movementSpeed;
            return;
        }

        if (patrolPauseRemaining > 0f)
        {
            patrolPauseRemaining -= Time.deltaTime;
            desiredVelocity = Vector2.zero;
            return;
        }

        if (!hasPatrolWaypoint)
        {
            Vector2 offset = Random.insideUnitCircle * patrolRadius;
            patrolWaypoint = homePosition + offset;
            hasPatrolWaypoint = true;
        }

        Vector2 toWaypoint = patrolWaypoint - body.position;
        if (toWaypoint.sqrMagnitude <= waypointArrivalDistance * waypointArrivalDistance)
        {
            hasPatrolWaypoint = false;
            float pauseMin = Mathf.Min(patrolPauseMinimum, patrolPauseMaximum);
            float pauseMax = Mathf.Max(patrolPauseMinimum, patrolPauseMaximum);
            patrolPauseRemaining = Random.Range(pauseMin, pauseMax);
            desiredVelocity = Vector2.zero;
            return;
        }

        desiredVelocity = toWaypoint.normalized * patrolSpeed;
    }

    private void SetShooterActive(bool active)
    {
        if (shooter == null)
        {
            return;
        }

        bool canShoot = active && combatStyle != EnemyCombatStyle2D.Melee;
        if (shooter.enabled != canShoot)
        {
            shooter.enabled = canShoot;
        }
    }

    private void ConfigureBody()
    {
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.useFullKinematicContacts = true;
    }

    private void ResolveTarget()
    {
        if (target == null)
        {
            GameObject playerObject = GameObject.Find("Player");
            if (playerObject != null)
            {
                target = playerObject.transform;
            }
        }

        targetVitals = target != null ? target.GetComponent<PlayerVitals>() : null;
    }
}
