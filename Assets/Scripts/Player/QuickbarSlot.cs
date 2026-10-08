using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Um dos dez espaços da barra rápida; aceita arrastar itens e ativa consumíveis com duplo clique.</summary>
public class QuickbarSlot : MonoBehaviour, IItemSlot, IPointerClickHandler
{
    [SerializeField] private Image background;
    private GameObject currentItem;
    private float lastClickTime = -1f;
    private QuickbarController owner;

    public Transform SlotTransform => transform;
    public GameObject CurrentItem => currentItem;
    public bool IsLocked => false;

    public void Initialize(QuickbarController controller, Image slotBackground)
    {
        owner = controller;
        background = slotBackground;
    }

    public bool CanAccept(Item item) => item != null;

    public bool TrySetItem(GameObject itemObject)
    {
        if (itemObject == null || !CanAccept(itemObject.GetComponent<Item>()) ||
            (currentItem != null && currentItem != itemObject)) return false;
        currentItem = itemObject;
        RectTransform rect = itemObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }
        ItemDragHandler dragHandler = itemObject.GetComponent<ItemDragHandler>();
        if (dragHandler != null) dragHandler.SetQuickbarVisualScale(0.72f);
        QuickbarController.NotifySlotChanged();
        return true;
    }

    public bool TryRemoveItem(GameObject itemObject)
    {
        if (currentItem != itemObject) return false;
        currentItem = null;
        QuickbarController.NotifySlotChanged();
        return true;
    }

    public void SelectFromItem() => owner?.Select(this);

    public void OnPointerClick(PointerEventData eventData)
    {
        owner?.Select(this);
        if (currentItem == null) return;
        if (Time.unscaledTime - lastClickTime <= 0.35f)
            ItemUseService.TryUse(currentItem, this);
        lastClickTime = Time.unscaledTime;
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? new Color(1f, 0.72f, 0.3f, 1f) : Color.white;
    }
}
