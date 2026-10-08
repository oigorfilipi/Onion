using UnityEngine;
using UnityEngine.UI;

/// <summary>Define em qual slot de equipamento um item pode ser colocado.</summary>
public enum ItemEquipmentType
{
    None,
    PrimaryWeapon,
    Shield,
    SecondaryWeapon,
    Power,
    Backpack,
    Chest,
    Helmet,
    Boots
}

/// <summary>
/// Define os dados compartilhados pelo item do mundo e sua cópia na UI: identidade, categoria, combate, bônus e cura.
/// </summary>
public class Item : MonoBehaviour
{
    public bool IsRuntimeSpawn { get; private set; }
    public void MarkRuntimeSpawn() => IsRuntimeSpawn = true;
    public string RewardDropId { get; private set; }
    public void SetRewardDropId(string id) => RewardDropId = id;
    private string worldSaveKey;
    public string WorldSaveKey => string.IsNullOrEmpty(worldSaveKey)
        ? worldSaveKey = WorldProgressKey.For(this) : worldSaveKey;

    private void Awake()
    {
        worldSaveKey = WorldProgressKey.For(this);
    }

    public int ID;
    public string Name;

    [Header("Inventário")]
    [Tooltip("Junta até 99 unidades de itens comuns e poderes absorvíveis no inventário e na mochila.")]
    public bool stackable;
    public int MaxStackSize => stackable &&
        (equipmentType == ItemEquipmentType.None ||
         (equipmentType == ItemEquipmentType.Power && powerType == ItemPowerType.Absorbable))
        ? 99 : 1;

    [Header("Equipamento")]
    public ItemEquipmentType equipmentType = ItemEquipmentType.None;
    public ItemPowerType powerType = ItemPowerType.None;

    [Header("Arma")]
    [Min(0)] public int weaponDamage = 1;
    public GameObject weaponPrefab;

    [Header("Atributos e consumo")]
    [Min(0)] public int defenseBonus;
    [Min(0)] public int healthBonus;
    [Min(0)] public int staminaBonus;
    [Min(0)] public int healAmount;
    [Min(0)] public int staminaRestore;
    [Min(0f)] public float fireImmunitySeconds;
    [Min(0f)] public float poisonImmunitySeconds;
    [Min(1f)] public float speedMultiplier = 1f;
    [Min(0f)] public float speedDuration;

    // Mostra o nome e ícone do item ao coletá-lo no mundo; o item físico é removido pelo coletor.
    public virtual void PickUp()
    {
        Sprite itemIcon = GetPickupIcon();
        if (ItemPickupUIController.Instance != null)
        {
            ItemPickupUIController.Instance.ShowItemPickup(Name, itemIcon);
        }
    }

    private Sprite GetPickupIcon()
    {
        Image itemImage = GetComponent<Image>();
        if (itemImage == null)
        {
            itemImage = GetComponentInChildren<Image>();
        }

        if (itemImage != null && itemImage.sprite != null)
        {
            return itemImage.sprite;
        }

        SpriteRenderer itemRenderer = GetComponent<SpriteRenderer>();
        if (itemRenderer == null)
        {
            itemRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        return itemRenderer != null ? itemRenderer.sprite : null;
    }
}
