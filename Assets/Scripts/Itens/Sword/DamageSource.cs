using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Entrega o dano da área de colisão da espada ao EnemyHealth atingido.
/// </summary>
public class DamageSource : MonoBehaviour
{
    [SerializeField] private int damageAmount = 1;

    public void SetDamageAmount(int damage)
    {
        damageAmount = Mathf.Max(0, damage);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        EnemyHealth enemyHealth = other.GetComponentInParent<EnemyHealth>();
        enemyHealth?.TakeDamage(damageAmount, GetComponentInParent<PlayerVitals>());
    }
}
