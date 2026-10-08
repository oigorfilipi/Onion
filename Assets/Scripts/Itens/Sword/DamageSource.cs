using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Entrega o dano da área de colisão da espada ao EnemyHealth atingido.
/// </summary>
public class DamageSource : MonoBehaviour
{
    [SerializeField] private int damageAmount = 1;
    private readonly HashSet<EnemyHealth> hitThisSwing = new HashSet<EnemyHealth>();
    private bool swingActive;

    public void BeginSwing()
    {
        hitThisSwing.Clear();
        swingActive = true;
    }

    public void EndSwing() => swingActive = false;

    public void SetDamageAmount(int damage)
    {
        damageAmount = Mathf.Max(0, damage);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryHit(other);

    // Um novo golpe também acerta um slime que continuou dentro do collider da espada.
    private void OnTriggerStay2D(Collider2D other) => TryHit(other);

    private void TryHit(Collider2D other)
    {
        if (!swingActive || damageAmount <= 0) return;
        EnemyHealth enemyHealth = other.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null || enemyHealth.IsDead || !hitThisSwing.Add(enemyHealth)) return;
        enemyHealth.TakeDamage(damageAmount, GetComponentInParent<PlayerVitals>());
    }
}
