using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class FireProjectile2D : MonoBehaviour
{
    private Rigidbody2D body;
    private Vector2 direction;
    private float speed;
    private int damage;
    private float maxTravelDistance;
    private float distanceTravelled;
    private float expiresAt;

    public event Action<FireProjectile2D> Finished;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 travelDirection, float travelSpeed, int hitDamage, float travelDistance = 9f)
    {
        direction = travelDirection.normalized;
        speed = Mathf.Max(0f, travelSpeed);
        damage = Mathf.Max(0, hitDamage);
        maxTravelDistance = Mathf.Max(0.1f, travelDistance);
        expiresAt = Time.time + Mathf.Max(5f, maxTravelDistance / Mathf.Max(0.1f, speed) + 1f);
    }

    private void FixedUpdate()
    {
        float stepDistance = speed * Time.fixedDeltaTime;
        float remainingDistance = maxTravelDistance - distanceTravelled;
        stepDistance = Mathf.Min(stepDistance, remainingDistance);
        body.MovePosition(body.position + direction * stepDistance);
        distanceTravelled += stepDistance;
        if (distanceTravelled >= maxTravelDistance || Time.time >= expiresAt)
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
            playerVitals.ApplyBurn();
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        Finished?.Invoke(this);
    }
}
