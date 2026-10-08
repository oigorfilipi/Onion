using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reúne os slots de equipamento, avisa quando mudam e reconstrói seus itens a partir do save.
/// </summary>
public class EquipmentController : MonoBehaviour
{
    [SerializeField] private EquipmentSlot[] equipmentSlots;

    private ItemDictionary itemDictionary;

    public event Action EquipmentChanged;

    public EquipmentSlot[] EquipmentSlots => equipmentSlots;

    private void Awake()
    {
        if (equipmentSlots == null || equipmentSlots.Length == 0)
        {
            EquipmentSlot[] sceneSlots = FindObjectsByType<EquipmentSlot>(
                FindObjectsInactive.Include);

            List<EquipmentSlot> localSlots = new List<EquipmentSlot>();
            foreach (EquipmentSlot slot in sceneSlots)
            {
                if (slot.gameObject.scene == gameObject.scene)
                {
                    localSlots.Add(slot);
                }
            }

            equipmentSlots = localSlots.ToArray();
        }

        Array.Sort(equipmentSlots, CompareHierarchyOrder);

        foreach (EquipmentSlot slot in equipmentSlots)
        {
            if (slot != null)
            {
                slot.ItemChanged += HandleEquipmentChanged;
            }
        }

        ValidateSlotTypes();

        itemDictionary = FindAnyObjectByType<ItemDictionary>();
        ConfigureBackpackRemovalRule();
    }

    private void OnDestroy()
    {
        if (equipmentSlots == null)
        {
            return;
        }

        foreach (EquipmentSlot slot in equipmentSlots)
        {
            if (slot != null)
            {
                slot.ItemChanged -= HandleEquipmentChanged;
            }
        }
    }

    public EquipmentSlot GetSlot(EquipmentSlotType slotType)
    {
        if (equipmentSlots == null)
        {
            return null;
        }

        foreach (EquipmentSlot slot in equipmentSlots)
        {
            if (slot != null && slot.slotType == slotType)
            {
                return slot;
            }
        }

        return null;
    }

    public EquipmentSlot[] GetPowerSlots()
    {
        return new[]
        {
            GetSlot(EquipmentSlotType.Power1),
            GetSlot(EquipmentSlotType.Power2)
        };
    }

    public bool HasEquipped(EquipmentSlotType slotType)
    {
        EquipmentSlot slot = GetSlot(slotType);
        return slot != null && slot.CurrentItem != null;
    }

    public List<EquipmentSaveData> GetEquipmentItems()
    {
        List<EquipmentSaveData> savedItems = new List<EquipmentSaveData>();
        if (equipmentSlots == null)
        {
            return savedItems;
        }

        foreach (EquipmentSlot slot in equipmentSlots)
        {
            if (slot == null || slot.CurrentItem == null)
            {
                continue;
            }

            Item item = slot.CurrentItem.GetComponent<Item>();
            if (item != null)
            {
                savedItems.Add(new EquipmentSaveData
                {
                    itemID = item.ID,
                    slotType = slot.slotType
                });
            }
        }

        return savedItems;
    }

    public void SetEquipmentItems(List<EquipmentSaveData> savedItems)
    {
        if (equipmentSlots == null)
        {
            return;
        }

        foreach (EquipmentSlot slot in equipmentSlots)
        {
            if (slot != null)
            {
                slot.ClearForLoad();
            }
        }

        if (savedItems == null)
        {
            return;
        }

        if (itemDictionary == null)
        {
            itemDictionary = FindAnyObjectByType<ItemDictionary>();
        }

        if (itemDictionary == null)
        {
            Debug.LogError("Could not load equipped items: no ItemDictionary was found in the scene.");
            return;
        }

        foreach (EquipmentSaveData savedItem in savedItems)
        {
            EquipmentSlot slot = GetSlot(savedItem.slotType);
            GameObject prefab = itemDictionary.GetItemPrefab(savedItem.itemID);

            if (slot == null || prefab == null)
            {
                continue;
            }

            GameObject uiItem = InventoryController.CreateInventoryItem(prefab, slot.transform);
            if (uiItem == null || !slot.RestoreItem(uiItem))
            {
                if (uiItem != null)
                {
                    Destroy(uiItem);
                }
            }
        }
    }

    public bool RemoveAbsorbablePower(EquipmentSlot slot)
    {
        return slot != null && slot.RemoveAbsorbablePower();
    }

    private void ConfigureBackpackRemovalRule()
    {
        EquipmentSlot backpackSlot = GetSlot(EquipmentSlotType.Backpack);
        BackpackController backpackController = GetComponent<BackpackController>();

        if (backpackSlot != null && backpackController != null)
        {
            backpackSlot.SetCanRemoveValidator(() => backpackController.IsBackpackEmpty);
        }
    }

    private void ValidateSlotTypes()
    {
        HashSet<EquipmentSlotType> configuredTypes = new HashSet<EquipmentSlotType>();
        foreach (EquipmentSlot slot in equipmentSlots)
        {
            if (slot == null)
            {
                continue;
            }

            if (slot.slotType == EquipmentSlotType.None)
            {
                Debug.LogWarning($"Equipment slot '{slot.name}' needs an Equipment Slot Type selected in the Inspector.", slot);
            }
            else if (!configuredTypes.Add(slot.slotType))
            {
                Debug.LogWarning($"More than one equipment slot is configured as '{slot.slotType}'.", slot);
            }
        }
    }

    private void HandleEquipmentChanged(EquipmentSlot changedSlot)
    {
        EquipmentChanged?.Invoke();
    }

    private static int CompareHierarchyOrder(EquipmentSlot first, EquipmentSlot second)
    {
        if (first == null) return second == null ? 0 : -1;
        if (second == null) return 1;
        return first.transform.GetSiblingIndex().CompareTo(second.transform.GetSiblingIndex());
    }
}
