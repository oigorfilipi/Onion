using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Sincroniza os slots de espada e arco com os ataques e visuais do personagem.
/// </summary>
public class ActiveWeapon : MonoBehaviour
{
    [SerializeField] private Vector2 bowDisplayOffset = new Vector2(0f, 1f);
    [SerializeField, Min(0f)] private float bowFacingSideOffset = 0.2f;
    [SerializeField] private float bowFloatAmplitude = 0.06f;
    [SerializeField] private float bowFloatFrequency = 3f;
    [SerializeField, Min(0f)] private float bowVisibleAfterShot = 0.25f;
    [SerializeField] private float arrowSpeed = 12f;
    [SerializeField] private float arrowLifetime = 4f;
    [SerializeField] private float arrowSpriteAngleOffset;
    [SerializeField] private Sprite arrowProjectileSprite;
    [SerializeField] private Vector2 arrowSpawnOffset = new Vector2(0f, 0.45f);
    [SerializeField, Min(0.01f)] private float arrowWorldScale = 1f;
    [SerializeField, Min(0.01f)] private float arrowColliderRadius = 0.12f;
    [SerializeField, Min(0)] private int arrowDamage = 7;
    [SerializeField, Range(0f, 1f)] private float diamondCriticalChance = 0.1f;

    private PlayerControls playerControls;
    private InputAction secondaryAttackAction;
    private PlayerController playerController;
    private PlayerVitals playerVitals;
    private EquipmentController equipmentController;
    private InventoryController inventoryController;
    private BackpackController backpackController;
    private ItemDictionary itemDictionary;
    private MenuController menuController;
    private Sword legacySword;
    private Sword equippedSword;
    private SpriteRenderer equippedSwordRenderer;
    private Sprite equippedSwordSprite;
    private GameObject equippedSwordInstance;
    private GameObject equippedSwordPrefab;
    private Item equippedPrimaryItem;
    private Item equippedSecondaryItem;
    private SpriteRenderer bowDisplayRenderer;
    private bool showingBow;
    private float bowVisibleUntil;

    private void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
        playerVitals = GetComponentInParent<PlayerVitals>();
        equipmentController = GetComponentInParent<EquipmentController>();
        inventoryController = FindAnyObjectByType<InventoryController>();
        backpackController = GetComponentInParent<BackpackController>();
        itemDictionary = FindAnyObjectByType<ItemDictionary>();
        menuController = FindAnyObjectByType<MenuController>();
        legacySword = GetComponentInChildren<Sword>(true);

        playerControls = new PlayerControls();
        playerControls.Combat.Attack.performed += OnPrimaryAttack;
        secondaryAttackAction = new InputAction(
            "SecondaryAttack",
            InputActionType.Button,
            "<Mouse>/rightButton");
        secondaryAttackAction.performed += OnSecondaryAttack;

        if (equipmentController != null)
        {
            equipmentController.EquipmentChanged += RefreshEquippedWeapons;
        }

        if (legacySword != null)
        {
            legacySword.enabled = false;
            legacySword.SetAttackColliderActive(false);
            SpriteRenderer swordRenderer = legacySword.GetComponent<SpriteRenderer>();
            if (swordRenderer != null)
            {
                swordRenderer.enabled = false;
            }
        }
    }

    private void OnEnable()
    {
        playerControls?.Combat.Enable();
        secondaryAttackAction?.Enable();
    }

    private void Start()
    {
        RefreshEquippedWeapons();
    }

    private void Update()
    {
        RefreshWeaponsIfSlotChanged();

        if (showingBow && Time.time >= bowVisibleUntil &&
            (secondaryAttackAction == null || !secondaryAttackAction.IsPressed()))
        {
            showingBow = false;
            UpdateWeaponVisibility();
        }

        UpdateBowDisplay();
    }

    private void LateUpdate()
    {
        if (equippedSwordRenderer != null && equippedSwordSprite != null &&
            equippedSwordRenderer.sprite != equippedSwordSprite)
        {
            equippedSwordRenderer.sprite = equippedSwordSprite;
        }
    }

    private void OnDisable()
    {
        playerControls?.Combat.Disable();
        secondaryAttackAction?.Disable();
        showingBow = false;
        if (bowDisplayRenderer != null)
        {
            bowDisplayRenderer.enabled = false;
        }

        if (equippedSword != null)
        {
            equippedSword.SetAttackColliderActive(false);
        }
    }

    private void OnDestroy()
    {
        if (equipmentController != null)
        {
            equipmentController.EquipmentChanged -= RefreshEquippedWeapons;
        }

        if (playerControls != null)
        {
            playerControls.Combat.Attack.performed -= OnPrimaryAttack;
            playerControls.Dispose();
        }

        if (secondaryAttackAction != null)
        {
            secondaryAttackAction.performed -= OnSecondaryAttack;
            secondaryAttackAction.Dispose();
        }
    }

    // Lê os slots para instanciar a espada correta e preparar o visual do arco sem perder o item de origem.
    private void RefreshEquippedWeapons()
    {
        if (equipmentController == null)
        {
            return;
        }

        EquipmentSlot primarySlot = equipmentController.GetSlot(EquipmentSlotType.PrimaryWeapon);
        EquipmentSlot secondarySlot = equipmentController.GetSlot(EquipmentSlotType.SecondaryWeapon);

        Item newPrimaryItem = primarySlot != null && primarySlot.CurrentItem != null
            ? primarySlot.CurrentItem.GetComponent<Item>()
            : null;
        equippedSecondaryItem = secondarySlot != null && secondarySlot.CurrentItem != null
            ? secondarySlot.CurrentItem.GetComponent<Item>()
            : null;

        bool hasPrimaryWeapon = newPrimaryItem != null &&
                                newPrimaryItem.equipmentType == ItemEquipmentType.PrimaryWeapon;
        GameObject desiredSwordPrefab = hasPrimaryWeapon ? newPrimaryItem.weaponPrefab : null;
        bool swordChanged = equippedPrimaryItem != newPrimaryItem ||
                            equippedSwordPrefab != desiredSwordPrefab ||
                            (desiredSwordPrefab != null && equippedSwordInstance == null);
        equippedPrimaryItem = newPrimaryItem;

        if (swordChanged)
        {
            EquipSwordPrefab(desiredSwordPrefab);
        }

        UpdateEquippedSwordVisual();

        bool hasSecondaryWeapon = equippedSecondaryItem != null &&
                                  equippedSecondaryItem.equipmentType == ItemEquipmentType.SecondaryWeapon;
        if (hasSecondaryWeapon)
        {
            EnsureBowDisplay();
            bowDisplayRenderer.sprite = GetItemSprite(equippedSecondaryItem);
            MatchBowToSword();
        }
        else
        {
            showingBow = false;
        }

        UpdateWeaponVisibility();
    }

    private void RefreshWeaponsIfSlotChanged()
    {
        if (equipmentController == null)
        {
            return;
        }

        EquipmentSlot primarySlot = equipmentController.GetSlot(EquipmentSlotType.PrimaryWeapon);
        EquipmentSlot secondarySlot = equipmentController.GetSlot(EquipmentSlotType.SecondaryWeapon);
        GameObject primaryObject = primarySlot != null ? primarySlot.CurrentItem : null;
        GameObject secondaryObject = secondarySlot != null ? secondarySlot.CurrentItem : null;
        Item primaryItem = primaryObject != null ? primaryObject.GetComponent<Item>() : null;
        Item secondaryItem = secondaryObject != null ? secondaryObject.GetComponent<Item>() : null;

        if (primaryItem != equippedPrimaryItem || secondaryItem != equippedSecondaryItem)
        {
            RefreshEquippedWeapons();
        }
    }

    private void OnPrimaryAttack(InputAction.CallbackContext context)
    {
        if (IsInventoryInteraction())
        {
            return;
        }

        if (equippedPrimaryItem == null ||
            equippedPrimaryItem.equipmentType != ItemEquipmentType.PrimaryWeapon ||
            equippedSword == null)
        {
            return;
        }

        showingBow = false;
        UpdateWeaponVisibility();
        int attackDamage = GetSwordDamage();
        bool diamondSword = equippedPrimaryItem.Name.ToLowerInvariant().Contains("diamante");
        if (diamondSword && Random.value < diamondCriticalChance) attackDamage = 15;
        equippedSword.PerformAttack(attackDamage);
        GameSession.RecordWeaponAttack(false, equippedPrimaryItem.Name.ToLowerInvariant().Contains("ferro"));
    }

    private void OnSecondaryAttack(InputAction.CallbackContext context)
    {
        if (IsInventoryInteraction())
        {
            return;
        }

        RefreshEquippedWeapons();
        if (FireArrow())
        {
            showingBow = true;
            bowVisibleUntil = Time.time + bowVisibleAfterShot;
            UpdateWeaponVisibility();
        }
    }

    private void EquipSwordPrefab(GameObject swordPrefab)
    {
        if (equippedSwordInstance != null)
        {
            equippedSwordInstance.SetActive(false);
            Destroy(equippedSwordInstance);
        }

        equippedSword = null;
        equippedSwordRenderer = null;
        equippedSwordSprite = null;
        equippedSwordInstance = null;
        equippedSwordPrefab = swordPrefab;

        if (swordPrefab == null)
        {
            return;
        }

        equippedSwordInstance = Instantiate(swordPrefab, transform, false);
        equippedSwordInstance.name = $"{equippedPrimaryItem.Name} Equipped";
        equippedSwordInstance.transform.localPosition = Vector3.zero;
        equippedSwordInstance.transform.localRotation = Quaternion.identity;
        equippedSword = equippedSwordInstance.GetComponentInChildren<Sword>(true);

        if (equippedSword == null)
        {
            Debug.LogError($"The equipped weapon prefab '{swordPrefab.name}' has no Sword component.", swordPrefab);
            equippedSwordInstance.SetActive(false);
            Destroy(equippedSwordInstance);
            equippedSwordInstance = null;
            return;
        }

        equippedSword.enabled = true;
        equippedSwordRenderer = equippedSword.GetComponent<SpriteRenderer>();
        equippedSword.SetAttackColliderActive(false);
    }

    private void UpdateEquippedSwordVisual()
    {
        if (legacySword != null)
        {
            legacySword.SetAttackColliderActive(false);
            SpriteRenderer legacyRenderer = legacySword.GetComponent<SpriteRenderer>();
            if (legacyRenderer != null)
            {
                legacyRenderer.enabled = false;
            }
        }

        if (equippedSword == null || equippedPrimaryItem == null)
        {
            equippedSwordSprite = null;
            return;
        }

        equippedSwordSprite = GetItemSprite(equippedPrimaryItem);
        if (equippedSwordRenderer != null && equippedSwordSprite != null)
        {
            equippedSwordRenderer.sprite = equippedSwordSprite;
        }

        equippedSword.SetDamage(GetSwordDamage());
    }

    private int GetSwordDamage()
    {
        return equippedPrimaryItem != null
            ? equippedPrimaryItem.weaponDamage + (playerVitals != null ? playerVitals.SwordDamageBonus : 0)
            : 0;
    }

    // Exige arco e flecha, consome uma unidade e monta o projétil físico com o sprite do item.
    private bool FireArrow()
    {
        if (equippedSecondaryItem == null ||
            equippedSecondaryItem.equipmentType != ItemEquipmentType.SecondaryWeapon)
        {
            Debug.LogWarning("Equipe o arco no slot de Arma Secundária antes de disparar.", this);
            return false;
        }

        Sprite arrowSprite = arrowProjectileSprite;
        if (arrowSprite == null && itemDictionary != null)
        {
            GameObject arrowItemPrefab = itemDictionary.GetItemPrefab("Flecha");
            arrowSprite = arrowItemPrefab != null
                ? GetItemSprite(arrowItemPrefab.GetComponent<Item>())
                : null;
        }

        if (arrowSprite == null)
        {
            Debug.LogError("Defina Arrow Projectile Sprite no Active Weapon ou configure o sprite do item Flecha.", this);
            return false;
        }

        if (!TryConsumeArrow())
        {
            Debug.LogWarning("Você precisa ter pelo menos uma Flecha no inventário ou na mochila equipada.", this);
            return false;
        }

        Vector2 aimDirection = GetAimDirection();
        Vector3 spawnPosition = (playerController != null ? playerController.transform.position : transform.position)
                                + (Vector3)arrowSpawnOffset;
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg + arrowSpriteAngleOffset;

        GameObject arrow = new GameObject("Arrow Projectile");
        arrow.transform.position = spawnPosition;
        arrow.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        arrow.transform.localScale = Vector3.one * arrowWorldScale;

        SpriteRenderer arrowRenderer = arrow.AddComponent<SpriteRenderer>();
        arrowRenderer.sprite = arrowSprite;
        arrowRenderer.sortingLayerID = bowDisplayRenderer != null ? bowDisplayRenderer.sortingLayerID : 0;
        arrowRenderer.sortingOrder = bowDisplayRenderer != null ? bowDisplayRenderer.sortingOrder + 1 : 2;

        Rigidbody2D body = arrow.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        CircleCollider2D arrowCollider = arrow.AddComponent<CircleCollider2D>();
        arrowCollider.isTrigger = true;
        arrowCollider.radius = arrowColliderRadius;

        if (playerController != null)
        {
            foreach (Collider2D playerCollider in playerController.GetComponentsInChildren<Collider2D>(true))
            {
                if (playerCollider != null)
                {
                    Physics2D.IgnoreCollision(arrowCollider, playerCollider);
                }
            }
        }

        ArrowProjectile projectile = arrow.GetComponent<ArrowProjectile>();
        if (projectile == null)
        {
            projectile = arrow.AddComponent<ArrowProjectile>();
        }

        int bowDamage = arrowDamage + (playerVitals != null ? playerVitals.SwordDamageBonus : 0);
        projectile.Launch(aimDirection, arrowSpeed, bowDamage, arrowLifetime, playerVitals);
        GameAudio.PlayArrow();
        GameSession.RecordWeaponAttack(true);
        return true;
    }

    // Procura munição nos espaços permitidos antes de confirmar o disparo.
    private bool TryConsumeArrow()
    {
        if (inventoryController != null && inventoryController.TryConsumeItem("Flecha"))
        {
            return true;
        }

        return backpackController != null && backpackController.TryConsumeItem("Flecha");
    }

    private Vector2 GetAimDirection()
    {
        if (playerController == null || Camera.main == null || Mouse.current == null)
        {
            return Vector2.right;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        float distanceFromCamera = Mathf.Abs(Camera.main.transform.position.z - playerController.transform.position.z);
        Vector3 screenPosition = new Vector3(mousePosition.x, mousePosition.y, distanceFromCamera);
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 direction = mouseWorldPosition - playerController.transform.position;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
    }

    private void EnsureBowDisplay()
    {
        if (bowDisplayRenderer != null || playerController == null)
        {
            return;
        }

        GameObject bowDisplay = new GameObject("Equipped Bow Display");
        bowDisplay.transform.SetParent(playerController.transform, false);
        bowDisplay.transform.localPosition = bowDisplayOffset;
        bowDisplayRenderer = bowDisplay.AddComponent<SpriteRenderer>();
        bowDisplayRenderer.enabled = false;
        bowDisplayRenderer.sortingOrder = 1;
    }

    private void MatchBowToSword()
    {
        if (bowDisplayRenderer == null || bowDisplayRenderer.sprite == null || playerController == null)
        {
            return;
        }

        Sword referenceSword = equippedSword != null ? equippedSword : legacySword;
        SpriteRenderer swordRenderer = referenceSword != null
            ? referenceSword.GetComponent<SpriteRenderer>()
            : null;
        if (swordRenderer == null || swordRenderer.sprite == null)
        {
            return;
        }

        bowDisplayRenderer.sortingLayerID = swordRenderer.sortingLayerID;
        bowDisplayRenderer.sortingOrder = swordRenderer.sortingOrder;

        Vector3 swordSize = swordRenderer.sprite.bounds.size;
        Vector3 swordScale = swordRenderer.transform.lossyScale;
        float swordWorldSize = Mathf.Max(
            swordSize.x * Mathf.Abs(swordScale.x),
            swordSize.y * Mathf.Abs(swordScale.y));

        Vector3 bowSize = bowDisplayRenderer.sprite.bounds.size;
        Vector3 playerScale = playerController.transform.lossyScale;
        float bowWorldSizeAtScaleOne = Mathf.Max(
            bowSize.x * Mathf.Abs(playerScale.x),
            bowSize.y * Mathf.Abs(playerScale.y));

        if (bowWorldSizeAtScaleOne > 0.0001f)
        {
            bowDisplayRenderer.transform.localScale = Vector3.one *
                                                      (swordWorldSize / bowWorldSizeAtScaleOne);
        }
    }

    private void UpdateWeaponVisibility()
    {
        bool bowEquipped = equippedSecondaryItem != null &&
                           equippedSecondaryItem.equipmentType == ItemEquipmentType.SecondaryWeapon &&
                           bowDisplayRenderer != null && bowDisplayRenderer.sprite != null;
        bool displayBow = showingBow && bowEquipped;

        if (bowDisplayRenderer != null)
        {
            bowDisplayRenderer.enabled = displayBow;
        }

        if (equippedSword != null)
        {
            SpriteRenderer swordRenderer = equippedSword.GetComponent<SpriteRenderer>();
            if (swordRenderer != null)
            {
                swordRenderer.enabled = !displayBow && equippedPrimaryItem != null;
            }

            if (displayBow)
            {
                equippedSword.SetAttackColliderActive(false);
            }
        }
    }

    private void UpdateBowDisplay()
    {
        if (bowDisplayRenderer == null || !bowDisplayRenderer.enabled || playerController == null)
        {
            return;
        }

        float bob = Mathf.Sin(Time.time * bowFloatFrequency) * bowFloatAmplitude;
        float side = playerController.FacingLeft ? -bowFacingSideOffset : bowFacingSideOffset;
        bowDisplayRenderer.transform.localPosition = new Vector3(
            bowDisplayOffset.x + side,
            bowDisplayOffset.y + bob,
            0f);

        Vector2 aimDirection = GetAimDirection();
        // Compensa a orientação original do sprite do arco.
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg + 180f;
        bowDisplayRenderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private bool IsInventoryInteraction()
    {
        return Time.timeScale <= 0f ||
               (menuController != null && menuController.menuCanvas != null &&
                menuController.menuCanvas.activeInHierarchy);
    }

    private static Sprite GetItemSprite(Item item)
    {
        if (item == null)
        {
            return null;
        }

        Image image = item.GetComponentInChildren<Image>(true);
        if (image != null && image.sprite != null)
        {
            return image.sprite;
        }

        SpriteRenderer spriteRenderer = item.GetComponentInChildren<SpriteRenderer>(true);
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

}
