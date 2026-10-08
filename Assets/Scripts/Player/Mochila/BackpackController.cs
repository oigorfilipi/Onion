using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gerencia os dez espaços extras da mochila e só abre sua janela na aba Inventário.
/// </summary>
public class BackpackController : MonoBehaviour
{
    private const int InventoryTabIndex = 1;

    [SerializeField] private GameObject backpackWindow;
    [SerializeField] private Transform backpackSlotsPanel;

    private PlayerControls playerControls;
    private EquipmentController equipmentController;
    private ItemDictionary itemDictionary;
    private MenuController menuController;
    private TabController tabController;
    private bool subscribedToTabChanges;

    public bool IsBackpackEquipped => equipmentController != null &&
                                      equipmentController.HasEquipped(EquipmentSlotType.Backpack);

    public bool IsBackpackEmpty
    {
        get
        {
            if (backpackSlotsPanel == null)
            {
                return true;
            }

            foreach (Slot slot in backpackSlotsPanel.GetComponentsInChildren<Slot>(true))
            {
                if (slot.currentItem != null)
                {
                    return false;
                }
            }

            return true;
        }
    }

    private void Awake()
    {
        equipmentController = GetComponent<EquipmentController>();
        itemDictionary = FindAnyObjectByType<ItemDictionary>();
        playerControls = new PlayerControls();
        playerControls.Equipment.ToggleBackpack.performed += OnToggleBackpack;

        FindBackpackObjects();
        DetachWindowFromInventoryMenu();
        FindInventoryMenu();
    }

    private void Start()
    {
        if (equipmentController != null)
        {
            equipmentController.EquipmentChanged += HandleEquipmentChanged;
        }

        if (backpackWindow != null)
        {
            backpackWindow.SetActive(false);
        }
    }

    private void OnEnable()
    {
        playerControls?.Equipment.Enable();
    }

    private void OnDisable()
    {
        playerControls?.Equipment.Disable();
    }

    private void OnDestroy()
    {
        if (equipmentController != null)
        {
            equipmentController.EquipmentChanged -= HandleEquipmentChanged;
        }

        if (subscribedToTabChanges && tabController != null)
        {
            tabController.TabChanged -= HandleTabChanged;
        }

        if (playerControls != null)
        {
            playerControls.Equipment.ToggleBackpack.performed -= OnToggleBackpack;
            playerControls.Dispose();
        }
    }

    public List<InventorySaveData> GetBackpackItems()
    {
        List<InventorySaveData> savedItems = new List<InventorySaveData>();
        if (backpackSlotsPanel == null)
        {
            return savedItems;
        }

        for (int i = 0; i < backpackSlotsPanel.childCount; i++)
        {
            Transform slotTransform = backpackSlotsPanel.GetChild(i);
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null || slot.currentItem == null)
            {
                continue;
            }

            Item item = slot.currentItem.GetComponent<Item>();
            if (item != null)
            {
                savedItems.Add(new InventorySaveData
                {
                    itemID = item.ID,
                    slotIndex = i,
                    quantity = ItemStack.Ensure(item.gameObject).Quantity
                });
            }
        }

        return savedItems;
    }

    public bool TryAddItem(Item worldItem)
    {
        if (worldItem == null || !IsBackpackEquipped || backpackSlotsPanel == null)
        {
            return false;
        }

        if (itemDictionary == null)
        {
            itemDictionary = FindAnyObjectByType<ItemDictionary>();
        }

        GameObject itemPrefab = itemDictionary != null
            ? itemDictionary.GetItemPrefab(worldItem.Name)
            : null;
        if (itemPrefab == null)
        {
            return false;
        }

        if (TryAddToExistingStack(itemPrefab.GetComponent<Item>()))
        {
            return true;
        }

        foreach (Transform slotTransform in backpackSlotsPanel)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null || slot.currentItem != null)
            {
                continue;
            }

            GameObject uiItem = InventoryController.CreateInventoryItem(itemPrefab, slotTransform);
            if (uiItem != null && slot.TrySetItem(uiItem))
            {
                return true;
            }

            if (uiItem != null)
            {
                Destroy(uiItem);
            }
        }

        return false;
    }

    public int CountItem(string itemName)
    {
        if (backpackSlotsPanel == null || string.IsNullOrEmpty(itemName)) return 0;
        int count = 0;
        foreach (Transform slotTransform in backpackSlotsPanel)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            Item item = slot != null && slot.currentItem != null
                ? slot.currentItem.GetComponent<Item>() : null;
            if (item != null && item.Name == itemName)
                count += item.GetComponent<ItemStack>()?.Quantity ?? 1;
        }
        return count;
    }

    public bool TryAddToExistingStack(Item prefabItem)
    {
        if (!IsBackpackEquipped || backpackSlotsPanel == null ||
            prefabItem == null || prefabItem.MaxStackSize <= 1)
        {
            return false;
        }

        foreach (Transform slotTransform in backpackSlotsPanel)
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

        return false;
    }

    public bool TryConsumeItem(string itemName)
    {
        if (!IsBackpackEquipped || backpackSlotsPanel == null || string.IsNullOrWhiteSpace(itemName))
        {
            return false;
        }

        foreach (Transform slotTransform in backpackSlotsPanel)
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

    public void SetBackpackItems(List<InventorySaveData> savedItems)
    {
        if (backpackSlotsPanel == null)
        {
            FindBackpackObjects();
        }

        if (backpackSlotsPanel == null)
        {
            Debug.LogError("Could not load backpack contents: BackpackSlotsPanel was not found in the scene.");
            return;
        }

        foreach (Slot slot in backpackSlotsPanel.GetComponentsInChildren<Slot>(true))
        {
            GameObject oldItem = slot.currentItem;
            slot.currentItem = null;
            if (oldItem != null)
            {
                Destroy(oldItem);
            }
        }

        if (savedItems == null || savedItems.Count == 0)
        {
            return;
        }

        if (itemDictionary == null)
        {
            itemDictionary = FindAnyObjectByType<ItemDictionary>();
        }

        if (itemDictionary == null)
        {
            Debug.LogError("Could not load backpack contents: no ItemDictionary was found in the scene.");
            return;
        }

        foreach (InventorySaveData savedItem in savedItems)
        {
            if (savedItem.slotIndex < 0 || savedItem.slotIndex >= backpackSlotsPanel.childCount)
            {
                continue;
            }

            Transform slotTransform = backpackSlotsPanel.GetChild(savedItem.slotIndex);
            Slot slot = slotTransform.GetComponent<Slot>();
            GameObject prefab = itemDictionary.GetItemPrefab(savedItem.itemID);
            if (slot == null || prefab == null)
            {
                continue;
            }

            GameObject uiItem = InventoryController.CreateInventoryItem(prefab, slotTransform);
            if (uiItem != null)
            {
                ItemStack.Ensure(uiItem).SetQuantity(Mathf.Max(1, savedItem.quantity));
                if (!slot.TrySetItem(uiItem))
                {
                    Destroy(uiItem);
                }
            }
        }
    }

    // B só alterna a janela quando há mochila equipada e a aba Inventário está aberta.
    private void OnToggleBackpack(InputAction.CallbackContext context)
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        if (backpackWindow == null)
        {
            FindBackpackObjects();
        }

        FindInventoryMenu();

        if (backpackWindow == null || !IsBackpackEquipped ||
            menuController == null || menuController.menuCanvas == null ||
            !menuController.menuCanvas.activeInHierarchy ||
            tabController == null || tabController.CurrentTabIndex != InventoryTabIndex)
        {
            CloseWindow();
            return;
        }

        backpackWindow.SetActive(!backpackWindow.activeSelf);
    }

    public void CloseWindow()
    {
        if (backpackWindow != null)
        {
            backpackWindow.SetActive(false);
        }
    }

    private void HandleEquipmentChanged()
    {
        if (!IsBackpackEquipped && backpackWindow != null)
        {
            backpackWindow.SetActive(false);
        }
    }

    private void HandleTabChanged(int tabIndex)
    {
        if (tabIndex != InventoryTabIndex)
        {
            CloseWindow();
        }
    }

    private void FindInventoryMenu()
    {
        if (menuController == null)
        {
            menuController = FindAnyObjectByType<MenuController>();
        }

        if (tabController == null && menuController != null && menuController.menuCanvas != null)
        {
            tabController = menuController.menuCanvas.GetComponentInChildren<TabController>(true);
        }

        if (!subscribedToTabChanges && tabController != null)
        {
            tabController.TabChanged += HandleTabChanged;
            subscribedToTabChanges = true;
        }
    }

    private void FindBackpackObjects()
    {
        Transform[] sceneTransforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include);

        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (sceneTransform.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            if (backpackWindow == null && sceneTransform.name == "BackpackWindow")
            {
                backpackWindow = sceneTransform.gameObject;
            }

            if (backpackSlotsPanel == null && sceneTransform.name == "BackpackSlotsPanel")
            {
                backpackSlotsPanel = sceneTransform;
            }

            if (backpackWindow != null && backpackSlotsPanel != null)
            {
                break;
            }
        }
    }

    // Retira a janela da hierarquia da página para que fechar o inventário não a deixe presa ao layout.
    private void DetachWindowFromInventoryMenu()
    {
        if (backpackWindow == null)
        {
            return;
        }

        MenuController menuController = FindAnyObjectByType<MenuController>();
        if (menuController == null || menuController.menuCanvas == null)
        {
            return;
        }

        Transform menuTransform = menuController.menuCanvas.transform;
        if (backpackWindow.transform == menuTransform ||
            !backpackWindow.transform.IsChildOf(menuTransform))
        {
            return;
        }

        Transform independentParent = menuTransform.parent;
        if (independentParent != null)
        {
            backpackWindow.transform.SetParent(independentParent, true);
        }
    }
}
