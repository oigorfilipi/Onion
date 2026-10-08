using System;
using UnityEngine;

/// <summary>Contrato comum para arrastar itens entre inventário, mochila e equipamentos.</summary>
public interface IItemSlot
{
    Transform SlotTransform { get; }
    GameObject CurrentItem { get; }
    bool IsLocked { get; }
    bool CanAccept(Item item);
    bool TrySetItem(GameObject itemObject);
    bool TryRemoveItem(GameObject itemObject);
}

/// <summary>
/// Registra o item de um slot comum e emite o evento que permite salvar mudanças de inventário.
/// </summary>
public class Slot : MonoBehaviour, IItemSlot
{
    public static event Action AnySlotChanged;
    public GameObject currentItem;

    public Transform SlotTransform => transform;
    public GameObject CurrentItem => currentItem;
    public bool IsLocked => false;

    public bool CanAccept(Item item)
    {
        return item != null;
    }

    public bool TrySetItem(GameObject itemObject)
    {
        if (itemObject == null || !CanAccept(itemObject.GetComponent<Item>()))
        {
            return false;
        }

        if (currentItem != null && currentItem != itemObject)
        {
            return false;
        }

        bool changed = currentItem != itemObject;
        currentItem = itemObject;
        if (changed) AnySlotChanged?.Invoke();
        return true;
    }

    public bool TryRemoveItem(GameObject itemObject)
    {
        if (currentItem != itemObject)
        {
            return false;
        }

        currentItem = null;
        AnySlotChanged?.Invoke();
        return true;
    }
}
