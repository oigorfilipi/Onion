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
    [SerializeField, Min(0f)] private float knockBackThrust = 4f;
    [SerializeField, Min(0)] private int experienceReward;

    private Knockback knockback;
    private Flash flash;
    private bool isDying;
    private string slimeKind = "Fire";
    public bool IsBoss { get; private set; }
    private string worldSaveKey;
    public bool IsRuntimeSpawn { get; private set; }
    public string WorldSaveKey => string.IsNullOrEmpty(worldSaveKey)
        ? worldSaveKey = WorldProgressKey.For(this) : worldSaveKey;

    public event Action HealthChanged;
    public int MaxHealth => startingHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;
    public string SlimeKind => slimeKind;

    public void ConfigureRuntimeHealth(float multiplier)
    {
        IsRuntimeSpawn = true;
        startingHealth = Mathf.Max(1, Mathf.RoundToInt(startingHealth * multiplier));
        CurrentHealth = startingHealth;
        HealthChanged?.Invoke();
    }

    public void ConfigureAsBoss(float multiplier = 3f)
    {
        IsBoss = true;
        ConfigureRuntimeHealth(multiplier);
    }

    private void Awake()
    {
        worldSaveKey = WorldProgressKey.For(this);
        string lowerName = gameObject.name.ToLowerInvariant();
        slimeKind = lowerName.Contains("ghost") || lowerName.Contains("fantasma") ? "Ghost" : "Fire";
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
        WorldDamageNumbers.Show(transform.position + Vector3.up * 0.75f, damage,
            IsBoss ? new Color(1f, 0.55f, 0.2f) : Color.white);
        GameAudio.PlayHit();
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
            GameSession.RecordEnemyKill(IsBoss ? "Boss" : slimeKind);
            GameSession.AddCoins(5);
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
