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

    private EnemyPathfinding enemyPathfinding;
    private PlayerVitals playerVitals;
    private EnemyHealth enemyHealth;
    private Animator animator;
    private bool hasAttackTrigger;
    private bool chasing;
    private float nextRoamChangeTime;
    private float nextAttackTime;

    public void ConfigureRuntimeDamage(float multiplier)
    {
        attackDamage = Mathf.Max(1, Mathf.RoundToInt(attackDamage * multiplier));
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

        Vector2 toPlayer = playerVitals.transform.position - transform.position;
        float distance = toPlayer.magnitude;
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
            enemyPathfinding.MoveTo(toPlayer.normalized);
            return;
        }

        enemyPathfinding.MoveTo(Vector2.zero);
        if (Time.time >= nextAttackTime)
        {
            if (hasAttackTrigger)
            {
                animator.SetTrigger(attackTrigger);
            }

            playerVitals.TakeDamage(attackDamage);
            nextAttackTime = Time.time + attackCooldown;
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
