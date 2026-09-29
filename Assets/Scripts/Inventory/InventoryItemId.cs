public enum InventoryItemId
{
    None,
    Stick,
    MetalScrap,
    Bread,
    Cheese,
    HumanFinger,
    Sword,
    Bow,
    Arrow,
    ChestArmor,
    Helmet,
    MetalBoots,
    Shield,
    Backpack
}

public static class InventoryItemCatalog
{
    public static string GetDisplayName(InventoryItemId itemId)
    {
        switch (itemId)
        {
            case InventoryItemId.Stick: return "Graveto";
            case InventoryItemId.MetalScrap: return "Peca de metal";
            case InventoryItemId.Bread: return "Pao";
            case InventoryItemId.Cheese: return "Queijo";
            case InventoryItemId.HumanFinger: return "Dedo humano";
            case InventoryItemId.Sword: return "Espada";
            case InventoryItemId.Bow: return "Arco";
            case InventoryItemId.Arrow: return "Flecha";
            case InventoryItemId.ChestArmor: return "Peitoral de metal";
            case InventoryItemId.Helmet: return "Capacete de metal";
            case InventoryItemId.MetalBoots: return "Bota de metal";
            case InventoryItemId.Shield: return "Escudo de metal";
            case InventoryItemId.Backpack: return "Mochila";
            default: return "Vazio";
        }
    }

    public static bool IsConsumable(InventoryItemId itemId)
    {
        return itemId == InventoryItemId.Bread || itemId == InventoryItemId.Cheese;
    }

    public static bool IsStackable(InventoryItemId itemId)
    {
        return itemId == InventoryItemId.Stick ||
               itemId == InventoryItemId.MetalScrap ||
               itemId == InventoryItemId.Bread ||
               itemId == InventoryItemId.Cheese ||
               itemId == InventoryItemId.Arrow;
    }
}
