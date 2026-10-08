using System;
using System.Collections;
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
    [SerializeField, Min(0f)] private float regenerationDelay = 3f;
    [SerializeField, Min(0f)] private float healthRegenerationPerSecond = 1f;
    [SerializeField, Min(0f)] private float staminaRegenerationPerSecond = 5f;
    [SerializeField, Min(1)] private int armorBonusEveryLevels = 5;
    [SerializeField, Min(1)] private int level = 1;
    [SerializeField, Min(0)] private int experience;

    private EquipmentController equipmentController;
    private Flash flash;
    private PlayerController playerController;
    private ActiveWeapon activeWeapon;
    private bool controlsDisabledForDeath;
    private float nextDamageTime;
    private float lastDamageTime;
    private float healthRegenerationRemainder;
    private float staminaRegenerationRemainder;
    private bool shieldProtectionActive;
    private bool shieldWasEquipped;
    private Coroutine shieldRoutine;
    private Coroutine fireStatusRoutine;
    private Coroutine poisonStatusRoutine;
    private float fireImmunityUntil;
    private float poisonImmunityUntil;
    private Color normalSpriteColor;

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
    public int MaxHealth => baseHealth + BonusHealth + Mathf.Max(0, level - 1) * 25;
    public int Defense => BaseDefense + BonusDefense;
    public int MaxStamina => baseStamina + BonusStamina + Mathf.Max(0, level - 1) * 25;
    public int CurrentHealth { get; private set; }
    public int CurrentStamina { get; private set; }
    public bool IsDead => CurrentHealth <= 0;
    public bool ShieldProtectionActive => shieldProtectionActive;
    public float RemainingFireImmunity => Mathf.Max(0f, fireImmunityUntil - Time.time);
    public float RemainingPoisonImmunity => Mathf.Max(0f, poisonImmunityUntil - Time.time);
    public bool FireImmune => RemainingFireImmunity > 0f;
    public bool PoisonImmune => RemainingPoisonImmunity > 0f;

    private void Awake()
    {
        equipmentController = GetComponent<EquipmentController>();
        flash = GetComponent<Flash>();
        playerController = GetComponent<PlayerController>();
        activeWeapon = GetComponentInChildren<ActiveWeapon>(true);
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        normalSpriteColor = sprite != null ? sprite.color : Color.white;
        CurrentHealth = MaxHealth;
        CurrentStamina = MaxStamina;
        lastDamageTime = Time.time;
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

    private void Update()
    {
        RegenerateVitals();
        UpdateShieldState();
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
        if (rawDamage <= 0 || IsDead || shieldProtectionActive || Time.time < nextDamageTime)
        {
            return 0;
        }

        int actualDamage = Mathf.Max(1, rawDamage - Defense);
        CurrentHealth = Mathf.Max(0, CurrentHealth - actualDamage);
        nextDamageTime = Time.time + damageInterval;
        lastDamageTime = Time.time;
        healthRegenerationRemainder = 0f;
        staminaRegenerationRemainder = 0f;

        if (flash != null)
        {
            StartCoroutine(flash.FlashRoutine());
        }

        WorldDamageNumbers.Show(transform.position + Vector3.up * 0.8f, actualDamage, new Color(1f, 0.35f, 0.35f));
        GameAudio.PlayDamage();

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

    public bool ConsumeStamina(int amount)
    {
        if (amount <= 0) return true;
        if (IsDead || CurrentStamina < amount) return false;

        CurrentStamina -= amount;
        StatsChanged?.Invoke();
        return true;
    }

    public void RestoreStamina(int amount)
    {
        if (amount <= 0 || IsDead) return;
        int restored = Mathf.Min(amount, MaxStamina - CurrentStamina);
        if (restored <= 0) return;

        CurrentStamina += restored;
        StatsChanged?.Invoke();
    }

    public void GrantStatusImmunity(float fireSeconds, float poisonSeconds)
    {
        if (fireSeconds > 0f)
        {
            fireImmunityUntil = Mathf.Max(fireImmunityUntil, Time.time + fireSeconds);
            if (fireStatusRoutine != null) StopCoroutine(fireStatusRoutine);
            fireStatusRoutine = null;
        }
        if (poisonSeconds > 0f)
        {
            poisonImmunityUntil = Mathf.Max(poisonImmunityUntil, Time.time + poisonSeconds);
            if (poisonStatusRoutine != null) StopCoroutine(poisonStatusRoutine);
            poisonStatusRoutine = null;
        }
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.color = normalSpriteColor;
        StatsChanged?.Invoke();
    }

    public void ApplyStatusDamage(bool fire, int damagePerSecond, float duration, Color tint)
    {
        if (IsDead || damagePerSecond <= 0 || duration <= 0f ||
            (fire ? FireImmune : PoisonImmune)) return;
        if (fire)
        {
            if (fireStatusRoutine != null) StopCoroutine(fireStatusRoutine);
            fireStatusRoutine = StartCoroutine(StatusDamageRoutine(true, damagePerSecond, duration, tint));
        }
        else
        {
            if (poisonStatusRoutine != null) StopCoroutine(poisonStatusRoutine);
            poisonStatusRoutine = StartCoroutine(StatusDamageRoutine(false, damagePerSecond, duration, tint));
        }
    }

    private IEnumerator StatusDamageRoutine(bool fire, int damagePerSecond, float duration, Color tint)
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        bool greenFlash = false;
        while (elapsed < duration && !IsDead)
        {
            if (renderer != null)
            {
                greenFlash = !greenFlash;
                renderer.color = greenFlash ? tint : normalSpriteColor;
            }
            yield return new WaitForSeconds(1f);
            if (IsDead || (fire ? FireImmune : PoisonImmune)) break;
            TakeDamage(damagePerSecond + Defense);
            elapsed += 1f;
        }
        if (renderer != null) renderer.color = normalSpriteColor;
        if (fire) fireStatusRoutine = null;
        else poisonStatusRoutine = null;
    }

    // Mantém XP excedente ao subir de nível; cada nível aumenta dano de espada e defesa base por propriedades calculadas.
    public void GainExperience(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int previousLevel = level;
        int previousMaxHealth = MaxHealth;
        int previousMaxStamina = MaxStamina;
        experience += amount;
        while (experience >= ExperienceToNextLevel)
        {
            experience -= ExperienceToNextLevel;
            level++;
        }

        if (level > previousLevel)
        {
            GameAudio.PlayLevelUp();
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + MaxHealth - previousMaxHealth);
            CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + MaxStamina - previousMaxStamina);
            PlayerProfileService.RecordLevelReached(level, MaxHealth, MaxStamina);
            RefreshBonuses();
            FindAnyObjectByType<SaveController>()?.SaveGame();
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
        fireImmunityUntil = 0f;
        poisonImmunityUntil = 0f;
        lastDamageTime = Time.time;
        healthRegenerationRemainder = 0f;
        staminaRegenerationRemainder = 0f;
        UpdateAliveState();
        StatsChanged?.Invoke();
    }

    public void RestoreSavedVitals(int health, int stamina, int savedLevel = 1,
                                   int savedExperience = 0, float fireImmunity = 0f,
                                   float poisonImmunity = 0f)
    {
        level = Mathf.Max(1, savedLevel);
        experience = Mathf.Clamp(savedExperience, 0, ExperienceToNextLevel - 1);
        RefreshBonuses();
        CurrentHealth = Mathf.Clamp(health, 0, MaxHealth);
        CurrentStamina = Mathf.Clamp(stamina, 0, MaxStamina);
        fireImmunityUntil = Time.time + Mathf.Max(0f, fireImmunity);
        poisonImmunityUntil = Time.time + Mathf.Max(0f, poisonImmunity);
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

    // Regenera os dois atributos em passos inteiros depois de alguns segundos sem receber dano.
    private void RegenerateVitals()
    {
        if (IsDead || Time.timeScale <= 0f || Time.time < lastDamageTime + regenerationDelay)
        {
            return;
        }

        bool changed = false;
        if (CurrentHealth < MaxHealth && healthRegenerationPerSecond > 0f)
        {
            healthRegenerationRemainder += healthRegenerationPerSecond * Time.deltaTime;
            int amount = Mathf.FloorToInt(healthRegenerationRemainder);
            if (amount > 0)
            {
                healthRegenerationRemainder -= amount;
                CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
                changed = true;
            }
        }

        if (CurrentStamina < MaxStamina && staminaRegenerationPerSecond > 0f)
        {
            staminaRegenerationRemainder += staminaRegenerationPerSecond * Time.deltaTime;
            int amount = Mathf.FloorToInt(staminaRegenerationRemainder);
            if (amount > 0)
            {
                staminaRegenerationRemainder -= amount;
                CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + amount);
                changed = true;
            }
        }

        if (changed) StatsChanged?.Invoke();
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
                int armorGrowth = (slot.slotType == EquipmentSlotType.Chest ||
                                   slot.slotType == EquipmentSlotType.Helmet ||
                                   slot.slotType == EquipmentSlotType.Boots)
                    ? level / Mathf.Max(1, armorBonusEveryLevels) : 0;
                BonusDefense += Mathf.Max(0, item.defenseBonus) + armorGrowth;
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

    private void UpdateShieldState()
    {
        bool hasShield = equipmentController != null &&
            equipmentController.GetSlot(EquipmentSlotType.Shield) != null &&
            equipmentController.GetSlot(EquipmentSlotType.Shield).CurrentItem != null;
        if (hasShield == shieldWasEquipped) return;

        shieldWasEquipped = hasShield;
        if (shieldRoutine != null) StopCoroutine(shieldRoutine);
        shieldProtectionActive = false;
        shieldRoutine = hasShield ? StartCoroutine(ShieldCycleRoutine()) : null;
    }

    private IEnumerator ShieldCycleRoutine()
    {
        while (shieldWasEquipped && !IsDead)
        {
            shieldProtectionActive = true;
            StatsChanged?.Invoke();
            yield return new WaitForSeconds(10f);
            shieldProtectionActive = false;
            StatsChanged?.Invoke();
            yield return new WaitForSeconds(60f);
        }
        shieldProtectionActive = false;
        shieldRoutine = null;
    }
}
