using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;

/// <summary>Cria a barra rápida fixa de dez espaços e serializa seus itens junto com o save.</summary>
public class QuickbarController : MonoBehaviour
{
    public static event Action AnySlotChanged;
    private readonly List<QuickbarSlot> slots = new List<QuickbarSlot>(10);
    private ItemDictionary itemDictionary;
    private int selectedIndex = -1;

    public void Build(Transform parent)
    {
        if (parent == null) return;
        itemDictionary = FindAnyObjectByType<ItemDictionary>();
        slots.Clear();
        for (int i = 0; i < parent.childCount; i++)
        {
            QuickbarSlot existing = parent.GetChild(i).GetComponent<QuickbarSlot>();
            if (existing == null) continue;
            if (slots.Count == 10) break;
            existing.Initialize(this, existing.GetComponent<Image>());
            slots.Add(existing);
        }

        for (int i = slots.Count; i < 10; i++)
        {
            GameObject cell = new GameObject("QuickbarSlot_" + (i + 1), typeof(RectTransform), typeof(Image), typeof(QuickbarSlot));
            cell.transform.SetParent(parent, false);
            Image background = cell.GetComponent<Image>();
            background.color = Color.white;
            background.raycastTarget = true;
            QuickbarSlot slot = cell.GetComponent<QuickbarSlot>();
            slot.Initialize(this, background);
            RectTransform rect = cell.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(62f, 62f);
            slots.Add(slot);
        }
    }

    private void Update()
    {
        if (Keyboard.current == null || slots.Count < 10) return;
        Key[] keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
                       Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0 };
        for (int i = 0; i < keys.Length; i++)
            if (Keyboard.current[keys[i]].wasPressedThisFrame) Select(slots[i]);
    }

    public void Select(QuickbarSlot selected)
    {
        selectedIndex = slots.IndexOf(selected);
        for (int i = 0; i < slots.Count; i++) slots[i].SetSelected(i == selectedIndex);
    }

    public static void NotifySlotChanged() => AnySlotChanged?.Invoke();

    public int CountItem(string itemName)
    {
        int count = 0;
        foreach (QuickbarSlot slot in slots)
        {
            Item item = slot.CurrentItem != null ? slot.CurrentItem.GetComponent<Item>() : null;
            if (item != null && item.Name == itemName)
                count += ItemStack.Ensure(item.gameObject).Quantity;
        }
        return count;
    }

    public bool TryConsumeItem(string itemName)
    {
        foreach (QuickbarSlot slot in slots)
        {
            Item item = slot.CurrentItem != null ? slot.CurrentItem.GetComponent<Item>() : null;
            if (item == null || item.Name != itemName) continue;
            ItemStack stack = ItemStack.Ensure(item.gameObject);
            if (stack.Quantity > 1) stack.RemoveUpTo(1);
            else
            {
                slot.TryRemoveItem(item.gameObject);
                Destroy(item.gameObject);
            }
            return true;
        }
        return false;
    }

    public List<QuickbarSaveData> GetSavedItems()
    {
        List<QuickbarSaveData> result = new List<QuickbarSaveData>();
        for (int i = 0; i < slots.Count; i++)
        {
            Item item = slots[i].CurrentItem != null ? slots[i].CurrentItem.GetComponent<Item>() : null;
            if (item == null) continue;
            result.Add(new QuickbarSaveData
            {
                itemID = item.ID,
                slotIndex = i,
                quantity = ItemStack.Ensure(item.gameObject).Quantity
            });
        }
        return result;
    }

    public void SetSavedItems(List<QuickbarSaveData> savedItems)
    {
        if (slots.Count == 0) return;
        foreach (QuickbarSlot slot in slots)
        {
            GameObject existing = slot.CurrentItem;
            if (existing != null)
            {
                slot.TryRemoveItem(existing);
                Destroy(existing);
            }
        }
        if (savedItems == null) return;
        if (itemDictionary == null) itemDictionary = FindAnyObjectByType<ItemDictionary>();
        if (itemDictionary == null) return;
        foreach (QuickbarSaveData saved in savedItems)
        {
            if (saved.slotIndex < 0 || saved.slotIndex >= slots.Count) continue;
            GameObject prefab = itemDictionary.GetItemPrefab(saved.itemID);
            GameObject item = InventoryController.CreateInventoryItem(prefab, slots[saved.slotIndex].transform);
            if (item == null) continue;
            ItemStack.Ensure(item).SetQuantity(Mathf.Max(1, saved.quantity));
            slots[saved.slotIndex].TrySetItem(item);
        }
    }
}
