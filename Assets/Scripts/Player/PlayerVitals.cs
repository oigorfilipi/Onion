using System;
using UnityEngine;

public sealed class PlayerVitals : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0)] private int currentHealth = 100;

    [Header("Energia")]
    [SerializeField, Min(1)] private int maxEnergy = 100;
    [SerializeField, Min(0)] private int currentEnergy = 100;

    [Header("Defesa")]
    [SerializeField, Range(0f, 1f)] private float blockDamageMultiplier = 0.5f;

    [Header("Regeneracao passiva (segundos por ponto)")]
    [SerializeField, Min(0.01f)] private float secondsPerRegenerationPoint = 12f;

    private float healthRegenerationTimer;
    private float energyRegenerationTimer;
    private PlayerEquipment2D equipment;

    public bool IsBlocking { get; private set; }
    public float BlockDamageMultiplier => blockDamageMultiplier;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int CurrentEnergy => currentEnergy;
    public int MaxEnergy => maxEnergy;

    public event Action<int, int> HealthChanged;
    public event Action<int, int> EnergyChanged;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        maxEnergy = Mathf.Max(1, maxEnergy);
        blockDamageMultiplier = Mathf.Clamp01(blockDamageMultiplier);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        equipment = GetComponent<PlayerEquipment2D>();
    }

    private void Update()
    {
        RegenerateHealthOverTime();
        RegenerateEnergyOverTime();
    }

    public bool TrySpendEnergy(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (currentEnergy < amount)
        {
            return false;
        }

        SetEnergy(currentEnergy - amount);
        return true;
    }

    public void RestoreEnergy(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetEnergy(currentEnergy + amount);
    }

    public void ReceiveDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int damageAfterArmor = equipment != null ? equipment.ReduceIncomingDamage(amount) : amount;
        if (IsBlocking)
        {
            damageAfterArmor = Mathf.CeilToInt(damageAfterArmor * blockDamageMultiplier);
        }

        SetHealth(currentHealth - damageAfterArmor);
    }

    public void SetBlocking(bool blocking)
    {
        IsBlocking = blocking;
    }

    public void RestoreHealth(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetHealth(currentHealth + amount);
    }

    private void RegenerateHealthOverTime()
    {
        if (currentHealth >= maxHealth)
        {
            healthRegenerationTimer = 0f;
            return;
        }

        healthRegenerationTimer += Time.deltaTime;
        int pointsToRestore = Mathf.FloorToInt(healthRegenerationTimer / secondsPerRegenerationPoint);
        if (pointsToRestore <= 0)
        {
            return;
        }

        healthRegenerationTimer -= pointsToRestore * secondsPerRegenerationPoint;
        SetHealth(currentHealth + pointsToRestore);
    }

    private void RegenerateEnergyOverTime()
    {
        if (currentEnergy >= maxEnergy)
        {
            energyRegenerationTimer = 0f;
            return;
        }

        energyRegenerationTimer += Time.deltaTime;
        int pointsToRestore = Mathf.FloorToInt(energyRegenerationTimer / secondsPerRegenerationPoint);
        if (pointsToRestore <= 0)
        {
            return;
        }

        energyRegenerationTimer -= pointsToRestore * secondsPerRegenerationPoint;
        SetEnergy(currentEnergy + pointsToRestore);
    }

    private void SetHealth(int value)
    {
        int newValue = Mathf.Clamp(value, 0, maxHealth);
        if (newValue == currentHealth)
        {
            return;
        }

        currentHealth = newValue;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void SetEnergy(int value)
    {
        int newValue = Mathf.Clamp(value, 0, maxEnergy);
        if (newValue == currentEnergy)
        {
            return;
        }

        currentEnergy = newValue;
        EnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }
}
