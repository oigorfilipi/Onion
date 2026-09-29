using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class InventorySlot2D
{
    public InventoryItemId itemId;
    [Min(0)] public int quantity;

    public bool IsEmpty => itemId == InventoryItemId.None || quantity <= 0;
}

public sealed class PlayerInventory2D : MonoBehaviour
{
    public const int RequiredSlotCount = 40;
    public const int BackpackSlotBonus = 20;
    public const int MaximumSlotCount = RequiredSlotCount + BackpackSlotBonus;

    [SerializeField, Min(1)] private int maxStackPerSlot = 99;
    [SerializeField] private List<InventorySlot2D> slots = new List<InventorySlot2D>();
    private PlayerEquipment2D equipment;

    public IReadOnlyList<InventorySlot2D> Slots => slots;
    public int SlotCount
    {
        get
        {
            if (equipment == null)
            {
                equipment = GetComponent<PlayerEquipment2D>();
            }

            return equipment != null && equipment.HasBackpack ? MaximumSlotCount : RequiredSlotCount;
        }
    }
    public bool IsOpen { get; private set; }

    public event Action InventoryChanged;

    private void Awake()
    {
        equipment = GetComponent<PlayerEquipment2D>();
        EnsureSlots();
        maxStackPerSlot = Mathf.Max(1, maxStackPerSlot);
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
    }

    public int CountItem(InventoryItemId itemId)
    {
        EnsureSlots();
        int count = 0;
        for (int i = 0; i < SlotCount; i++)
        {
            InventorySlot2D slot = slots[i];
            if (slot != null && slot.itemId == itemId)
            {
                count += slot.quantity;
            }
        }

        return count;
    }

    public InventoryItemId GetItemAt(int slotIndex)
    {
        EnsureSlots();
        if (slotIndex < 0 || slotIndex >= SlotCount || slots[slotIndex].IsEmpty)
        {
            return InventoryItemId.None;
        }

        return slots[slotIndex].itemId;
    }

    public bool TryRemoveItemAt(int slotIndex, int amount = 1)
    {
        EnsureSlots();
        if (slotIndex < 0 || slotIndex >= SlotCount || amount <= 0)
        {
            return false;
        }

        InventorySlot2D slot = slots[slotIndex];
        if (slot.IsEmpty || slot.quantity < amount)
        {
            return false;
        }

        slot.quantity -= amount;
        ClearIfEmpty(slot);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool CanAddItem(InventoryItemId itemId, int amount = 1)
    {
        EnsureSlots();
        if (itemId == InventoryItemId.None || amount <= 0)
        {
            return false;
        }

        int stackLimit = GetStackLimit(itemId);
        int remainingCapacity = 0;
        for (int i = 0; i < SlotCount; i++)
        {
            InventorySlot2D slot = slots[i];
            if (slot.IsEmpty)
            {
                remainingCapacity += stackLimit;
            }
            else if (InventoryItemCatalog.IsStackable(itemId) && slot.itemId == itemId)
            {
                remainingCapacity += Mathf.Max(0, stackLimit - slot.quantity);
            }

            if (remainingCapacity >= amount)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryAddItem(InventoryItemId itemId, int amount = 1)
    {
        if (!CanAddItem(itemId, amount))
        {
            return false;
        }

        int remaining = amount;
        int stackLimit = GetStackLimit(itemId);

        if (InventoryItemCatalog.IsStackable(itemId))
        {
            for (int i = 0; i < SlotCount; i++)
            {
                InventorySlot2D slot = slots[i];
                if (!slot.IsEmpty && slot.itemId == itemId && slot.quantity < stackLimit)
                {
                    int added = Mathf.Min(remaining, stackLimit - slot.quantity);
                    slot.quantity += added;
                    remaining -= added;
                    if (remaining == 0) break;
                }
            }
        }

        for (int i = 0; i < SlotCount; i++)
        {
            InventorySlot2D slot = slots[i];
            if (!slot.IsEmpty) continue;

            int added = Mathf.Min(remaining, stackLimit);
            slot.itemId = itemId;
            slot.quantity = added;
            remaining -= added;
            if (remaining == 0) break;
        }

        InventoryChanged?.Invoke();
        return true;
    }

    public bool TryRemoveItem(InventoryItemId itemId, int amount = 1)
    {
        if (itemId == InventoryItemId.None || amount <= 0 || CountItem(itemId) < amount)
        {
            return false;
        }

        int remaining = amount;
        for (int i = SlotCount - 1; i >= 0 && remaining > 0; i--)
        {
            InventorySlot2D slot = slots[i];
            if (slot.IsEmpty || slot.itemId != itemId) continue;

            int removed = Mathf.Min(remaining, slot.quantity);
            slot.quantity -= removed;
            remaining -= removed;
            ClearIfEmpty(slot);
        }

        InventoryChanged?.Invoke();
        return true;
    }

    public bool TryUseItemAt(int slotIndex, PlayerVitals vitals, out string resultMessage)
    {
        resultMessage = "Esse item nao pode ser usado diretamente.";
        if (slotIndex < 0 || slotIndex >= SlotCount || vitals == null)
        {
            return false;
        }

        InventorySlot2D slot = slots[slotIndex];
        if (slot.IsEmpty)
        {
            resultMessage = "Esse espaco esta vazio.";
            return false;
        }

        if (slot.itemId == InventoryItemId.Bread)
        {
            if (vitals.CurrentHealth >= vitals.MaxHealth)
            {
                resultMessage = "Sua vida ja esta cheia.";
                return false;
            }

            vitals.RestoreHealth(50);
            resultMessage = "Pao usado: +50 de vida.";
        }
        else if (slot.itemId == InventoryItemId.Cheese)
        {
            if (vitals.CurrentEnergy >= vitals.MaxEnergy)
            {
                resultMessage = "Sua estamina ja esta cheia.";
                return false;
            }

            vitals.RestoreEnergy(50);
            resultMessage = "Queijo usado: +50 de estamina.";
        }
        else
        {
            return false;
        }

        slot.quantity--;
        ClearIfEmpty(slot);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool IsConsumableAt(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < SlotCount && !slots[slotIndex].IsEmpty &&
               InventoryItemCatalog.IsConsumable(slots[slotIndex].itemId);
    }

    public bool HasItemsInRange(int startIndex, int endExclusive)
    {
        EnsureSlots();
        int start = Mathf.Clamp(startIndex, 0, slots.Count);
        int end = Mathf.Clamp(endExclusive, start, slots.Count);
        for (int i = start; i < end; i++)
        {
            if (!slots[i].IsEmpty)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasEmptySlotInRange(int startIndex, int endExclusive)
    {
        EnsureSlots();
        int start = Mathf.Clamp(startIndex, 0, slots.Count);
        int end = Mathf.Clamp(endExclusive, start, slots.Count);
        for (int i = start; i < end; i++)
        {
            if (slots[i].IsEmpty)
            {
                return true;
            }
        }

        return false;
    }

    private int GetStackLimit(InventoryItemId itemId)
    {
        return InventoryItemCatalog.IsStackable(itemId) ? maxStackPerSlot : 1;
    }

    private void EnsureSlots()
    {
        if (slots == null)
        {
            slots = new List<InventorySlot2D>();
        }

        while (slots.Count < MaximumSlotCount)
        {
            slots.Add(new InventorySlot2D());
        }

        if (slots.Count > MaximumSlotCount)
        {
            slots.RemoveRange(MaximumSlotCount, slots.Count - MaximumSlotCount);
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = new InventorySlot2D();
            }
        }
    }

    private static void ClearIfEmpty(InventorySlot2D slot)
    {
        if (slot.quantity <= 0)
        {
            slot.quantity = 0;
            slot.itemId = InventoryItemId.None;
        }
    }
}
