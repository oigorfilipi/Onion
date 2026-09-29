using UnityEngine;

public enum EquipmentSlot2D
{
    PrimaryWeapon = 0,
    SecondaryWeapon = 1,
    Weapon = PrimaryWeapon,
    Chest = 2,
    Helmet = 3,
    Boots = 4,
    Shield = 5,
    Backpack = 6,
    Contact = 7
}

public sealed class PlayerEquipment2D : MonoBehaviour
{
    [Header("Equipado")]
    [SerializeField] private InventoryItemId equippedWeapon;
    [SerializeField] private InventoryItemId equippedSecondaryWeapon;
    [SerializeField] private InventoryItemId equippedChest;
    [SerializeField] private InventoryItemId equippedHelmet;
    [SerializeField] private InventoryItemId equippedBoots;
    [SerializeField] private InventoryItemId equippedShield;
    [SerializeField] private InventoryItemId equippedBackpack;

    [Header("Efeitos provisórios")]
    [SerializeField, Min(1f)] private float swordDamageMultiplier = 1.5f;
    [SerializeField, Min(0)] private int chestDamageReduction = 3;
    [SerializeField, Min(0)] private int helmetDamageReduction = 2;
    [SerializeField, Min(0)] private int bootsDamageReduction = 1;

    public InventoryItemId EquippedWeapon => equippedWeapon;
    public InventoryItemId EquippedPrimaryWeapon => equippedWeapon;
    public InventoryItemId EquippedSecondaryWeapon => equippedSecondaryWeapon;
    public InventoryItemId EquippedChest => equippedChest;
    public InventoryItemId EquippedHelmet => equippedHelmet;
    public InventoryItemId EquippedBoots => equippedBoots;
    public InventoryItemId EquippedShield => equippedShield;
    public InventoryItemId EquippedBackpack => equippedBackpack;
    public bool HasBackpack => equippedBackpack == InventoryItemId.Backpack;
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
        InventoryItemId itemToEquip = inventory != null ? inventory.GetItemAt(inventorySlot) : InventoryItemId.None;
        EquipmentSlot2D? slot = GetSlotFor(itemToEquip);
        if (slot == EquipmentSlot2D.PrimaryWeapon)
        {
            return TryEquipSelected(inventory, inventorySlot, EquipmentSlot2D.PrimaryWeapon, out resultMessage);
        }

        return TryEquipSelected(inventory, inventorySlot, slot ?? EquipmentSlot2D.PrimaryWeapon, out resultMessage);
    }

    public bool TryEquipSelected(PlayerInventory2D inventory, int inventorySlot, EquipmentSlot2D targetSlot, out string resultMessage)
    {
        resultMessage = "Esse item nao pode ser equipado.";
        if (inventory == null)
        {
            resultMessage = "Inventario nao encontrado.";
            return false;
        }

        InventoryItemId itemToEquip = inventory.GetItemAt(inventorySlot);
        EquipmentSlot2D? validSlot = GetSlotFor(itemToEquip);
        if (!validSlot.HasValue || !CanEquipInSlot(itemToEquip, targetSlot))
        {
            return false;
        }

        InventoryItemId currentItem = GetEquippedItem(targetSlot);
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

        SetEquippedItem(targetSlot, itemToEquip);
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

        if (slot == EquipmentSlot2D.Backpack && inventory.HasItemsInRange(PlayerInventory2D.RequiredSlotCount, PlayerInventory2D.MaximumSlotCount))
        {
            resultMessage = "Esvazie os espacos extras da mochila antes de desequipa-la.";
            return false;
        }

        if (slot == EquipmentSlot2D.Backpack && !inventory.HasEmptySlotInRange(0, PlayerInventory2D.RequiredSlotCount))
        {
            resultMessage = "Libere um espaco nos 40 slots principais antes de desequipar a mochila.";
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
        return ApplyAttackBonus(baseDamage, equippedWeapon);
    }

    public int ApplyAttackBonus(int baseDamage, InventoryItemId weapon)
    {
        if (weapon == InventoryItemId.Sword)
        {
            return Mathf.RoundToInt(baseDamage * swordDamageMultiplier);
        }

        return baseDamage;
    }

    public int ReduceIncomingDamage(int incomingDamage)
    {
        return Mathf.Max(0, incomingDamage - TotalDamageReduction);
    }

    public bool CanBlock => equippedShield == InventoryItemId.Shield ||
        equippedWeapon == InventoryItemId.Sword || equippedSecondaryWeapon == InventoryItemId.Sword;

    public int BlockEnergyCost => equippedShield == InventoryItemId.Shield ? 10 : CanBlock ? 15 : 0;

    public float BlockDamageMultiplier => equippedShield == InventoryItemId.Shield ? 0.5f : CanBlock ? 0.85f : 1f;

    public bool SwapWeaponRoles()
    {
        if (equippedWeapon == InventoryItemId.None || equippedSecondaryWeapon == InventoryItemId.None)
        {
            return false;
        }

        InventoryItemId previousPrimary = equippedWeapon;
        equippedWeapon = equippedSecondaryWeapon;
        equippedSecondaryWeapon = previousPrimary;
        return true;
    }

    public static EquipmentSlot2D? GetSlotFor(InventoryItemId itemId)
    {
        switch (itemId)
        {
            case InventoryItemId.Sword:
            case InventoryItemId.Bow: return EquipmentSlot2D.PrimaryWeapon;
            case InventoryItemId.ChestArmor: return EquipmentSlot2D.Chest;
            case InventoryItemId.Helmet: return EquipmentSlot2D.Helmet;
            case InventoryItemId.MetalBoots: return EquipmentSlot2D.Boots;
            case InventoryItemId.Shield: return EquipmentSlot2D.Shield;
            case InventoryItemId.Backpack: return EquipmentSlot2D.Backpack;
            default: return null;
        }
    }

    public InventoryItemId GetEquippedItem(EquipmentSlot2D slot)
    {
        switch (slot)
        {
            case EquipmentSlot2D.PrimaryWeapon: return equippedWeapon;
            case EquipmentSlot2D.SecondaryWeapon: return equippedSecondaryWeapon;
            case EquipmentSlot2D.Chest: return equippedChest;
            case EquipmentSlot2D.Helmet: return equippedHelmet;
            case EquipmentSlot2D.Boots: return equippedBoots;
            case EquipmentSlot2D.Shield: return equippedShield;
            case EquipmentSlot2D.Backpack: return equippedBackpack;
            default: return InventoryItemId.None;
        }
    }

    private void SetEquippedItem(EquipmentSlot2D slot, InventoryItemId itemId)
    {
        switch (slot)
        {
            case EquipmentSlot2D.PrimaryWeapon: equippedWeapon = itemId; break;
            case EquipmentSlot2D.SecondaryWeapon: equippedSecondaryWeapon = itemId; break;
            case EquipmentSlot2D.Chest: equippedChest = itemId; break;
            case EquipmentSlot2D.Helmet: equippedHelmet = itemId; break;
            case EquipmentSlot2D.Boots: equippedBoots = itemId; break;
            case EquipmentSlot2D.Shield: equippedShield = itemId; break;
            case EquipmentSlot2D.Backpack: equippedBackpack = itemId; break;
        }
    }

    private static bool CanEquipInSlot(InventoryItemId itemId, EquipmentSlot2D slot)
    {
        switch (slot)
        {
            case EquipmentSlot2D.PrimaryWeapon:
            case EquipmentSlot2D.SecondaryWeapon:
                return itemId == InventoryItemId.Sword || itemId == InventoryItemId.Bow;
            case EquipmentSlot2D.Chest: return itemId == InventoryItemId.ChestArmor;
            case EquipmentSlot2D.Helmet: return itemId == InventoryItemId.Helmet;
            case EquipmentSlot2D.Boots: return itemId == InventoryItemId.MetalBoots;
            case EquipmentSlot2D.Shield: return itemId == InventoryItemId.Shield;
            case EquipmentSlot2D.Backpack: return itemId == InventoryItemId.Backpack;
            default: return false;
        }
    }
}
