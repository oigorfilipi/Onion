using UnityEngine;
using System;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class EnemyHealth2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 60;
    [SerializeField, Min(0)] private int currentHealth = 60;
    [SerializeField] private bool isBoss;
    [SerializeField] private string bossDisplayName = "Chefe";
    [SerializeField, Min(0f)] private float fullRegenerationDelay = 10f;
    [SerializeField, Min(0f)] private float bossRegenerationDelayAfterDamage = 6f;
    [SerializeField, Min(0.1f)] private float bossRegenerationInterval = 20f;
    [SerializeField, Min(1)] private int bossRegenerationAmount = 1;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float restoreColorAt;
    private int poisonDamagePerTick;
    private int poisonTicksRemaining;
    private float poisonInterval;
    private float nextPoisonTickAt;
    private int damageFloor;
    private float lastDamageAt;
    private float nextBossRegenerationAt;
    private bool hasTakenDamage;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsBoss => isBoss;
    public event Action<EnemyHealth2D> Died;
    public event Action<EnemyHealth2D, int, int> HealthChanged;
    public string BossDisplayName => bossDisplayName;

    private bool damageLocked;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 1, maxHealth);
    }

    private void Update()
    {
        if (restoreColorAt > 0f && Time.time >= restoreColorAt)
        {
            spriteRenderer.color = originalColor;
            restoreColorAt = 0f;
        }

        if (poisonTicksRemaining > 0 && Time.time >= nextPoisonTickAt)
        {
            poisonTicksRemaining--;
            nextPoisonTickAt = Time.time + poisonInterval;
            ApplyDamage(poisonDamagePerTick);
        }

        if (!isBoss && hasTakenDamage && currentHealth < maxHealth && Time.time >= lastDamageAt + fullRegenerationDelay)
        {
            currentHealth = maxHealth;
            hasTakenDamage = false;
            HealthChanged?.Invoke(this, currentHealth, maxHealth);
        }

        if (isBoss && !damageLocked && hasTakenDamage && currentHealth < maxHealth &&
            Time.time >= lastDamageAt + bossRegenerationDelayAfterDamage && Time.time >= nextBossRegenerationAt)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + bossRegenerationAmount);
            nextBossRegenerationAt = Time.time + bossRegenerationInterval;
            HealthChanged?.Invoke(this, currentHealth, maxHealth);
            if (currentHealth >= maxHealth)
            {
                hasTakenDamage = false;
            }
        }
    }

    public void ReceiveDamage(int amount)
    {
        ApplyDamage(amount);
    }

    public void SetDamageLocked(bool locked)
    {
        damageLocked = locked;
    }

    public void SetDamageFloor(int healthFloor)
    {
        damageFloor = Mathf.Clamp(healthFloor, 0, maxHealth);
    }

    public void ConfigureBoss(int health, string displayName)
    {
        maxHealth = Mathf.Max(1, health);
        currentHealth = maxHealth;
        damageFloor = 0;
        damageLocked = false;
        isBoss = true;
        bossDisplayName = string.IsNullOrWhiteSpace(displayName) ? "Chefe" : displayName;
        hasTakenDamage = false;
        nextBossRegenerationAt = 0f;
    }

    public void ConfigureRegularEnemy(int health)
    {
        maxHealth = Mathf.Max(1, health);
        currentHealth = maxHealth;
        isBoss = false;
        bossDisplayName = string.Empty;
        damageFloor = 0;
        damageLocked = false;
        hasTakenDamage = false;
        nextBossRegenerationAt = 0f;
    }

    public void DefeatImmediately()
    {
        if (currentHealth <= 0)
        {
            return;
        }

        currentHealth = 0;
        HealthChanged?.Invoke(this, currentHealth, maxHealth);
        Died?.Invoke(this);
        Destroy(gameObject);
    }

    public void ApplyPoison(int damagePerTick, int ticks, float interval)
    {
        if (currentHealth <= 0 || damagePerTick <= 0 || ticks <= 0 || interval <= 0f)
        {
            return;
        }

        poisonDamagePerTick = damagePerTick;
        poisonTicksRemaining = Mathf.Max(poisonTicksRemaining, ticks);
        poisonInterval = interval;
        nextPoisonTickAt = Time.time + interval;
    }

    private void ApplyDamage(int amount)
    {
        if (amount <= 0 || currentHealth <= 0 || damageLocked)
        {
            return;
        }

        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(damageFloor, currentHealth - amount);
        if (currentHealth < previousHealth)
        {
            lastDamageAt = Time.time;
            hasTakenDamage = true;
            if (isBoss)
            {
                nextBossRegenerationAt = Time.time + bossRegenerationDelayAfterDamage;
            }
        }
        HealthChanged?.Invoke(this, currentHealth, maxHealth);
        if (currentHealth == 0 && damageLocked)
        {
            currentHealth = 1;
            HealthChanged?.Invoke(this, currentHealth, maxHealth);
        }

        spriteRenderer.color = Color.white;
        restoreColorAt = Time.time + 0.1f;

        if (currentHealth == 0)
        {
            Died?.Invoke(this);
            Destroy(gameObject);
        }
    }

    private void OnGUI()
    {
        if (currentHealth <= 0)
        {
            return;
        }

        if (isBoss)
        {
            DrawBossHealthBar();
            return;
        }

        Camera sceneCamera = Camera.main;
        if (sceneCamera == null)
        {
            return;
        }

        Vector3 screenPosition = sceneCamera.WorldToScreenPoint(transform.position + Vector3.up * 0.8f);
        if (screenPosition.z < 0f)
        {
            return;
        }

        float x = screenPosition.x - 40f;
        float y = Screen.height - screenPosition.y;
        GUI.Label(new Rect(x, y - 19f, 80f, 18f), $"{currentHealth}/{maxHealth}");

        Rect bar = new Rect(x, y, 80f, 8f);
        GUI.color = Color.black;
        GUI.DrawTexture(bar, Texture2D.whiteTexture);
        GUI.color = new Color(0.85f, 0.16f, 0.12f);
        bar.width *= (float)currentHealth / maxHealth;
        GUI.DrawTexture(bar, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawBossHealthBar()
    {
        float width = Mathf.Min(520f, Screen.width - 40f);
        float left = (Screen.width - width) * 0.5f;
        const float top = 18f;

        GUIStyle nameStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        GUI.Label(new Rect(left, top, width, 26f), $"{bossDisplayName}  {currentHealth}/{maxHealth}", nameStyle);

        Rect bar = new Rect(left, top + 28f, width, 16f);
        GUI.color = Color.black;
        GUI.DrawTexture(bar, Texture2D.whiteTexture);
        GUI.color = new Color(0.78f, 0.12f, 0.12f);
        bar.width *= (float)currentHealth / maxHealth;
        GUI.DrawTexture(bar, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
