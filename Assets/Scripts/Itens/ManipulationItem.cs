using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Aplica as regras de arrastar, trocar, juntar e consumir itens na interface do inventário.
/// </summary>
public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private IItemSlot registeredSlot;
    private IItemSlot originalSlot;
    private bool isDragging;
    private Vector3 defaultLocalScale;
    private bool hasDefaultLocalScale;

    private void Start()
    {
        CacheDefaultScale();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        RegisterInParentSlot();
    }

    private void OnTransformParentChanged()
    {
        RegisterInParentSlot();
    }

    private void RegisterInParentSlot()
    {
        IItemSlot parentSlot = FindSlotInParents(transform.parent);
        if (registeredSlot != parentSlot)
        {
            if (registeredSlot != null && registeredSlot.CurrentItem == gameObject)
            {
                registeredSlot.TryRemoveItem(gameObject);
            }

            registeredSlot = parentSlot;
        }

        if (registeredSlot == null)
        {
            return;
        }

        ItemStack.Ensure(gameObject);
        CenterInSlot(GetComponent<RectTransform>());
        if (registeredSlot.CurrentItem == null || registeredSlot.CurrentItem == gameObject)
        {
            registeredSlot.TrySetItem(gameObject);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        CacheDefaultScale();
        originalParent = transform.parent;
        originalSlot = FindSlotInParents(originalParent);

        if (originalSlot != null && (originalSlot.IsLocked || !CanRemove(originalSlot)))
        {
            originalParent = null;
            originalSlot = null;
            return;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        isDragging = true;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
        transform.SetParent(transform.root, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            transform.position = eventData.position;
        }
    }

    // Devolve o item ao local original quando o destino é inválido; nos destinos válidos, junta ou troca itens.
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        IItemSlot dropSlot = FindSlotInParents(eventData.pointerEnter != null
            ? eventData.pointerEnter.transform
            : null);
        Item item = GetComponent<Item>();

        if (dropSlot == null || dropSlot == originalSlot || item == null || !dropSlot.CanAccept(item))
        {
            RestoreToOriginalParent();
            return;
        }

        ItemStack draggedStack = ItemStack.Ensure(gameObject);
        if (dropSlot is EquipmentSlot && originalSlot is Slot && draggedStack.Quantity > 1)
        {
            if (dropSlot.CurrentItem != null)
            {
                RestoreToOriginalParent();
                return;
            }

            GameObject equippedUnit = Instantiate(gameObject, dropSlot.SlotTransform, false);
            equippedUnit.transform.localScale = defaultLocalScale;
            ItemStack.Ensure(equippedUnit).SetQuantity(1);
            CenterInSlot(equippedUnit.GetComponent<RectTransform>());
            if (!dropSlot.TrySetItem(equippedUnit))
            {
                Destroy(equippedUnit);
                RestoreToOriginalParent();
                return;
            }

            draggedStack.RemoveUpTo(1);
            RestoreToOriginalParent();
            return;
        }

        GameObject displacedItem = dropSlot.CurrentItem;
        if (displacedItem != null)
        {
            Item displacedItemData = displacedItem.GetComponent<Item>();
            if (dropSlot is Slot && ItemStack.CanCombine(item, displacedItemData))
            {
                ItemStack sourceStack = ItemStack.Ensure(gameObject);
                ItemStack targetStack = ItemStack.Ensure(displacedItem);
                int transferred = targetStack.AddUpTo(sourceStack.Quantity);
                if (transferred > 0)
                {
                    sourceStack.RemoveUpTo(transferred);
                    if (sourceStack.Quantity == 0)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        RestoreToOriginalParent();
                    }

                    return;
                }

                RestoreToOriginalParent();
                return;
            }

            if (originalSlot == null || dropSlot.IsLocked || !CanRemove(dropSlot) ||
                originalSlot.IsLocked || !CanRemove(originalSlot) ||
                !originalSlot.CanAccept(displacedItemData))
            {
                RestoreToOriginalParent();
                return;
            }
        }

        if (originalSlot != null && originalSlot.CurrentItem == gameObject &&
            !originalSlot.TryRemoveItem(gameObject))
        {
            RestoreToOriginalParent();
            return;
        }

        if (originalSlot != null && originalSlot.CurrentItem != null &&
            originalSlot.CurrentItem != gameObject)
        {
            RestoreToOriginalParent();
            return;
        }

        if (displacedItem != null && !dropSlot.TryRemoveItem(displacedItem))
        {
            if (originalSlot != null)
            {
                originalSlot.TrySetItem(gameObject);
            }

            RestoreToOriginalParent();
            return;
        }

        transform.SetParent(dropSlot.SlotTransform, false);
        RestoreDefaultScale();
        CenterInSlot(GetComponent<RectTransform>());
        dropSlot.TrySetItem(gameObject);

        if (displacedItem != null && originalSlot != null)
        {
            displacedItem.transform.SetParent(originalSlot.SlotTransform, false);
            displacedItem.GetComponent<ItemDragHandler>()?.RestoreDefaultScale();
            CenterInSlot(displacedItem.GetComponent<RectTransform>());
            originalSlot.TrySetItem(displacedItem);
        }

        originalParent = null;
        originalSlot = null;
    }

    // Clique direito consome uma cura de um slot comum somente quando o jogador pode recuperar vida.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging || eventData.button != PointerEventData.InputButton.Right)
        {
            return;
        }

        Item item = GetComponent<Item>();
        IItemSlot slot = FindSlotInParents(transform.parent);
        PlayerVitals playerVitals = FindAnyObjectByType<PlayerVitals>();
        if (item == null || item.healAmount <= 0 || slot is not Slot ||
            slot.CurrentItem != gameObject || playerVitals == null ||
            playerVitals.IsDead || playerVitals.CurrentHealth >= playerVitals.MaxHealth)
        {
            return;
        }

        ItemStack stack = ItemStack.Ensure(gameObject);
        if (stack.Quantity > 1)
        {
            if (playerVitals.Heal(item.healAmount))
            {
                stack.RemoveUpTo(1);
            }

            return;
        }

        if (!slot.TryRemoveItem(gameObject))
        {
            return;
        }

        if (playerVitals.Heal(item.healAmount))
        {
            Destroy(gameObject);
        }
        else
        {
            slot.TrySetItem(gameObject);
        }
    }

    private void RestoreToOriginalParent()
    {
        if (originalParent == null)
        {
            return;
        }

        transform.SetParent(originalParent, false);
        RestoreDefaultScale();
        CenterInSlot(GetComponent<RectTransform>());

        if (originalSlot != null &&
            (originalSlot.CurrentItem == null || originalSlot.CurrentItem == gameObject))
        {
            originalSlot.TrySetItem(gameObject);
        }

        originalParent = null;
        originalSlot = null;
    }

    private void CacheDefaultScale()
    {
        if (hasDefaultLocalScale)
        {
            return;
        }

        defaultLocalScale = transform.localScale;
        hasDefaultLocalScale = true;
    }

    private void RestoreDefaultScale()
    {
        CacheDefaultScale();
        transform.localScale = defaultLocalScale;
    }

    private static bool CanRemove(IItemSlot slot)
    {
        return slot is not EquipmentSlot equipmentSlot || equipmentSlot.CanRemoveItem;
    }

    private static IItemSlot FindSlotInParents(Transform start)
    {
        Transform current = start;
        while (current != null)
        {
            // Equipment slots also have the regular Slot component from the shared slot prefab.
            EquipmentSlot equipmentSlot = current.GetComponent<EquipmentSlot>();
            if (equipmentSlot != null)
            {
                return equipmentSlot;
            }

            Slot inventorySlot = current.GetComponent<Slot>();
            if (inventorySlot != null)
            {
                return inventorySlot;
            }

            current = current.parent;
        }

        return null;
    }

    private static void CenterInSlot(RectTransform itemRect)
    {
        if (itemRect == null)
        {
            return;
        }

        itemRect.anchorMin = new Vector2(0.5f, 0.5f);
        itemRect.anchorMax = new Vector2(0.5f, 0.5f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.anchoredPosition = Vector2.zero;
    }
}
