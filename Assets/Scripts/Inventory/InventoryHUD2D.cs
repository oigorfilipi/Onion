using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class InventoryHUD2D : MonoBehaviour
{
    private enum GameplayPage
    {
        Status,
        Map,
        Skills,
        SkillTree,
        Upgrades,
        Missions
    }

    [SerializeField] private PlayerInventory2D inventory;
    [SerializeField] private PlayerWallet2D wallet;
    [SerializeField] private PlayerVitals vitals;
    [SerializeField] private PlayerEquipment2D equipment;
    [SerializeField] private PlayerPowerLoadout2D powerLoadout;
    [SerializeField] private PlayerProgression2D progression;
    [SerializeField] private DialogueManager2D dialogueManager;
    [SerializeField] private PrologueQuest mainQuest;
    [SerializeField] private PrologueSideQuest2D sideQuest;

    private int selectedSlot = -1;
    private string statusMessage = "Aperte E para fechar.";
    private bool gameplayMenuOpen;
    private bool pauseMenuOpen;
    private bool showOptions;
    private GameplayPage currentPage = GameplayPage.Status;
    private GUIStyle slotStyle;
    private GUIStyle selectedSlotStyle;
    private GUIStyle equipmentButtonStyle;

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

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            HandleEscape();
            return;
        }

        if (pauseMenuOpen)
        {
            return;
        }

        bool dialogueOpen = dialogueManager != null && dialogueManager.BlocksWorldInput;
        if (dialogueOpen)
        {
            return;
        }

        if (keyboard.mKey.wasPressedThisFrame) OpenGameplayPage(GameplayPage.Map);
        else if (keyboard.nKey.wasPressedThisFrame) OpenGameplayPage(GameplayPage.Missions);
        else if (keyboard.hKey.wasPressedThisFrame) OpenGameplayPage(GameplayPage.Upgrades);
        else if (keyboard.kKey.wasPressedThisFrame) OpenGameplayPage(GameplayPage.SkillTree);

        if (gameplayMenuOpen)
        {
            if (keyboard.iKey.wasPressedThisFrame)
            {
                CloseGameplayMenu();
            }
            return;
        }

        if (keyboard.iKey.wasPressedThisFrame)
        {
            OpenGameplayPage(GameplayPage.Status);
            return;
        }

        if (keyboard.eKey.wasPressedThisFrame)
        {
            inventory.SetOpen(!inventory.IsOpen);
            statusMessage = "Aperte E para fechar.";
        }

        if (keyboard.bKey.wasPressedThisFrame && equipment != null && equipment.HasBackpack)
        {
            inventory.SetOpen(true);
            statusMessage = "Mochila aberta.";
        }

        if (!inventory.IsOpen)
        {
            return;
        }

        if (keyboard.enterKey.wasPressedThisFrame)
        {
            if (selectedSlot >= 0 && inventory.IsConsumableAt(selectedSlot))
            {
                UseSelectedItem();
            }
            else
            {
                EquipmentSlot2D? targetSlot = PlayerEquipment2D.GetSlotFor(inventory.GetItemAt(selectedSlot));
                if (targetSlot.HasValue)
                {
                    EquipSelectedItem(targetSlot.Value);
                }
            }
        }
    }

    private void HandleEscape()
    {
        if (inventory != null && inventory.IsOpen)
        {
            inventory.SetOpen(false);
            return;
        }

        if (gameplayMenuOpen)
        {
            CloseGameplayMenu();
            return;
        }

        if (pauseMenuOpen)
        {
            if (showOptions)
            {
                showOptions = false;
            }
            else
            {
                ClosePauseMenu();
            }
            return;
        }

        pauseMenuOpen = true;
        showOptions = false;
        Time.timeScale = 0f;
    }

    private void OpenGameplayPage(GameplayPage page)
    {
        if (inventory != null)
        {
            inventory.SetOpen(false);
        }

        currentPage = page;
        gameplayMenuOpen = true;
        pauseMenuOpen = false;
        showOptions = false;
        Time.timeScale = 0f;
    }

    private void CloseGameplayMenu()
    {
        gameplayMenuOpen = false;
        Time.timeScale = 1f;
    }

    private void ClosePauseMenu()
    {
        pauseMenuOpen = false;
        showOptions = false;
        Time.timeScale = 1f;
    }

    private void OnGUI()
    {
        if (inventory != null && inventory.IsOpen)
        {
            DrawInventory();
            return;
        }

        if (gameplayMenuOpen)
        {
            DrawGameplayMenu();
        }
        else if (pauseMenuOpen)
        {
            DrawPauseMenu();
        }
    }

    private void DrawInventory()
    {
        int rows = inventory.SlotCount / 10;
        float panelWidth = Mathf.Min(1080f, Screen.width - 40f);
        float panelHeight = rows > 4 ? 580f : 500f;
        float left = (Screen.width - panelWidth) * 0.5f;
        float top = (Screen.height - panelHeight) * 0.5f;
        GUI.Box(new Rect(left, top, panelWidth, panelHeight), GUIContent.none);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        GUI.Label(new Rect(left + 18f, top + 10f, 360f, 32f), $"Inventario - {inventory.SlotCount} espacos", titleStyle);
        int coinCount = wallet != null ? wallet.Coins : 0;
        GUI.Label(new Rect(left + panelWidth - 188f, top + 15f, 170f, 26f), $"Moedas: {coinCount}");
        EnsureStyles();

        const int columns = 10;
        const float cellHeight = 60f;
        const float gap = 4f;
        float gridLeft = left + 270f;
        float gridWidth = panelWidth - 288f;
        float cellWidth = (gridWidth - gap * (columns - 1)) / columns;
        float gridTop = top + 82f;

        EquipmentSlot2D[,] slotsByRow =
        {
            { EquipmentSlot2D.PrimaryWeapon, EquipmentSlot2D.SecondaryWeapon },
            { EquipmentSlot2D.Shield, EquipmentSlot2D.Helmet },
            { EquipmentSlot2D.Chest, EquipmentSlot2D.Boots },
            { EquipmentSlot2D.Contact, EquipmentSlot2D.Backpack }
        };
        string[,] equipmentLabels =
        {
            { "Primaria", "Secundaria" },
            { "Escudo", "Capacete" },
            { "Peitoral", "Botas" },
            { "Contato", "Mochila" }
        };
        const float equipmentGap = 4f;
        float equipmentColumnWidth = 234f;
        float equipmentButtonWidth = (equipmentColumnWidth - equipmentGap) / 2f;
        for (int row = 0; row < 4; row++)
        {
            float y = gridTop + row * (cellHeight + gap);
            for (int column = 0; column < 2; column++)
            {
                float x = left + 18f + column * (equipmentButtonWidth + equipmentGap);
                Rect buttonRect = new Rect(x, y, equipmentButtonWidth, cellHeight);
                EquipmentSlot2D slot = slotsByRow[row, column];
                if (slot == EquipmentSlot2D.Contact)
                {
                    DrawContactPowerButton(buttonRect);
                }
                else
                {
                    DrawEquipmentButton(buttonRect, equipmentLabels[row, column], slot);
                }
            }
        }

        for (int i = 0; i < inventory.SlotCount; i++)
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

        float footerTop = gridTop + rows * (cellHeight + gap) + 2f;
        string selectedName = selectedSlot >= 0 && selectedSlot < inventory.SlotCount && !inventory.Slots[selectedSlot].IsEmpty
            ? InventoryItemCatalog.GetDisplayName(inventory.Slots[selectedSlot].itemId)
            : "Nenhum item selecionado";
        GUI.Label(new Rect(gridLeft, footerTop, gridWidth - 280f, 24f), selectedName);

        bool canUse = selectedSlot >= 0 && inventory.IsConsumableAt(selectedSlot);
        float actionLeft = gridLeft + gridWidth - 262f;
        GUI.enabled = canUse;
        if (GUI.Button(new Rect(actionLeft, footerTop - 2f, 70f, 30f), "Usar"))
        {
            UseSelectedItem();
        }

        bool selectedWeapon = selectedSlot >= 0 &&
            (inventory.GetItemAt(selectedSlot) == InventoryItemId.Sword || inventory.GetItemAt(selectedSlot) == InventoryItemId.Bow);
        GUI.enabled = equipment != null && selectedSlot >= 0 && equipment.CanEquipItem(inventory.GetItemAt(selectedSlot));
        if (GUI.Button(new Rect(actionLeft + 74f, footerTop - 2f, 86f, 30f), selectedWeapon ? "Primaria" : "Equipar"))
        {
            EquipSelectedItem(selectedWeapon ? EquipmentSlot2D.PrimaryWeapon : PlayerEquipment2D.GetSlotFor(inventory.GetItemAt(selectedSlot)).Value);
        }

        GUI.enabled = equipment != null && selectedWeapon;
        if (GUI.Button(new Rect(actionLeft + 164f, footerTop - 2f, 86f, 30f), "Secundaria"))
        {
            EquipSelectedItem(EquipmentSlot2D.SecondaryWeapon);
        }
        GUI.enabled = true;

        GUI.Label(new Rect(gridLeft, footerTop + 31f, gridWidth, 26f), statusMessage);
        GUI.Label(new Rect(left + 18f, top + panelHeight - 27f, panelWidth - 36f, 20f), "E/Esc: fechar | B: mochila equipada | Tab: trocar armas | Enter: usar/equipar");
    }

    private void DrawGameplayMenu()
    {
        DrawDimBackground();
        float panelWidth = Mathf.Min(980f, Screen.width - 40f);
        float panelHeight = Mathf.Min(680f, Screen.height - 40f);
        float left = (Screen.width - panelWidth) * 0.5f;
        float top = (Screen.height - panelHeight) * 0.5f;
        GUI.Box(new Rect(left, top, panelWidth, panelHeight), GUIContent.none);
        GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
        GUI.Label(new Rect(left + 28f, top + 18f, panelWidth - 56f, 42f), "Menu de gameplay", title);

        GameplayPage[] pages = { GameplayPage.Status, GameplayPage.Map, GameplayPage.Skills, GameplayPage.SkillTree, GameplayPage.Upgrades, GameplayPage.Missions };
        string[] labels = { "Status", "Mapa", "Skills", "Arvore", "Melhorias", "Missoes" };
        float tabGap = 6f;
        float tabWidth = (panelWidth - 56f - tabGap * (pages.Length - 1)) / pages.Length;
        for (int i = 0; i < pages.Length; i++)
        {
            Rect tabRect = new Rect(left + 28f + i * (tabWidth + tabGap), top + 70f, tabWidth, 36f);
            if (GUI.Toggle(tabRect, currentPage == pages[i], labels[i], GUI.skin.button))
            {
                currentPage = pages[i];
            }
        }

        Rect content = new Rect(left + 34f, top + 124f, panelWidth - 68f, panelHeight - 170f);
        GUI.Box(content, GUIContent.none);
        DrawGameplayPage(content);
        GUI.Label(new Rect(left + 28f, top + panelHeight - 34f, panelWidth - 56f, 24f), "I/Esc: fechar | M: mapa | N: missoes | H: melhorias | K: arvore de habilidades");
    }

    private void DrawGameplayPage(Rect content)
    {
        GUIStyle heading = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold };
        GUIStyle body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
        float y = content.y + 18f;

        switch (currentPage)
        {
            case GameplayPage.Status:
                GUI.Label(new Rect(content.x + 22f, y, content.width - 44f, 32f), "Status do personagem", heading);
                y += 48f;
                GUI.Label(new Rect(content.x + 22f, y, content.width - 44f, 30f),
                    progression != null ? $"Nivel: {progression.Level}" : "Nivel: indisponivel", body);
                y += 34f;
                GUI.Label(new Rect(content.x + 22f, y, content.width - 44f, 30f),
                    vitals != null ? $"Vida: {vitals.CurrentHealth}/{vitals.MaxHealth}    Estamina: {vitals.CurrentEnergy}/{vitals.MaxEnergy}" : "Recursos indisponiveis", body);
                y += 42f;
                string primary = equipment != null ? InventoryItemCatalog.GetDisplayName(equipment.EquippedPrimaryWeapon) : "Vazio";
                string secondary = equipment != null ? InventoryItemCatalog.GetDisplayName(equipment.EquippedSecondaryWeapon) : "Vazio";
                GUI.Label(new Rect(content.x + 22f, y, content.width - 44f, 30f), $"Arma primaria: {primary}    Arma secundaria: {secondary}", body);
                y += 34f;
                GUI.Label(new Rect(content.x + 22f, y, content.width - 44f, 30f),
                    $"Peitoral: {EquipmentName(EquipmentSlot2D.Chest)}    Capacete: {EquipmentName(EquipmentSlot2D.Helmet)}    Botas: {EquipmentName(EquipmentSlot2D.Boots)}", body);
                y += 42f;
                GUI.Label(new Rect(content.x + 22f, y, content.width - 44f, 30f),
                    $"Escudo: {EquipmentName(EquipmentSlot2D.Shield)}    Mochila: {EquipmentName(EquipmentSlot2D.Backpack)}", body);
                y += 34f;
                string contact = powerLoadout != null ? powerLoadout.EquippedContactPower.ToString() : "Nenhum";
                string absorption = powerLoadout != null ? powerLoadout.ActiveAbsorptionPower.ToString() : "Nenhum";
                GUI.Label(new Rect(content.x + 22f, y, content.width - 44f, 30f), $"Poder de contato: {contact}    Poder de absorcao: {absorption}", body);
                if (progression != null && !progression.IsAtMaximumLevel)
                {
                    y += 36f;
                    GUI.Label(new Rect(content.x + 22f, y, content.width - 44f, 30f),
                        $"Experiencia: {progression.CurrentExperience}/{progression.ExperienceRequiredForNextLevel}", body);
                }
                break;

            case GameplayPage.Missions:
                GUI.Label(new Rect(content.x + 22f, y, content.width - 44f, 32f), "Missoes", heading);
                y += 48f;
                GUI.Label(new Rect(content.x + 22f, y, content.width - 44f, 54f),
                    mainQuest != null ? "Principal: " + mainQuest.ObjectiveText : "Principal: nenhuma missao ativa.", body);
                y += 66f;
                GUI.Label(new Rect(content.x + 22f, y, content.width - 44f, 70f),
                    sideQuest != null && sideQuest.IsTracking ? "Secundaria: " + sideQuest.ObjectiveText : "Secundaria: nenhuma missao em destaque.", body);
                y += 84f;
                GUI.Label(new Rect(content.x + 22f, y, content.width - 44f, 34f), "A lista de missoes concluídas sera ampliada junto com o sistema de missoes.", body);
                break;

            case GameplayPage.Map:
                DrawDemonstrationPage(content, heading, body, "Mapa em tela cheia", "A tela do mapa esta montada como demonstracao. A organizacao das regioes sera definida na etapa de mapa.");
                break;
            case GameplayPage.Skills:
                DrawDemonstrationPage(content, heading, body, "Skills", "Esta aba demonstra onde ficarao as habilidades dos poderes. O desbloqueio continua sem efeito nesta versao.");
                break;
            case GameplayPage.SkillTree:
                DrawDemonstrationPage(content, heading, body, "Arvore de habilidades", "Esta aba demonstra a arvore de progressao do personagem. Os atributos ainda nao podem ser aprimorados por aqui.");
                break;
            case GameplayPage.Upgrades:
                DrawDemonstrationPage(content, heading, body, "Melhorias de armas e ferramentas", "Esta aba demonstra o espaco de melhorias. O sistema de aprimoramento sera implementado em outra etapa.");
                break;
        }
    }

    private void DrawPauseMenu()
    {
        DrawDimBackground();
        const float width = 470f;
        const float height = 390f;
        float left = (Screen.width - width) * 0.5f;
        float top = (Screen.height - height) * 0.5f;
        GUI.Box(new Rect(left, top, width, height), GUIContent.none);
        GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(left + 24f, top + 24f, width - 48f, 44f), showOptions ? "Opcoes" : "Jogo pausado", title);

        float buttonWidth = width - 100f;
        float buttonLeft = left + 50f;
        if (showOptions)
        {
            GUI.Label(new Rect(buttonLeft, top + 110f, buttonWidth, 72f), "Opcoes de audio e video serao adicionadas em uma etapa futura.",
                new GUIStyle(GUI.skin.label) { wordWrap = true, alignment = TextAnchor.MiddleCenter, fontSize = 16 });
            if (GUI.Button(new Rect(buttonLeft, top + 220f, buttonWidth, 48f), "Voltar"))
            {
                showOptions = false;
            }
            return;
        }

        if (GUI.Button(new Rect(buttonLeft, top + 100f, buttonWidth, 52f), "Continuar jogando"))
        {
            ClosePauseMenu();
        }
        if (GUI.Button(new Rect(buttonLeft, top + 166f, buttonWidth, 52f), "Opcoes"))
        {
            showOptions = true;
        }
        if (GUI.Button(new Rect(buttonLeft, top + 232f, buttonWidth, 52f), "Sair para o menu"))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Menu");
        }
        GUI.Label(new Rect(buttonLeft, top + 316f, buttonWidth, 24f), "Esc: continuar", GUI.skin.label);
    }

    private static void DrawDimBackground()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private static void DrawDemonstrationPage(Rect content, GUIStyle heading, GUIStyle body, string title, string description)
    {
        GUI.Label(new Rect(content.x + 22f, content.y + 18f, content.width - 44f, 32f), title, heading);
        GUI.Label(new Rect(content.x + 22f, content.y + 68f, content.width - 44f, 90f), description, body);
    }

    private string EquipmentName(EquipmentSlot2D slot)
    {
        InventoryItemId item = equipment != null ? equipment.GetEquippedItem(slot) : InventoryItemId.None;
        return InventoryItemCatalog.GetDisplayName(item);
    }

    private void UseSelectedItem()
    {
        if (inventory == null || vitals == null)
        {
            statusMessage = "O inventario ou os recursos do jogador nao estao configurados.";
            return;
        }

        inventory.TryUseItemAt(selectedSlot, vitals, out statusMessage);
    }

    private void EquipSelectedItem(EquipmentSlot2D slot)
    {
        if (equipment == null)
        {
            statusMessage = "O equipamento do jogador nao esta configurado.";
            return;
        }

        equipment.TryEquipSelected(inventory, selectedSlot, slot, out statusMessage);
    }

    private void DrawEquipmentButton(Rect rect, string slotName, EquipmentSlot2D slot)
    {
        InventoryItemId equippedItem = equipment != null ? equipment.GetEquippedItem(slot) : InventoryItemId.None;
        string itemName = equippedItem == InventoryItemId.None ? "vazio" : InventoryItemCatalog.GetDisplayName(equippedItem);
        GUI.enabled = equipment != null && equippedItem != InventoryItemId.None;
        if (GUI.Button(rect, $"{slotName}: {itemName}", equipmentButtonStyle))
        {
            equipment.TryUnequip(slot, inventory, out statusMessage);
        }
        GUI.enabled = true;
    }

    private void DrawContactPowerButton(Rect rect)
    {
        string powerName = "vazio";
        if (powerLoadout != null)
        {
            switch (powerLoadout.EquippedContactPower)
            {
                case ContactPowerId.SpoonMagnetism: powerName = "Magnetismo"; break;
                case ContactPowerId.RedGloveStrength: powerName = "Luva"; break;
            }
        }

        GUI.enabled = powerLoadout != null && powerLoadout.OwnedContactPowers.Count > 0;
        if (GUI.Button(rect, "Contato: " + powerName, equipmentButtonStyle))
        {
            powerLoadout.CycleContactPower();
            statusMessage = powerLoadout.EquippedContactPower == ContactPowerId.None
                ? "Poder de Contato desequipado."
                : "Poder de Contato trocado.";
        }
        GUI.enabled = true;
    }

    private void ResolveReferences()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            if (inventory == null) inventory = player.GetComponent<PlayerInventory2D>();
            if (wallet == null) wallet = player.GetComponent<PlayerWallet2D>();
            if (vitals == null) vitals = player.GetComponent<PlayerVitals>();
            if (equipment == null) equipment = player.GetComponent<PlayerEquipment2D>();
            if (powerLoadout == null) powerLoadout = player.GetComponent<PlayerPowerLoadout2D>();
            if (progression == null) progression = player.GetComponent<PlayerProgression2D>();
        }

        if (dialogueManager == null) dialogueManager = Object.FindFirstObjectByType<DialogueManager2D>();
        if (mainQuest == null) mainQuest = Object.FindFirstObjectByType<PrologueQuest>();
        if (sideQuest == null) sideQuest = Object.FindFirstObjectByType<PrologueSideQuest2D>();
    }

    private void EnsureStyles()
    {
        if (slotStyle == null)
        {
            slotStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 9,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            selectedSlotStyle = new GUIStyle(slotStyle)
            {
                fontStyle = FontStyle.Bold
            };
            selectedSlotStyle.normal.textColor = Color.yellow;
            equipmentButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
        }
    }
}
