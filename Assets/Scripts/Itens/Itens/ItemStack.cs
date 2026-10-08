using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Controla quantidades empilhadas, o limite por tipo e o número exibido sobre o ícone.
/// </summary>
public class ItemStack : MonoBehaviour
{
    public static event Action AnyQuantityChanged;
    [SerializeField, Min(1)] private int quantity = 1;

    private Item item;
    private TMP_Text quantityText;

    public int Quantity => quantity;
    public int Capacity => item != null ? item.MaxStackSize : 1;

    public static ItemStack Ensure(GameObject itemObject)
    {
        return itemObject != null
            ? itemObject.GetComponent<ItemStack>() ?? itemObject.AddComponent<ItemStack>()
            : null;
    }

    private void Awake()
    {
        item = GetComponent<Item>();
        RefreshLabel();
    }

    // Pilhas só se juntam se ambos os itens forem empilháveis e tiverem o mesmo nome canônico.
    public static bool CanCombine(Item first, Item second)
    {
        return first != null && second != null && first.MaxStackSize > 1 &&
               second.MaxStackSize > 1 && first.Name == second.Name;
    }

    // Acrescenta apenas o que cabe na pilha e emite evento para o autosave.
    public int AddUpTo(int amount)
    {
        int added = Mathf.Min(Mathf.Max(0, amount), Capacity - quantity);
        quantity += added;
        RefreshLabel();
        if (added > 0) AnyQuantityChanged?.Invoke();
        return added;
    }

    public int RemoveUpTo(int amount)
    {
        int removed = Mathf.Min(Mathf.Max(0, amount), quantity);
        quantity -= removed;
        RefreshLabel();
        if (removed > 0) AnyQuantityChanged?.Invoke();
        return removed;
    }

    public void SetQuantity(int amount)
    {
        int oldQuantity = quantity;
        quantity = Mathf.Clamp(amount, 1, Capacity);
        RefreshLabel();
        if (quantity != oldQuantity) AnyQuantityChanged?.Invoke();
    }

    private void RefreshLabel()
    {
        if (item == null)
        {
            item = GetComponent<Item>();
        }

        if (quantityText == null)
        {
            Transform existingLabel = transform.Find("QuantityText");
            quantityText = existingLabel != null ? existingLabel.GetComponent<TMP_Text>() : null;
        }

        if (quantity <= 1 || Capacity <= 1)
        {
            if (quantityText != null)
            {
                quantityText.gameObject.SetActive(false);
            }

            return;
        }

        if (quantityText == null)
        {
            GameObject labelObject = new GameObject("QuantityText", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(1f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(1f, 0f);
            labelRect.anchoredPosition = new Vector2(-2f, 2f);
            labelRect.sizeDelta = new Vector2(48f, 30f);

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.BottomRight;
            label.fontSize = 24f;
            label.color = Color.white;
            label.outlineColor = Color.black;
            label.outlineWidth = 0.25f;
            label.raycastTarget = false;
            quantityText = label;
        }

        quantityText.gameObject.SetActive(true);
        quantityText.text = quantity.ToString();
    }
}
