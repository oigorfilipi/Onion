using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InventoryHUD2D : MonoBehaviour
{
    [SerializeField] private PlayerInventory2D inventory;
    [SerializeField] private PlayerWallet2D wallet;
    [SerializeField] private PlayerVitals vitals;
    [SerializeField] private PlayerEquipment2D equipment;
    [SerializeField] private DialogueManager2D dialogueManager;

    private int selectedSlot = -1;
    private string statusMessage = "Aperte I para fechar.";
    private GUIStyle slotStyle;
    private GUIStyle selectedSlotStyle;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || inventory == null)
        {
            return;
        }

        bool dialogueOpen = dialogueManager != null && dialogueManager.BlocksWorldInput;
        if (keyboard.escapeKey.wasPressedThisFrame && inventory.IsOpen)
        {
            inventory.SetOpen(false);
            return;
        }

        if (dialogueOpen)
        {
            return;
        }

        if (keyboard.iKey.wasPressedThisFrame)
        {
            inventory.SetOpen(!inventory.IsOpen);
        }

        if (inventory.IsOpen && keyboard.enterKey.wasPressedThisFrame)
        {
            if (selectedSlot >= 0 && inventory.IsConsumableAt(selectedSlot))
            {
                UseSelectedItem();
            }
            else
            {
                EquipSelectedItem();
            }
        }
    }

    private void OnGUI()
    {
        if (inventory == null || !inventory.IsOpen)
        {
            return;
        }

        const float panelWidth = 560f;
        const float panelHeight = 500f;
        float left = (Screen.width - panelWidth) * 0.5f;
        float top = (Screen.height - panelHeight) * 0.5f;
        GUI.Box(new Rect(left, top, panelWidth, panelHeight), GUIContent.none);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        GUI.Label(new Rect(left + 18f, top + 10f, 330f, 32f), "Inventario - 40 espacos", titleStyle);
        int coinCount = wallet != null ? wallet.Coins : 0;
        GUI.Label(new Rect(left + 370f, top + 15f, 170f, 26f), $"Moedas: {coinCount}");

        DrawEquipmentButton(new Rect(left + 18f, top + 45f, 126f, 30f), "Arma", EquipmentSlot2D.Weapon);
        DrawEquipmentButton(new Rect(left + 150f, top + 45f, 126f, 30f), "Peitoral", EquipmentSlot2D.Chest);
        DrawEquipmentButton(new Rect(left + 282f, top + 45f, 126f, 30f), "Capacete", EquipmentSlot2D.Helmet);
        DrawEquipmentButton(new Rect(left + 414f, top + 45f, 126f, 30f), "Botas", EquipmentSlot2D.Boots);

        EnsureStyles();
        const int columns = 8;
        const float cellWidth = 62f;
        const float cellHeight = 49f;
        const float gap = 4f;
        float gridLeft = left + 18f;
        float gridTop = top + 82f;

        for (int i = 0; i < PlayerInventory2D.RequiredSlotCount; i++)
        {
            int row = i / columns;
            int column = i % columns;
            Rect cell = new Rect(gridLeft + column * (cellWidth + gap), gridTop + row * (cellHeight + gap), cellWidth, cellHeight);
            InventorySlot2D slot = inventory.Slots[i];
            string label = slot.IsEmpty
                ? $"{i + 1}\n-"
                : $"{i + 1}\n{InventoryItemCatalog.GetDisplayName(slot.itemId)}" +
                  (InventoryItemCatalog.IsStackable(slot.itemId) ? $" x{slot.quantity}" : string.Empty);

            if (GUI.Button(cell, label, selectedSlot == i ? selectedSlotStyle : slotStyle))
            {
                selectedSlot = i;
                statusMessage = slot.IsEmpty
                    ? "Espaco vazio."
                    : $"Selecionado: {InventoryItemCatalog.GetDisplayName(slot.itemId)}.";
            }
        }

        float footerTop = gridTop + 5f * (cellHeight + gap) + 2f;
        string selectedName = selectedSlot >= 0 && selectedSlot < inventory.Slots.Count && !inventory.Slots[selectedSlot].IsEmpty
            ? InventoryItemCatalog.GetDisplayName(inventory.Slots[selectedSlot].itemId)
            : "Nenhum item selecionado";
        GUI.Label(new Rect(left + 18f, footerTop, 325f, 24f), selectedName);

        bool canUse = selectedSlot >= 0 && inventory.IsConsumableAt(selectedSlot);
        GUI.enabled = canUse;
        if (GUI.Button(new Rect(left + 355f, footerTop - 2f, 82f, 30f), "Usar"))
        {
            UseSelectedItem();
        }

        GUI.enabled = equipment != null && selectedSlot >= 0 && equipment.CanEquipItem(inventory.GetItemAt(selectedSlot));
        if (GUI.Button(new Rect(left + 443f, footerTop - 2f, 97f, 30f), "Equipar"))
        {
            EquipSelectedItem();
        }
        GUI.enabled = true;

        GUI.Label(new Rect(left + 18f, footerTop + 31f, panelWidth - 36f, 26f), statusMessage);
        GUI.Label(new Rect(left + 18f, top + panelHeight - 27f, panelWidth - 36f, 20f), "I/Esc: fechar | Clique no equipamento para desequipar | Enter: usar/equipar");
    }

    private void UseSelectedItem()
    {
        if (inventory == null || vitals == null)
        {
            statusMessage = "O inventario ou os recursos do jogador nao estao configurados.";
            return;
        }

        if (inventory.TryUseItemAt(selectedSlot, vitals, out string result))
        {
            statusMessage = result;
        }
        else
        {
            statusMessage = result;
        }
    }

    private void EquipSelectedItem()
    {
        if (equipment == null)
        {
            statusMessage = "O equipamento do jogador nao esta configurado.";
            return;
        }

        equipment.TryEquipSelected(inventory, selectedSlot, out statusMessage);
    }

    private void DrawEquipmentButton(Rect rect, string slotName, EquipmentSlot2D slot)
    {
        InventoryItemId equippedItem = equipment != null ? equipment.GetEquippedItem(slot) : InventoryItemId.None;
        string itemName = equippedItem == InventoryItemId.None ? "vazio" : InventoryItemCatalog.GetDisplayName(equippedItem);
        GUI.enabled = equipment != null && equippedItem != InventoryItemId.None;
        if (GUI.Button(rect, $"{slotName}: {itemName}"))
        {
            equipment.TryUnequip(slot, inventory, out statusMessage);
        }
        GUI.enabled = true;
    }

    private void ResolveReferences()
    {
        if (inventory != null && wallet != null && vitals != null && equipment != null && dialogueManager != null)
        {
            return;
        }

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            return;
        }

        if (inventory == null) inventory = player.GetComponent<PlayerInventory2D>();
        if (wallet == null) wallet = player.GetComponent<PlayerWallet2D>();
        if (vitals == null) vitals = player.GetComponent<PlayerVitals>();
        if (equipment == null) equipment = player.GetComponent<PlayerEquipment2D>();
        if (dialogueManager == null) dialogueManager = Object.FindFirstObjectByType<DialogueManager2D>();
    }

    private void EnsureStyles()
    {
        if (slotStyle == null)
        {
            slotStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            selectedSlotStyle = new GUIStyle(slotStyle)
            {
                fontStyle = FontStyle.Bold
            };
            selectedSlotStyle.normal.textColor = Color.yellow;
        }
    }
}
