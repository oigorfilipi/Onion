using System;
using UnityEngine;

/// <summary>
/// Valida o tipo de item equipado, bloqueia poderes absorvíveis e anuncia mudanças aos outros sistemas.
/// </summary>
public class EquipmentSlot : MonoBehaviour, IItemSlot
{
    public EquipmentSlotType slotType = EquipmentSlotType.None;
    public GameObject currentItem;

    public event Action<EquipmentSlot> ItemChanged;

    public Transform SlotTransform => transform;
    public GameObject CurrentItem
    {
        get
        {
            if (currentItem != null)
            {
                return currentItem;
            }

            // Find a child item without mutating the slot here. TrySetItem registers
            // it and raises ItemChanged so equipment systems can refresh.
            foreach (Transform child in transform)
            {
                Item childItem = child.GetComponent<Item>();
                if (childItem != null && child.gameObject.activeSelf && CanAccept(childItem))
                {
                    return child.gameObject;
                }
            }

            return null;
        }
    }

    // Absorbable powers stay locked in their power slot until a game action removes them.
    public bool IsLocked
    {
        get
        {
            GameObject itemObject = CurrentItem;
            Item item = itemObject != null ? itemObject.GetComponent<Item>() : null;
            return item != null && item.equipmentType == ItemEquipmentType.Power &&
                   item.powerType == ItemPowerType.Absorbable;
        }
    }

    public bool CanRemoveItem => !IsLocked && (canRemoveValidator == null || canRemoveValidator());

    private Func<bool> canRemoveValidator;

    public void SetCanRemoveValidator(Func<bool> validator)
    {
        canRemoveValidator = validator;
    }

    // Impede colocar itens comuns em slots de equipamento e reserva Poder1 e Poder2 para tipos de poder definidos.
    public bool CanAccept(Item item)
    {
        if (item == null || slotType == EquipmentSlotType.None)
        {
            return false;
        }

        if (slotType == EquipmentSlotType.Power1 || slotType == EquipmentSlotType.Power2)
        {
            return item.equipmentType == ItemEquipmentType.Power && item.powerType != ItemPowerType.None;
        }

        if (item.powerType != ItemPowerType.None)
        {
            return false;
        }

        return item.equipmentType == RequiredItemType();
    }

    private ItemEquipmentType RequiredItemType()
    {
        switch (slotType)
        {
            case EquipmentSlotType.PrimaryWeapon: return ItemEquipmentType.PrimaryWeapon;
            case EquipmentSlotType.Shield: return ItemEquipmentType.Shield;
            case EquipmentSlotType.SecondaryWeapon: return ItemEquipmentType.SecondaryWeapon;
            case EquipmentSlotType.Backpack: return ItemEquipmentType.Backpack;
            case EquipmentSlotType.Chest: return ItemEquipmentType.Chest;
            case EquipmentSlotType.Helmet: return ItemEquipmentType.Helmet;
            case EquipmentSlotType.Boots: return ItemEquipmentType.Boots;
            default: return ItemEquipmentType.None;
        }
    }

    public bool TrySetItem(GameObject itemObject)
    {
        Item item = itemObject != null ? itemObject.GetComponent<Item>() : null;
        if (!CanAccept(item))
        {
            return false;
        }

        GameObject existingItem = CurrentItem;
        if (existingItem != null && existingItem != itemObject)
        {
            return false;
        }

        bool changed = currentItem != itemObject;
        currentItem = itemObject;
        if (changed)
        {
            ItemChanged?.Invoke(this);
        }

        return true;
    }

    public bool TryRemoveItem(GameObject itemObject)
    {
        if (CurrentItem != itemObject || !CanRemoveItem)
        {
            return false;
        }

        currentItem = null;
        ItemChanged?.Invoke(this);
        return true;
    }

    // A ação própria de retirada libera o slot e destrói definitivamente o poder absorvido.
    public bool RemoveAbsorbablePower()
    {
        GameObject itemObject = CurrentItem;
        Item item = itemObject != null ? itemObject.GetComponent<Item>() : null;
        if (item == null || item.equipmentType != ItemEquipmentType.Power ||
            item.powerType != ItemPowerType.Absorbable)
        {
            return false;
        }

        GameObject consumedItem = itemObject;
        currentItem = null;
        ItemChanged?.Invoke(this);
        Destroy(consumedItem);
        return true;
    }

    public bool RestoreItem(GameObject itemObject)
    {
        if (itemObject == null || !CanAccept(itemObject.GetComponent<Item>()))
        {
            return false;
        }

        GameObject existingItem = CurrentItem;
        bool changed = currentItem != itemObject;
        if (existingItem == itemObject)
        {
            currentItem = itemObject;
            if (changed)
            {
                ItemChanged?.Invoke(this);
            }

            return true;
        }

        if (existingItem != null)
        {
            Destroy(existingItem);
        }

        currentItem = itemObject;
        ItemChanged?.Invoke(this);
        return true;
    }

    public void ClearForLoad()
    {
        GameObject existingItem = CurrentItem;
        if (existingItem != null)
        {
            existingItem.SetActive(false);
            Destroy(existingItem);
            currentItem = null;
            ItemChanged?.Invoke(this);
        }
    }
}
