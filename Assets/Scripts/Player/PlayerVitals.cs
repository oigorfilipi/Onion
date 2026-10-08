using System;
using UnityEngine;

/// <summary>
/// Concentra vida, defesa, estamina, experiência e nível do jogador; notifica HUD, save e tela de derrota.
/// </summary>
public class PlayerVitals : MonoBehaviour
{
    [SerializeField] private string playerName = "Jogador";
    [SerializeField, Min(1)] private int baseHealth = 100;
    [SerializeField, Min(0)] private int baseDefense = 5;
    [SerializeField, Min(1)] private int baseStamina = 100;
    [SerializeField, Min(0f)] private float damageInterval = 0.5f;
    [SerializeField, Min(1)] private int level = 1;
    [SerializeField, Min(0)] private int experience;

    private EquipmentController equipmentController;
    private Flash flash;
    private PlayerController playerController;
    private ActiveWeapon activeWeapon;
    private bool controlsDisabledForDeath;
    private float nextDamageTime;

    public event Action StatsChanged;
    public event Action Died;

    public string PlayerName => playerName;
    public int BaseHealth => baseHealth;
    public int BaseDefense => baseDefense + SwordDamageBonus;
    public int BaseStamina => baseStamina;
    public int Level => level;
    public int Experience => experience;
    public int ExperienceToNextLevel => 100 + (level - 1) * 50;
    public int SwordDamageBonus => level - 1;
    public int BonusHealth { get; private set; }
    public int BonusDefense { get; private set; }
    public int BonusStamina { get; private set; }
    public int MaxHealth => baseHealth + BonusHealth;
    public int Defense => BaseDefense + BonusDefense;
    public int MaxStamina => baseStamina + BonusStamina;
    public int CurrentHealth { get; private set; }
    public int CurrentStamina { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    private void Awake()
    {
        equipmentController = GetComponent<EquipmentController>();
        flash = GetComponent<Flash>();
        playerController = GetComponent<PlayerController>();
        activeWeapon = GetComponentInChildren<ActiveWeapon>(true);
        CurrentHealth = MaxHealth;
        CurrentStamina = MaxStamina;
    }

    private void OnEnable()
    {
        if (equipmentController != null)
        {
            equipmentController.EquipmentChanged += RefreshBonuses;
        }
    }

    private void Start()
    {
        RefreshBonuses();
    }

    private void OnDisable()
    {
        if (equipmentController != null)
        {
            equipmentController.EquipmentChanged -= RefreshBonuses;
        }
    }

    // Subtrai a defesa do dano bruto, garante pelo menos 1 de dano e respeita a breve proteção entre acertos.
    public int TakeDamage(int rawDamage)
    {
        if (rawDamage <= 0 || IsDead || Time.time < nextDamageTime)
        {
            return 0;
        }

        int actualDamage = Mathf.Max(1, rawDamage - Defense);
        CurrentHealth = Mathf.Max(0, CurrentHealth - actualDamage);
        nextDamageTime = Time.time + damageInterval;

        if (flash != null)
        {
            StartCoroutine(flash.FlashRoutine());
        }

        StatsChanged?.Invoke();
        if (IsDead)
        {
            UpdateAliveState();
            Died?.Invoke();
        }

        return actualDamage;
    }

    public bool Heal(int amount)
    {
        if (amount <= 0 || IsDead || CurrentHealth >= MaxHealth)
        {
            return false;
        }

        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        StatsChanged?.Invoke();
        return true;
    }

    // Mantém XP excedente ao subir de nível; cada nível aumenta dano de espada e defesa base por propriedades calculadas.
    public void GainExperience(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        experience += amount;
        while (experience >= ExperienceToNextLevel)
        {
            experience -= ExperienceToNextLevel;
            level++;
        }

        StatsChanged?.Invoke();
    }

    public void SetPlayerName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }

        playerName = newName.Trim();
        StatsChanged?.Invoke();
    }

    public void ResetForNewGame(string newName)
    {
        SetPlayerName(newName);
        level = 1;
        experience = 0;
        RefreshBonuses();
        CurrentHealth = MaxHealth;
        CurrentStamina = MaxStamina;
        nextDamageTime = 0f;
        UpdateAliveState();
        StatsChanged?.Invoke();
    }

    public void RestoreSavedVitals(int health, int stamina, int savedLevel = 1, int savedExperience = 0)
    {
        level = Mathf.Max(1, savedLevel);
        experience = Mathf.Clamp(savedExperience, 0, ExperienceToNextLevel - 1);
        RefreshBonuses();
        CurrentHealth = Mathf.Clamp(health, 0, MaxHealth);
        CurrentStamina = Mathf.Clamp(stamina, 0, MaxStamina);
        UpdateAliveState();
        StatsChanged?.Invoke();
    }

    private void UpdateAliveState()
    {
        if (IsDead && !controlsDisabledForDeath)
        {
            controlsDisabledForDeath = true;
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            if (activeWeapon != null)
            {
                activeWeapon.enabled = false;
            }
        }
        else if (!IsDead && controlsDisabledForDeath)
        {
            controlsDisabledForDeath = false;
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            if (activeWeapon != null)
            {
                activeWeapon.enabled = true;
            }
        }
    }

    // Recalcula bônus a partir dos itens equipados e avisa HUD e save quando o resultado muda.
    private void RefreshBonuses()
    {
        int oldMaxHealth = MaxHealth;
        int oldMaxStamina = MaxStamina;
        BonusHealth = 0;
        BonusDefense = 0;
        BonusStamina = 0;

        if (equipmentController != null && equipmentController.EquipmentSlots != null)
        {
            foreach (EquipmentSlot slot in equipmentController.EquipmentSlots)
            {
                Item item = slot != null && slot.CurrentItem != null
                    ? slot.CurrentItem.GetComponent<Item>()
                    : null;
                if (item == null)
                {
                    continue;
                }

                BonusHealth += Mathf.Max(0, item.healthBonus);
                BonusDefense += Mathf.Max(0, item.defenseBonus);
                BonusStamina += Mathf.Max(0, item.staminaBonus);
            }
        }

        if (MaxHealth > oldMaxHealth)
        {
            CurrentHealth += MaxHealth - oldMaxHealth;
        }

        if (MaxStamina > oldMaxStamina)
        {
            CurrentStamina += MaxStamina - oldMaxStamina;
        }

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
        CurrentStamina = Mathf.Clamp(CurrentStamina, 0, MaxStamina);
        StatsChanged?.Invoke();
    }
}
