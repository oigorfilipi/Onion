using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Encontra NPCs próximos, mostra conversas e entrega presentes ao concluir o diálogo.
/// </summary>
public class NpcDialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text interactionHint;
    [SerializeField, Tooltip("Use {npc} no lugar do nome do personagem com quem falar.")]
    private string interactionHintFormat = "F: Conversar com {npc}";

    private PlayerVitals playerVitals;
    private PlayerController playerController;
    private ActiveWeapon activeWeapon;
    private NpcDialogue currentNpc;
    private int currentLine;
    private InventoryController inventoryController;
    private BackpackController backpackController;
    private ItemDictionary itemDictionary;
    private SaveController saveController;
    private bool wasMovementEnabled;
    private bool wasWeaponEnabled;
    public static bool IsDialogueOpen { get; private set; }

    private void Awake()
    {
        IsDialogueOpen = false;
        if (dialogueRoot != null) dialogueRoot.SetActive(false);
        if (interactionHint != null) interactionHint.gameObject.SetActive(false);
    }

    private void Start()
    {
        playerVitals = FindAnyObjectByType<PlayerVitals>();
        inventoryController = FindAnyObjectByType<InventoryController>();
        backpackController = FindAnyObjectByType<BackpackController>();
        itemDictionary = FindAnyObjectByType<ItemDictionary>();
        saveController = FindAnyObjectByType<SaveController>();
        if (playerVitals != null)
        {
            playerController = playerVitals.GetComponent<PlayerController>();
            activeWeapon = playerVitals.GetComponentInChildren<ActiveWeapon>(true);
        }
    }

    private void Update()
    {
        if (playerVitals == null || Keyboard.current == null) return;
        if (playerVitals.IsDead)
        {
            CloseDialogue();
            if (interactionHint != null) interactionHint.gameObject.SetActive(false);
            return;
        }

        if (IsDialogueOpen)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame) CloseDialogue();
            else if (Keyboard.current.fKey.wasPressedThisFrame ||
                     Keyboard.current.spaceKey.wasPressedThisFrame) NextLine();
            return;
        }

        if (Time.timeScale <= 0f) return;
        NpcDialogue nearby = FindNearestNpc();
        if (interactionHint != null)
        {
            interactionHint.gameObject.SetActive(nearby != null);
            if (nearby != null)
                interactionHint.text = interactionHintFormat.Replace("{npc}", nearby.DisplayName);
        }

        if (nearby != null && Keyboard.current.fKey.wasPressedThisFrame) OpenDialogue(nearby);
    }

    private NpcDialogue FindNearestNpc()
    {
        NpcDialogue nearest = null;
        float nearestSqrDistance = float.MaxValue;
        foreach (NpcDialogue npc in FindObjectsByType<NpcDialogue>())
        {
            float sqrDistance = (npc.transform.position - playerVitals.transform.position).sqrMagnitude;
            if (sqrDistance <= npc.InteractionDistance * npc.InteractionDistance &&
                sqrDistance < nearestSqrDistance)
            {
                nearest = npc;
                nearestSqrDistance = sqrDistance;
            }
        }
        return nearest;
    }

    // Fecha inventário, bloqueia controles do player e mostra falas; o mundo continua em execução.
    private void OpenDialogue(NpcDialogue npc)
    {
        if (dialogueRoot == null) return;
        currentNpc = npc;
        currentLine = 0;
        IsDialogueOpen = true;
        if (interactionHint != null) interactionHint.gameObject.SetActive(false);
        MenuController menu = FindAnyObjectByType<MenuController>();
        if (menu != null && menu.menuCanvas != null) menu.menuCanvas.SetActive(false);
        FindAnyObjectByType<BackpackController>()?.CloseWindow();
        wasMovementEnabled = playerController != null && playerController.enabled;
        wasWeaponEnabled = activeWeapon != null && activeWeapon.enabled;
        if (playerController != null) playerController.enabled = false;
        if (activeWeapon != null) activeWeapon.enabled = false;
        dialogueRoot.SetActive(true);
        RefreshLine();
    }

    private void NextLine()
    {
        if (currentNpc == null) { CloseDialogue(); return; }
        currentLine++;
        if (currentNpc.DialogueLines == null || currentLine >= currentNpc.DialogueLines.Length)
        {
            NpcDialogue completedNpc = currentNpc;
            CloseDialogue();
            GrantNpcReward(completedNpc);
        }
        else RefreshLine();
    }

    // Dá espadas e arco uma única vez; repõe flechas até 99 após o tempo permitido.
    private void GrantNpcReward(NpcDialogue npc)
    {
        if (npc == null || playerVitals == null || playerVitals.IsDead || GameSession.RunWon) return;
        bool alreadyClaimed = GameSession.HasClaimedNpcReward(npc.Role);

        if (npc.Role == NpcRole.Madrasta && !alreadyClaimed)
        {
            if (GiveItem("Espada de Ferro", 1) > 0)
                GameSession.ClaimNpcReward(npc.Role);
        }
        else if (npc.Role == NpcRole.Velho && !alreadyClaimed)
        {
            if (GiveItem("Espada de Diamante", 1) > 0)
                GameSession.ClaimNpcReward(npc.Role);
        }
        else if (npc.Role == NpcRole.LoucoDoArco)
        {
            if (!alreadyClaimed)
            {
                if (GiveItem("Arco", 1) <= 0) return;
                GameSession.ClaimNpcReward(npc.Role);
                RefillArrows();
                GameSession.SetNextArrowRefillAt(GameSession.RunElapsedSeconds + 60f);
            }
            else if (GameSession.RunElapsedSeconds >= GameSession.NextArrowRefillAt &&
                     RefillArrows() > 0)
            {
                GameSession.SetNextArrowRefillAt(GameSession.RunElapsedSeconds + 60f);
            }
        }
        else if (npc.Role == NpcRole.Mercadora && !alreadyClaimed)
        {
            int given = GiveItem("Capacete de Ferro", 1);
            given += GiveItem("Peitoral de Ferro", 1);
            given += GiveItem("Botas de Ferro", 1);
            if (given > 0) GameSession.ClaimNpcReward(npc.Role);
        }

        saveController?.SaveGame();
    }

    private int RefillArrows()
    {
        int owned = (inventoryController != null ? inventoryController.CountItem("Flecha") : 0) +
                    (backpackController != null ? backpackController.CountItem("Flecha") : 0) +
                    (saveController != null ? saveController.CountPendingRewardItem("Flecha") : 0);
        return GiveItem("Flecha", Mathf.Max(0, 99 - owned));
    }

    private int GiveItem(string itemName, int quantity)
    {
        if (quantity <= 0 || inventoryController == null || itemDictionary == null) return 0;
        GameObject prefab = itemDictionary.GetItemPrefab(itemName);
        Item item = prefab != null ? prefab.GetComponent<Item>() : null;
        if (item == null) return 0;

        int accepted = inventoryController.AddItemQuantity(item, quantity);
        int remaining = quantity - accepted;
        if (remaining > 0 && saveController != null)
        {
            saveController.CreateRewardDrop(item, remaining);
            accepted += remaining;
        }

        if (accepted > 0 && ItemPickupUIController.Instance != null)
        {
            SpriteRenderer renderer = item.GetComponent<SpriteRenderer>();
            Image image = item.GetComponent<Image>();
            Sprite icon = renderer != null && renderer.sprite != null
                ? renderer.sprite : image != null ? image.sprite : null;
            ItemPickupUIController.Instance.ShowItemPickup(
                accepted > 1 ? $"{accepted}x {item.Name}" : item.Name, icon);
        }

        return accepted;
    }

    private void RefreshLine()
    {
        if (speakerText != null) speakerText.text = currentNpc.DisplayName;
        if (bodyText == null) return;
        if (currentLine == 0)
        {
            string playerName = playerVitals != null ? playerVitals.PlayerName : "jogador";
            switch (currentNpc.Role)
            {
                case NpcRole.Madrasta:
                    bodyText.text = $"Olá {playerName}, pronto pra matança?";
                    return;
                case NpcRole.Velho:
                    bodyText.text = "Com essa espada de Ferro aí, você não irá pra lugar nenhum.";
                    return;
                case NpcRole.Mercadora:
                    bodyText.text = "Pelado assim, você pretende sobreviver por quanto tempo?";
                    return;
                case NpcRole.LoucoDoArco:
                    bodyText.text = "Os Slimes gostam da aproximação, tenta algo mais distante.";
                    return;
            }
        }
        bodyText.text = currentNpc.DialogueLines != null && currentNpc.DialogueLines.Length > currentLine
            ? currentNpc.DialogueLines[currentLine] : "Diálogo a definir.";
    }

    public void CloseDialogue()
    {
        if (!IsDialogueOpen) return;
        IsDialogueOpen = false;
        currentNpc = null;
        if (dialogueRoot != null) dialogueRoot.SetActive(false);
        if (playerVitals != null && !playerVitals.IsDead)
        {
            if (playerController != null) playerController.enabled = wasMovementEnabled;
            if (activeWeapon != null) activeWeapon.enabled = wasWeaponEnabled;
        }
    }

    private void OnDestroy()
    {
        if (IsDialogueOpen) CloseDialogue();
        IsDialogueOpen = false;
    }
}
