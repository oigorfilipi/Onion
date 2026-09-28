using UnityEngine;

public sealed class PlayerMaterialPouch2D : MonoBehaviour
{
    [SerializeField, Min(0)] private int metalScrapCount;
    private PlayerInventory2D inventory;

    public int MetalScrapCount
    {
        get
        {
            ResolveInventory();
            return inventory != null ? inventory.CountItem(InventoryItemId.MetalScrap) : metalScrapCount;
        }
    }

    private void Awake()
    {
        ResolveInventory();
        if (inventory != null && metalScrapCount > 0)
        {
            if (inventory.TryAddItem(InventoryItemId.MetalScrap, metalScrapCount))
            {
                metalScrapCount = 0;
            }
        }
    }

    public bool CanAddMetalScrap(int amount = 1)
    {
        ResolveInventory();
        return inventory != null ? inventory.CanAddItem(InventoryItemId.MetalScrap, amount) : amount > 0;
    }

    public bool AddMetalScrap(int amount = 1)
    {
        ResolveInventory();
        if (inventory != null)
        {
            return inventory.TryAddItem(InventoryItemId.MetalScrap, amount);
        }

        if (amount <= 0) return false;
        metalScrapCount += amount;
        return true;
    }

    public bool TrySpendMetalScrap(int amount = 1)
    {
        if (amount <= 0)
        {
            return true;
        }

        ResolveInventory();
        if (inventory != null)
        {
            return inventory.TryRemoveItem(InventoryItemId.MetalScrap, amount);
        }

        if (metalScrapCount < amount)
        {
            return false;
        }

        metalScrapCount -= amount;
        return true;
    }

    private void ResolveInventory()
    {
        if (inventory == null)
        {
            inventory = GetComponent<PlayerInventory2D>();
        }
    }
}
