using UnityEngine;

public sealed class WorldItemPickup2D : Interactable2D
{
    private enum PickupKind
    {
        InventoryItem,
        Coins
    }

    [SerializeField] private PickupKind kind;
    [SerializeField] private InventoryItemId itemId = InventoryItemId.Stick;
    [SerializeField, Min(1)] private int quantity = 1;
    [SerializeField] private string displayName;
    [SerializeField, Min(0)] private int experienceReward = 5;

    public void ConfigureItem(InventoryItemId id, int amount, string itemName = null)
    {
        kind = PickupKind.InventoryItem;
        itemId = id;
        quantity = Mathf.Max(1, amount);
        displayName = string.IsNullOrWhiteSpace(itemName) ? InventoryItemCatalog.GetDisplayName(id) : itemName;
        PromptText = "Pegar " + displayName.ToLowerInvariant();
    }

    public void ConfigureCoins(int amount, string pickupName = "Bau")
    {
        kind = PickupKind.Coins;
        quantity = Mathf.Max(1, amount);
        displayName = pickupName;
        experienceReward = 15;
        PromptText = "Abrir " + displayName.ToLowerInvariant();
    }

    public override void Interact(PlayerInteractor2D interactor)
    {
        string result;
        if (kind == PickupKind.Coins)
        {
            PlayerWallet2D wallet = interactor.GetComponent<PlayerWallet2D>();
            if (wallet == null)
            {
                StartDialogue(interactor, "O jogador ainda nao tem carteira de moedas.");
                return;
            }

            wallet.AddCoins(quantity);
            result = $"Voce encontrou {quantity} moedas.";
        }
        else
        {
            PlayerInventory2D inventory = interactor.GetComponent<PlayerInventory2D>();
            if (inventory == null || !inventory.TryAddItem(itemId, quantity))
            {
                StartDialogue(interactor, "O inventario esta cheio ou nao pode receber esse item.");
                return;
            }

            result = $"Voce pegou {quantity}x {displayName}.";
        }

        PlayerProgression2D progression = interactor.GetComponent<PlayerProgression2D>();
        if (progression != null)
        {
            progression.AwardExperience(experienceReward);
        }

        StartDialogue(interactor, result, () => Destroy(gameObject));
    }

    private void StartDialogue(PlayerInteractor2D interactor, string line, System.Action finished = null)
    {
        DialogueManager2D manager = interactor.DialogueManager;
        if (manager != null)
        {
            manager.StartDialogue(displayName, new[] { line }, finished);
        }
        else
        {
            finished?.Invoke();
        }
    }

    public static WorldItemPickup2D SpawnItem(InventoryItemId id, int amount, Vector2 position, Sprite sprite, Color color)
    {
        GameObject pickupObject = CreateWorldPickup(InventoryItemCatalog.GetDisplayName(id), position, sprite, color);
        WorldItemPickup2D pickup = pickupObject.AddComponent<WorldItemPickup2D>();
        pickup.ConfigureItem(id, amount);
        return pickup;
    }

    private static GameObject CreateWorldPickup(string objectName, Vector2 position, Sprite sprite, Color color)
    {
        GameObject pickupObject = new GameObject(objectName + " - drop");
        pickupObject.transform.position = position;
        pickupObject.transform.localScale = Vector3.one * 0.55f;

        SpriteRenderer renderer = pickupObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 9;

        BoxCollider2D collider = pickupObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        return pickupObject;
    }
}
