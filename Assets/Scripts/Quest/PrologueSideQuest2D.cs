using UnityEngine;

public sealed class PrologueSideQuest2D : Interactable2D
{
    [SerializeField] private string npcName = "Estranho";
    [SerializeField] private DialogueManager2D dialogueManager;
    [SerializeField] private InventoryItemId requestedItem = InventoryItemId.Stick;
    [SerializeField, Min(1)] private int requestedQuantity = 2;
    [SerializeField] private string requestDescription = "2 gravetos";
    [SerializeField, Min(1)] private int arrowReward = 10;
    [SerializeField, Min(0)] private int experienceReward = 40;

    private bool questStarted;
    private bool rewardClaimed;
    private PlayerInventory2D playerInventory;

    public bool IsTracking => questStarted && !rewardClaimed;

    public string ObjectiveText
    {
        get
        {
            if (!IsTracking)
            {
                return string.Empty;
            }

            int collected = playerInventory != null ? Mathf.Min(playerInventory.CountItem(requestedItem), requestedQuantity) : 0;
            return $"Entregue {requestDescription} ao estranho ({collected}/{requestedQuantity}).";
        }
    }

    public void ConfigureRequest(InventoryItemId itemId, int amount, string description)
    {
        requestedItem = itemId;
        requestedQuantity = Mathf.Max(1, amount);
        requestDescription = string.IsNullOrWhiteSpace(description)
            ? $"{requestedQuantity} {InventoryItemCatalog.GetDisplayName(itemId).ToLowerInvariant()}"
            : description;
    }

    public void SetPlayerInventory(PlayerInventory2D inventory)
    {
        playerInventory = inventory;
    }

    public override void Interact(PlayerInteractor2D interactor)
    {
        PlayerInventory2D interactorInventory = interactor.GetComponent<PlayerInventory2D>();
        if (interactorInventory != null)
        {
            playerInventory = interactorInventory;
        }

        DialogueManager2D dialogue = dialogueManager != null ? dialogueManager : interactor.DialogueManager;
        if (dialogue == null)
        {
            Debug.LogWarning("O estranho precisa de um DialogueManager2D.", this);
            return;
        }

        if (rewardClaimed)
        {
            dialogue.StartDialogue(npcName, new[] { "Boa sorte com o arco. As flechas podem ser usadas uma de cada vez." });
            return;
        }

        if (!questStarted)
        {
            dialogue.StartDialogue(
                npcName,
                new[]
                {
                    "Ei, preciso de madeira para terminar um arco.",
                    $"Traga {requestDescription} e eu te dou um arco de madeira com {arrowReward} flechas."
                },
                () => questStarted = true);
            return;
        }

        PlayerInventory2D inventory = interactor.GetComponent<PlayerInventory2D>();
        if (inventory == null)
        {
            dialogue.StartDialogue(npcName, new[] { "Voce precisa de uma mochila para carregar os gravetos e a recompensa." });
            return;
        }

        if (inventory.CountItem(requestedItem) < requestedQuantity)
        {
            dialogue.StartDialogue(npcName, new[] { $"Ainda preciso de {requestDescription}. Volte quando tiver todos." });
            return;
        }

        CompleteQuest(inventory, dialogue, interactor.GetComponent<PlayerProgression2D>());
    }

    private void CompleteQuest(PlayerInventory2D inventory, DialogueManager2D dialogue, PlayerProgression2D progression)
    {
        if (!inventory.TryRemoveItem(requestedItem, requestedQuantity))
        {
            dialogue.StartDialogue(npcName, new[] { "Nao consegui receber os materiais. Confira sua mochila e tente de novo." });
            return;
        }

        bool hasRoomForBow = inventory.CanAddItem(InventoryItemId.Bow);
        bool hasRoomForArrows = inventory.CanAddItem(InventoryItemId.Arrow, arrowReward);
        if (!hasRoomForBow || !hasRoomForArrows)
        {
            inventory.TryAddItem(requestedItem, requestedQuantity);
            dialogue.StartDialogue(npcName, new[] { "Preciso de espaco livre para te entregar o arco e as flechas. Libere a mochila e volte." });
            return;
        }

        if (!inventory.TryAddItem(InventoryItemId.Bow))
        {
            inventory.TryAddItem(requestedItem, requestedQuantity);
            dialogue.StartDialogue(npcName, new[] { "Algo deu errado ao separar a recompensa. Tente falar comigo novamente." });
            return;
        }

        if (!inventory.TryAddItem(InventoryItemId.Arrow, arrowReward))
        {
            inventory.TryRemoveItem(InventoryItemId.Bow);
            inventory.TryAddItem(requestedItem, requestedQuantity);
            dialogue.StartDialogue(npcName, new[] { "Libere mais espaco para eu te entregar as flechas junto com o arco." });
            return;
        }

        rewardClaimed = true;
        if (progression != null)
        {
            progression.AwardExperience(experienceReward);
        }
        dialogue.StartDialogue(npcName, new[] { $"Combinado. Aqui esta o arco e {arrowReward} flechas. Obrigado pela madeira." });
    }

}
