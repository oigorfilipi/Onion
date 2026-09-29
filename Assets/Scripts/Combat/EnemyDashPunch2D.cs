using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class EnemyDashPunch2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0.1f)] private float attackRange = 4.8f;
    [SerializeField, Min(0.1f)] private float minimumAttackDistance = 0.65f;
    [SerializeField, Min(0.1f)] private float secondsBetweenAttacks = 3.2f;
    [SerializeField, Min(0f)] private float windupDuration = 0.35f;
    [SerializeField, Min(0.1f)] private float dashSpeed = 10f;
    [SerializeField, Min(0.1f)] private float dashDistance = 4.5f;
    [SerializeField, Min(0f)] private float hitRadius = 0.75f;
    [SerializeField, Min(1)] private int punchDamage = 18;

    private Rigidbody2D body;
    private PlayerVitals targetVitals;
    private bool windingUp;
    private bool dashing;
    private Vector2 dashDirection;
    private float dashStartsAt;
    private float dashEndsAt;
    private float nextAttackAt;
    private bool attackWindowOpen = true;
    private float distanceDashed;
    private Vector3 originalScale;

    public bool IsAttacking => windingUp || dashing;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;
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
            return;
        }

        if (windingUp)
        {
            if (Time.time >= dashStartsAt)
            {
                BeginDash();
            }

            return;
        }

        if (dashing || !attackWindowOpen || target == null || targetVitals == null || Time.time < nextAttackAt)
        {
            return;
        }

        Vector2 offset = (Vector2)target.position - body.position;
        float distanceSquared = offset.sqrMagnitude;
        if (distanceSquared > attackRange * attackRange || distanceSquared < minimumAttackDistance * minimumAttackDistance)
        {
            return;
        }

        dashDirection = offset.normalized;
        windingUp = true;
        dashStartsAt = Time.time + windupDuration;
        nextAttackAt = Time.time + secondsBetweenAttacks;
        transform.localScale = originalScale * 1.2f;
    }

    private void FixedUpdate()
    {
        if (!dashing)
        {
            return;
        }

        if (target == null || targetVitals == null)
        {
            FinishDash();
            return;
        }

        if (Time.time >= dashEndsAt || distanceDashed >= dashDistance)
        {
            FinishDash();
            return;
        }

        Vector2 start = body.position;
        float step = Mathf.Min(dashSpeed * Time.fixedDeltaTime, dashDistance - distanceDashed);
        Vector2 end = start + dashDirection * step;
        Vector2 targetPosition = target != null ? (Vector2)target.position : end;

        if (DistanceFromPointToSegment(targetPosition, start, end) <= hitRadius)
        {
            float advance = Mathf.Clamp(Vector2.Dot(targetPosition - start, dashDirection), 0f, step);
            body.MovePosition(start + dashDirection * advance);
            targetVitals.ReceiveDamage(punchDamage);
            FinishDash();
            return;
        }

        body.MovePosition(end);
        distanceDashed += step;
    }

    private void OnDisable()
    {
        windingUp = false;
        dashing = false;
        if (originalScale != Vector3.zero)
        {
            transform.localScale = originalScale;
        }
    }

    public void ConfigureTarget(Transform newTarget)
    {
        target = newTarget;
        ResolveTarget();
    }

    public void SetAttackWindowOpen(bool open)
    {
        attackWindowOpen = open;
    }

    private void BeginDash()
    {
        windingUp = false;
        dashing = true;
        transform.localScale = originalScale;
        distanceDashed = 0f;
        dashEndsAt = Time.time + dashDistance / dashSpeed;
    }

    private void FinishDash()
    {
        dashing = false;
        windingUp = false;
        transform.localScale = originalScale;
    }

    private void ConfigureBody()
    {
        if (body == null)
        {
            return;
        }

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
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        targetVitals = target != null ? target.GetComponent<PlayerVitals>() : null;
    }

    private static float DistanceFromPointToSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
    {
        Vector2 segment = segmentEnd - segmentStart;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0.0001f)
        {
            return Vector2.Distance(point, segmentStart);
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - segmentStart, segment) / lengthSquared);
        return Vector2.Distance(point, segmentStart + segment * t);
    }
}
