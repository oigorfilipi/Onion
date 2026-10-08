using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Controla vida, flash, recuo, morte e experiência concedida por cada slime.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int startingHealth = 3;
    [SerializeField] private GameObject deathVFXPrefab;
    [SerializeField] private float knockBackThrust = 15f;
    [SerializeField, Min(0)] private int experienceReward;

    private Knockback knockback;
    private Flash flash;
    private bool isDying;
    private string worldSaveKey;
    public bool IsRuntimeSpawn { get; private set; }
    public string WorldSaveKey => string.IsNullOrEmpty(worldSaveKey)
        ? worldSaveKey = WorldProgressKey.For(this) : worldSaveKey;

    public event Action HealthChanged;
    public int MaxHealth => startingHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    public void ConfigureRuntimeHealth(float multiplier)
    {
        IsRuntimeSpawn = true;
        startingHealth = Mathf.Max(1, Mathf.RoundToInt(startingHealth * multiplier));
        CurrentHealth = startingHealth;
        HealthChanged?.Invoke();
    }

    private void Awake()
    {
        worldSaveKey = WorldProgressKey.For(this);
        CurrentHealth = startingHealth;
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();

        if (GetComponent<EnemyHealthBar>() == null)
        {
            gameObject.AddComponent<EnemyHealthBar>();
        }
    }

    // Atualiza barra e efeitos; ao morrer, registra inimigo fixo e concede XP apenas ao atacante informado.
    public void TakeDamage(int damage, PlayerVitals attacker = null)
    {
        if (damage <= 0 || isDying)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        HealthChanged?.Invoke();

        if (knockback != null && PlayerController.Instance != null)
        {
            knockback.GetKnockedBack(PlayerController.Instance.transform, knockBackThrust);
        }

        if (flash != null)
        {
            StartCoroutine(flash.FlashRoutine());
        }

        if (IsDead)
        {
            isDying = true;
            FindAnyObjectByType<SaveController>()?.RegisterDefeatedEnemy(this);
            attacker?.GainExperience(experienceReward);
            EnemyAI enemyAI = GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.enabled = false;
            }

            StartCoroutine(DieAfterFlash());
        }
    }

    private IEnumerator DieAfterFlash()
    {
        if (flash != null)
        {
            yield return new WaitForSeconds(flash.GetRestoreMatTime());
        }

        DetectDeath();
    }

    public void DetectDeath()
    {
        if (!IsDead)
        {
            return;
        }

        if (deathVFXPrefab != null)
        {
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
