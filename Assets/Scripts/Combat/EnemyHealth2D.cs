using UnityEngine;
using System;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class EnemyHealth2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 60;
    [SerializeField, Min(0)] private int currentHealth = 60;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float restoreColorAt;
    private int poisonDamagePerTick;
    private int poisonTicksRemaining;
    private float poisonInterval;
    private float nextPoisonTickAt;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public event Action<EnemyHealth2D> Died;
    public event Action<EnemyHealth2D, int, int> HealthChanged;

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
    }

    public void ReceiveDamage(int amount)
    {
        ApplyDamage(amount);
    }

    public void SetDamageLocked(bool locked)
    {
        damageLocked = locked;
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

        currentHealth = Mathf.Max(0, currentHealth - amount);
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
        Camera sceneCamera = Camera.main;
        if (sceneCamera == null || currentHealth <= 0)
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
}
