using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Recebe itens coletados, forma pilhas, cria ícones de UI e salva ou restaura os 40 slots principais.
/// </summary>
public class InventoryController : MonoBehaviour
{
    private ItemDictionary itemDictionary;

    public GameObject inventoryPanel;
    public GameObject slotPrefab;
    public int slotCount;
    public GameObject[] itemPrefabs;

    private void Start()
    {
        itemDictionary = FindAnyObjectByType<ItemDictionary>();
    }

    // Completa pilhas existentes, cria um ícone no primeiro slot livre ou encaminha o item à mochila equipada.
    public bool AddItem(Item worldItem)
    {
        if (worldItem == null || inventoryPanel == null)
        {
            Debug.LogError("Could not add item: the world item or inventory panel is missing.");
            return false;
        }

        if (itemDictionary == null)
        {
            itemDictionary = FindAnyObjectByType<ItemDictionary>();
        }

        if (itemDictionary == null)
        {
            Debug.LogError("Could not add item: no ItemDictionary was found in the scene.");
            return false;
        }

        GameObject inventoryItemPrefab = itemDictionary.GetItemPrefab(worldItem.Name);
        if (inventoryItemPrefab == null)
        {
            Debug.LogError($"Could not add item '{worldItem.Name}': no matching inventory prefab was found in ItemDictionary.");
            return false;
        }

        Item prefabItem = inventoryItemPrefab.GetComponent<Item>();
        BackpackController backpackController = FindAnyObjectByType<BackpackController>();
        if (prefabItem != null && prefabItem.MaxStackSize > 1)
        {
            foreach (Transform slotTransform in inventoryPanel.transform)
            {
                Slot slot = slotTransform.GetComponent<Slot>();
                Item existingItem = slot != null && slot.currentItem != null
                    ? slot.currentItem.GetComponent<Item>()
                    : null;
                if (ItemStack.CanCombine(prefabItem, existingItem) &&
                    ItemStack.Ensure(existingItem.gameObject).AddUpTo(1) == 1)
                {
                    return true;
                }
            }

            if (backpackController != null && backpackController.TryAddToExistingStack(prefabItem))
            {
                return true;
            }
        }

        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot != null && slot.currentItem == null)
            {
                GameObject inventoryItem = CreateInventoryItem(inventoryItemPrefab, slotTransform);
                if (inventoryItem == null || !slot.TrySetItem(inventoryItem))
                {
                    if (inventoryItem != null)
                    {
                        Destroy(inventoryItem);
                    }

                    return false;
                }

                return true;
            }
        }

        if (backpackController != null && backpackController.TryAddItem(worldItem))
        {
            return true;
        }

        Debug.Log("Inventory is full!");
        return false;
    }

    public int AddItemQuantity(Item worldItem, int quantity)
    {
        int added = 0;
        while (added < quantity && AddItem(worldItem)) added++;
        return added;
    }

    public int CountItem(string itemName)
    {
        if (inventoryPanel == null || string.IsNullOrEmpty(itemName)) return 0;
        int count = 0;
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            Item item = slot != null && slot.currentItem != null
                ? slot.currentItem.GetComponent<Item>() : null;
            if (item != null && item.Name == itemName)
                count += item.GetComponent<ItemStack>()?.Quantity ?? 1;
        }
        return count;
    }

    public bool TryConsumeItem(string itemName)
    {
        if (inventoryPanel == null || string.IsNullOrWhiteSpace(itemName))
        {
            return false;
        }

        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            Item item = slot != null && slot.currentItem != null
                ? slot.currentItem.GetComponent<Item>()
                : null;

            if (item == null || item.Name != itemName)
            {
                continue;
            }

            ItemStack stack = ItemStack.Ensure(item.gameObject);
            if (stack.Quantity > 1)
            {
                stack.RemoveUpTo(1);
                return true;
            }

            if (!slot.TryRemoveItem(item.gameObject))
            {
                continue;
            }

            Destroy(item.gameObject);
            return true;
        }

        return false;
    }

    public List<InventorySaveData> GetInventoryItems()
    {
        List<InventorySaveData> inventoryData = new List<InventorySaveData>();
        if (inventoryPanel == null)
        {
            return inventoryData;
        }

        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            Item item = slot != null && slot.currentItem != null
                ? slot.currentItem.GetComponent<Item>()
                : null;

            if (item != null)
            {
                inventoryData.Add(new InventorySaveData
                {
                    itemID = item.ID,
                    slotIndex = slotTransform.GetSiblingIndex(),
                    quantity = ItemStack.Ensure(item.gameObject).Quantity
                });
            }
        }

        return inventoryData;
    }

    // Ajusta a quantidade de slots e reconstrói cada item salvo com o prefab canônico do dicionário.
    public void SetInventoryItems(List<InventorySaveData> inventorySaveData)
    {
        if (inventoryPanel == null || slotPrefab == null)
        {
            Debug.LogError("Could not load inventory: the panel or slot prefab is not assigned.");
            return;
        }

        List<Slot> slots = new List<Slot>();
        foreach (Transform child in inventoryPanel.transform)
        {
            Slot slot = child.GetComponent<Slot>();
            if (slot != null)
            {
                slots.Add(slot);
            }
        }

        while (slots.Count > slotCount)
        {
            Slot extraSlot = slots[slots.Count - 1];
            slots.RemoveAt(slots.Count - 1);
            Destroy(extraSlot.gameObject);
        }

        while (slots.Count < slotCount)
        {
            GameObject slotObject = Instantiate(slotPrefab, inventoryPanel.transform);
            Slot slot = slotObject.GetComponent<Slot>();
            if (slot == null)
            {
                Debug.LogError("The inventory slot prefab does not contain the Slot component.");
                Destroy(slotObject);
                return;
            }

            slots.Add(slot);
        }

        foreach (Slot slot in slots)
        {
            if (slot.currentItem != null)
            {
                Destroy(slot.currentItem);
            }

            slot.currentItem = null;
        }

        if (itemDictionary == null)
        {
            itemDictionary = FindAnyObjectByType<ItemDictionary>();
        }

        if (itemDictionary == null || inventorySaveData == null)
        {
            return;
        }

        foreach (InventorySaveData savedItem in inventorySaveData)
        {
            if (savedItem.slotIndex < 0 || savedItem.slotIndex >= slots.Count)
            {
                continue;
            }

            GameObject itemPrefab = itemDictionary.GetItemPrefab(savedItem.itemID);
            Slot slot = slots[savedItem.slotIndex];
            if (itemPrefab == null || slot == null)
            {
                continue;
            }

            GameObject item = CreateInventoryItem(itemPrefab, slot.transform);
            if (item != null)
            {
                ItemStack.Ensure(item).SetQuantity(Mathf.Max(1, savedItem.quantity));
                if (!slot.TrySetItem(item))
                {
                    Destroy(item);
                }
            }
        }
    }

    // Desliga visual e colisão do mundo na cópia de UI e centraliza sua Image no slot.
    public static GameObject CreateInventoryItem(GameObject itemPrefab, Transform slotTransform)
    {
        if (itemPrefab == null || slotTransform == null)
        {
            return null;
        }

        GameObject item = Instantiate(itemPrefab, slotTransform, false);
        RectTransform itemRect = item.GetComponent<RectTransform>();
        Image itemImage = item.GetComponentInChildren<Image>(true);

        if (itemRect == null || itemImage == null)
        {
            Debug.LogError($"Inventory prefab '{itemPrefab.name}' needs a RectTransform and a UI Image.");
            Destroy(item);
            return null;
        }

        foreach (SpriteRenderer spriteRenderer in item.GetComponentsInChildren<SpriteRenderer>(true))
        {
            spriteRenderer.enabled = false;
        }

        foreach (Collider2D itemCollider in item.GetComponentsInChildren<Collider2D>(true))
        {
            itemCollider.enabled = false;
        }

        CenterItemInSlot(itemRect);
        ItemStack.Ensure(item);
        return item;
    }

    private static void CenterItemInSlot(RectTransform itemRect)
    {
        itemRect.anchorMin = new Vector2(0.5f, 0.5f);
        itemRect.anchorMax = new Vector2(0.5f, 0.5f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.anchoredPosition = Vector2.zero;
    }
}
