using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerCombat2D : MonoBehaviour
{
    [Header("Ataques")]
    [SerializeField, Min(0.1f)] private float attackRange = 1.15f;
    [SerializeField, Min(0.1f)] private float attackRadius = 0.7f;
    [SerializeField, Min(0)] private int basicDamage = 12;
    [SerializeField, Min(0)] private int heavyDamage = 30;
    [SerializeField, Min(1)] private int heavyEnergyCost = 25;
    [SerializeField, Min(0f)] private float basicCooldown = 0.35f;
    [SerializeField, Min(0f)] private float heavyCooldown = 0.7f;

    [Header("Arco e flechas")]
    [SerializeField, Min(1)] private int arrowDamage = 18;
    [SerializeField, Min(0.1f)] private float arrowProjectileSpeed = 10f;
    [SerializeField, Min(0f)] private float bowCooldown = 0.45f;

    [Header("Poder de Contato: colher magnetica")]
    [SerializeField, Min(1)] private int magneticDamage = 30;
    [SerializeField, Min(1)] private int magneticEnergyCost = 15;
    [SerializeField, Min(0.1f)] private float magneticProjectileSpeed = 8f;

    [Header("Poder de Absorcao: maca venenosa")]
    [SerializeField, Min(1)] private int poisonDirectDamage = 6;
    [SerializeField, Min(1)] private int poisonDamagePerTick = 4;
    [SerializeField, Min(1)] private int poisonTicks = 3;
    [SerializeField, Min(1)] private int poisonEnergyCost = 20;
    [SerializeField, Min(0.1f)] private float poisonProjectileSpeed = 7f;

    [Header("Poder de Contato: luva vermelha")]
    [SerializeField, Min(1f)] private float gloveBaseDamageMultiplier = 1.5f;
    [SerializeField, Min(1)] private int gloveUppercutDamage = 22;
    [SerializeField, Min(1)] private int gloveUppercutEnergyCost = 15;
    [SerializeField, Min(1)] private int gloveSlamDamage = 18;
    [SerializeField, Min(1)] private int gloveSlamEnergyCost = 25;
    [SerializeField, Min(0.1f)] private float gloveSlamRadius = 1.8f;

    private PlayerVitals vitals;
    private PlayerInteractor2D interactor;
    private PlayerPowerLoadout2D powerLoadout;
    private PlayerMaterialPouch2D materialPouch;
    private PlayerInventory2D inventory;
    private PlayerEquipment2D equipment;
    private SpriteRenderer playerRenderer;
    private Camera mainCamera;
    private Vector2 aimDirection = Vector2.down;
    private float nextAttackTime;

    private void Awake()
    {
        vitals = GetComponent<PlayerVitals>();
        interactor = GetComponent<PlayerInteractor2D>();
        powerLoadout = GetComponent<PlayerPowerLoadout2D>();
        materialPouch = GetComponent<PlayerMaterialPouch2D>();
        inventory = GetComponent<PlayerInventory2D>();
        equipment = GetComponent<PlayerEquipment2D>();
        playerRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        bool controlsBlocked = IsDialogueOpen() || (inventory != null && inventory.IsOpen);
        Keyboard keyboard = Keyboard.current;
        bool blockHeld = !controlsBlocked && keyboard != null &&
            (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed);
        if (vitals != null)
        {
            vitals.SetBlocking(blockHeld);
        }

        if (controlsBlocked || blockHeld)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            UpdateAim(mouse);
        }

        if (keyboard != null)
        {
            if (keyboard.tabKey.wasPressedThisFrame && powerLoadout != null)
            {
                powerLoadout.CycleContactPower();
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                TryMagneticShot();
            }

            if (keyboard.fKey.wasPressedThisFrame)
            {
                TryGloveUppercut();
            }

            if (keyboard.gKey.wasPressedThisFrame)
            {
                TryGloveSlam();
            }
        }

        if (mouse == null)
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (equipment != null && equipment.EquippedWeapon == InventoryItemId.Bow)
            {
                TryBowShot();
            }
            else
            {
                TryAttack(basicDamage, 0, basicCooldown);
            }
        }
        else if (mouse.rightButton.wasPressedThisFrame)
        {
            if (powerLoadout != null && powerLoadout.ActiveAbsorptionPower == AbsorptionPowerId.PoisonApple)
            {
                TryPoisonShot();
            }
            else
            {
                TryAttack(heavyDamage, heavyEnergyCost, heavyCooldown);
            }
        }
    }

    private bool IsDialogueOpen()
    {
        return interactor != null && interactor.DialogueManager != null && interactor.DialogueManager.BlocksWorldInput;
    }

    private void UpdateAim(Mouse mouse)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        Vector2 screenPosition = mouse.position.ReadValue();
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z));
        Vector2 direction = (Vector2)worldPosition - (Vector2)transform.position;
        if (direction.sqrMagnitude > 0.01f)
        {
            aimDirection = direction.normalized;
        }
    }

    private void TryAttack(int damage, int energyCost, float cooldown)
    {
        if (Time.time < nextAttackTime || vitals == null)
        {
            return;
        }

        if (!vitals.TrySpendEnergy(energyCost))
        {
            return;
        }

        if (equipment != null)
        {
            damage = equipment.ApplyAttackBonus(damage);
        }

        if (powerLoadout != null && powerLoadout.EquippedContactPower == ContactPowerId.RedGloveStrength)
        {
            damage = Mathf.RoundToInt(damage * gloveBaseDamageMultiplier);
        }

        nextAttackTime = Time.time + cooldown;
        Vector2 origin = transform.position;
        Vector2 hitCenter = origin + aimDirection * attackRange;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(hitCenter, attackRadius);
        HashSet<EnemyHealth2D> damagedEnemies = new HashSet<EnemyHealth2D>();

        foreach (Collider2D hitCollider in colliders)
        {
            EnemyHealth2D enemy = hitCollider.GetComponentInParent<EnemyHealth2D>();
            if (enemy == null || damagedEnemies.Contains(enemy))
            {
                continue;
            }

            Vector2 directionToEnemy = (Vector2)enemy.transform.position - origin;
            if (directionToEnemy.sqrMagnitude > 0.01f && Vector2.Dot(directionToEnemy.normalized, aimDirection) < 0.2f)
            {
                continue;
            }

            damagedEnemies.Add(enemy);
            enemy.ReceiveDamage(damage);
        }

        Debug.DrawLine(origin, hitCenter, energyCost > 0 ? Color.red : Color.yellow, 0.2f);
    }

    private void TryMagneticShot()
    {
        if (powerLoadout == null || powerLoadout.EquippedContactPower != ContactPowerId.SpoonMagnetism ||
            materialPouch == null || materialPouch.MetalScrapCount < 1 ||
            vitals == null || vitals.CurrentEnergy < magneticEnergyCost)
        {
            return;
        }

        Sprite projectileSprite = playerRenderer != null ? playerRenderer.sprite : null;
        if (projectileSprite == null)
        {
            Debug.LogWarning("O poder magnetico precisa de um sprite para representar a peca de metal.", this);
            return;
        }

        if (!vitals.TrySpendEnergy(magneticEnergyCost) || !materialPouch.TrySpendMetalScrap())
        {
            return;
        }

        GameObject projectile = new GameObject("Peca de metal magnetizada");
        projectile.transform.position = transform.position + (Vector3)(aimDirection * 0.8f);
        projectile.transform.localScale = Vector3.one * 0.3f;

        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = projectileSprite;
        renderer.color = new Color(0.68f, 0.73f, 0.78f);
        renderer.sortingOrder = 21;

        Rigidbody2D body = projectile.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;

        MagneticMetalProjectile2D metalProjectile = projectile.AddComponent<MagneticMetalProjectile2D>();
        metalProjectile.Initialize(aimDirection, magneticProjectileSpeed, magneticDamage);
    }

    private void TryBowShot()
    {
        if (inventory == null || inventory.CountItem(InventoryItemId.Arrow) < 1 || Time.time < nextAttackTime)
        {
            return;
        }

        Sprite projectileSprite = playerRenderer != null ? playerRenderer.sprite : null;
        if (projectileSprite == null)
        {
            Debug.LogWarning("O arco precisa de um sprite para representar as flechas.", this);
            return;
        }

        if (!inventory.TryRemoveItem(InventoryItemId.Arrow))
        {
            return;
        }

        nextAttackTime = Time.time + bowCooldown;
        GameObject arrow = new GameObject("Flecha do arco");
        arrow.transform.position = transform.position + (Vector3)(aimDirection * 0.8f);
        arrow.transform.localScale = new Vector3(0.42f, 0.16f, 1f);
        arrow.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg);

        SpriteRenderer renderer = arrow.AddComponent<SpriteRenderer>();
        renderer.sprite = projectileSprite;
        renderer.color = new Color(0.48f, 0.31f, 0.16f);
        renderer.sortingOrder = 21;

        Rigidbody2D body = arrow.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D collider = arrow.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.2f;

        ArrowProjectile2D arrowProjectile = arrow.AddComponent<ArrowProjectile2D>();
        arrowProjectile.Initialize(transform, aimDirection, arrowProjectileSpeed, arrowDamage);
    }

    private void TryGloveUppercut()
    {
        if (powerLoadout == null || powerLoadout.EquippedContactPower != ContactPowerId.RedGloveStrength)
        {
            return;
        }

        TryAttack(gloveUppercutDamage, gloveUppercutEnergyCost, heavyCooldown);
    }

    private void TryGloveSlam()
    {
        if (powerLoadout == null || powerLoadout.EquippedContactPower != ContactPowerId.RedGloveStrength ||
            vitals == null || Time.time < nextAttackTime)
        {
            return;
        }

        if (!vitals.TrySpendEnergy(gloveSlamEnergyCost))
        {
            return;
        }

        nextAttackTime = Time.time + heavyCooldown;
        int damage = Mathf.RoundToInt(gloveSlamDamage * gloveBaseDamageMultiplier);
        if (equipment != null)
        {
            damage = equipment.ApplyAttackBonus(damage);
        }
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, gloveSlamRadius);
        HashSet<EnemyHealth2D> damagedEnemies = new HashSet<EnemyHealth2D>();

        foreach (Collider2D hitCollider in colliders)
        {
            EnemyHealth2D enemy = hitCollider.GetComponentInParent<EnemyHealth2D>();
            if (enemy != null && damagedEnemies.Add(enemy))
            {
                enemy.ReceiveDamage(damage);
            }
        }
    }

    private void TryPoisonShot()
    {
        if (Time.time < nextAttackTime || vitals == null || vitals.CurrentEnergy < poisonEnergyCost)
        {
            return;
        }

        Sprite projectileSprite = playerRenderer != null ? playerRenderer.sprite : null;
        if (projectileSprite == null)
        {
            Debug.LogWarning("O ataque de veneno precisa de um sprite para representar a bolha.", this);
            return;
        }

        if (!vitals.TrySpendEnergy(poisonEnergyCost))
        {
            return;
        }

        nextAttackTime = Time.time + heavyCooldown;

        GameObject projectile = new GameObject("Bolha de veneno");
        projectile.transform.position = transform.position + (Vector3)(aimDirection * 0.8f);
        projectile.transform.localScale = Vector3.one * 0.3f;

        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = projectileSprite;
        renderer.color = new Color(0.45f, 0.9f, 0.2f);
        renderer.sortingOrder = 21;

        Rigidbody2D body = projectile.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;

        PoisonProjectile2D poisonProjectile = projectile.AddComponent<PoisonProjectile2D>();
        poisonProjectile.Initialize(aimDirection, poisonProjectileSpeed, poisonDirectDamage, poisonDamagePerTick, poisonTicks);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere((Vector2)transform.position + aimDirection * attackRange, attackRadius);
    }
}
