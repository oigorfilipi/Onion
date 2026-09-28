using UnityEngine;

public enum EquipmentSlot2D
{
    Weapon,
    Chest,
    Helmet,
    Boots
}

public sealed class PlayerEquipment2D : MonoBehaviour
{
    [Header("Equipado")]
    [SerializeField] private InventoryItemId equippedWeapon;
    [SerializeField] private InventoryItemId equippedChest;
    [SerializeField] private InventoryItemId equippedHelmet;
    [SerializeField] private InventoryItemId equippedBoots;

    [Header("Efeitos provisórios")]
    [SerializeField, Min(1f)] private float swordDamageMultiplier = 1.5f;
    [SerializeField, Min(0)] private int chestDamageReduction = 3;
    [SerializeField, Min(0)] private int helmetDamageReduction = 2;
    [SerializeField, Min(0)] private int bootsDamageReduction = 1;

    public InventoryItemId EquippedWeapon => equippedWeapon;
    public InventoryItemId EquippedChest => equippedChest;
    public InventoryItemId EquippedHelmet => equippedHelmet;
    public InventoryItemId EquippedBoots => equippedBoots;
    public int TotalDamageReduction =>
        (equippedChest == InventoryItemId.ChestArmor ? chestDamageReduction : 0) +
        (equippedHelmet == InventoryItemId.Helmet ? helmetDamageReduction : 0) +
        (equippedBoots == InventoryItemId.MetalBoots ? bootsDamageReduction : 0);

    public bool CanEquipItem(InventoryItemId itemId)
    {
        return GetSlotFor(itemId) != null;
    }

    public bool TryEquipSelected(PlayerInventory2D inventory, int inventorySlot, out string resultMessage)
    {
        resultMessage = "Esse item nao pode ser equipado.";
        if (inventory == null)
        {
            resultMessage = "Inventario nao encontrado.";
            return false;
        }

        InventoryItemId itemToEquip = inventory.GetItemAt(inventorySlot);
        EquipmentSlot2D? targetSlot = GetSlotFor(itemToEquip);
        if (!targetSlot.HasValue)
        {
            return false;
        }

        InventoryItemId currentItem = GetEquippedItem(targetSlot.Value);
        if (currentItem == itemToEquip)
        {
            resultMessage = "Esse item ja esta equipado.";
            return false;
        }

        if (!inventory.TryRemoveItemAt(inventorySlot))
        {
            resultMessage = "Nao foi possivel retirar o item do inventario.";
            return false;
        }

        if (currentItem != InventoryItemId.None && !inventory.TryAddItem(currentItem))
        {
            inventory.TryAddItem(itemToEquip);
            resultMessage = "Libere um espaco no inventario antes de trocar esse equipamento.";
            return false;
        }

        SetEquippedItem(targetSlot.Value, itemToEquip);
        resultMessage = InventoryItemCatalog.GetDisplayName(itemToEquip) + " equipado.";
        return true;
    }

    public bool TryUnequip(EquipmentSlot2D slot, PlayerInventory2D inventory, out string resultMessage)
    {
        resultMessage = "Inventario nao encontrado.";
        if (inventory == null)
        {
            return false;
        }

        InventoryItemId currentItem = GetEquippedItem(slot);
        if (currentItem == InventoryItemId.None)
        {
            resultMessage = "Esse espaco de equipamento esta vazio.";
            return false;
        }

        if (!inventory.TryAddItem(currentItem))
        {
            resultMessage = "Inventario cheio. Libere um espaco para desequipar.";
            return false;
        }

        SetEquippedItem(slot, InventoryItemId.None);
        resultMessage = InventoryItemCatalog.GetDisplayName(currentItem) + " desequipado.";
        return true;
    }

    public int ApplyAttackBonus(int baseDamage)
    {
        if (equippedWeapon == InventoryItemId.Sword)
        {
            return Mathf.RoundToInt(baseDamage * swordDamageMultiplier);
        }

        return baseDamage;
    }

    public int ReduceIncomingDamage(int incomingDamage)
    {
        return Mathf.Max(0, incomingDamage - TotalDamageReduction);
    }

    public static EquipmentSlot2D? GetSlotFor(InventoryItemId itemId)
    {
        switch (itemId)
        {
            case InventoryItemId.Sword:
            case InventoryItemId.Bow: return EquipmentSlot2D.Weapon;
            case InventoryItemId.ChestArmor: return EquipmentSlot2D.Chest;
            case InventoryItemId.Helmet: return EquipmentSlot2D.Helmet;
            case InventoryItemId.MetalBoots: return EquipmentSlot2D.Boots;
            default: return null;
        }
    }

    public InventoryItemId GetEquippedItem(EquipmentSlot2D slot)
    {
        switch (slot)
        {
            case EquipmentSlot2D.Weapon: return equippedWeapon;
            case EquipmentSlot2D.Chest: return equippedChest;
            case EquipmentSlot2D.Helmet: return equippedHelmet;
            case EquipmentSlot2D.Boots: return equippedBoots;
            default: return InventoryItemId.None;
        }
    }

    private void SetEquippedItem(EquipmentSlot2D slot, InventoryItemId itemId)
    {
        switch (slot)
        {
            case EquipmentSlot2D.Weapon: equippedWeapon = itemId; break;
            case EquipmentSlot2D.Chest: equippedChest = itemId; break;
            case EquipmentSlot2D.Helmet: equippedHelmet = itemId; break;
            case EquipmentSlot2D.Boots: equippedBoots = itemId; break;
        }
    }
}
