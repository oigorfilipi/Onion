using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Coordena leitura e escrita do save, restauração do mundo e entregas pendentes.
/// </summary>
public class SaveController : MonoBehaviour
{
    private string saveLocation;
    private InventoryController inventoryController;
    private EquipmentController equipmentController;
    private BackpackController backpackController;
    private PlayerVitals playerVitals;
    private GameObject player;
    private Vector3 originalPlayerPosition;
    private readonly HashSet<string> collectedWorldItems = new HashSet<string>();
    private readonly HashSet<string> defeatedEnemies = new HashSet<string>();
    private readonly List<PendingRewardDrop> pendingRewardDrops = new List<PendingRewardDrop>();
    private bool loading;
    private bool initialized;
    private bool saveQueued;
    private int saveQueuedFrame;
    public bool IsInitialized => initialized;

    // Escolhe entre nova partida, continuação, transição e reinício antes de ligar eventos de autosave.
    private void Start()
    {
        inventoryController = FindAnyObjectByType<InventoryController>();
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("SaveController: Player com a tag 'Player' não encontrado.");
            return;
        }

        equipmentController = player.GetComponent<EquipmentController>();
        backpackController = player.GetComponent<BackpackController>();
        playerVitals = player.GetComponent<PlayerVitals>();
        originalPlayerPosition = player.transform.position;
        saveLocation = !string.IsNullOrEmpty(GameSession.CurrentSavePath)
            ? GameSession.CurrentSavePath
            : Path.Combine(Application.persistentDataPath, "saveData.json");

        GameSession.StartMode mode = GameSession.StartMode.None;
        string requestedName = null;
        string spawnId = null;
        if (GameSession.TryConsume(out mode, out int slotIndex, out requestedName, out spawnId) &&
            (mode == GameSession.StartMode.NewGame || mode == GameSession.StartMode.Continue))
        {
            saveLocation = mode == GameSession.StartMode.NewGame
                ? SaveSlotService.GetSlotPath(slotIndex)
                : SaveSlotService.GetLoadPath(slotIndex);
        }

        GameSession.SetCurrentSavePath(saveLocation);

        if (mode == GameSession.StartMode.NewGame || mode == GameSession.StartMode.Restart)
        {
            ResetProgress(requestedName);
            PlayerProfileService.BeginRun(GameSession.CurrentRunId, GameSession.CurrentSlotIndex,
                playerVitals != null ? playerVitals.PlayerName : requestedName,
                GameSession.CurrentDifficulty);
            if (SceneSpawnPoint.TryFind("Casa", out Vector3 homePosition))
                player.transform.position = homePosition;
            SaveGame();
        }
        else
        {
            LoadGame();
            if (mode == GameSession.StartMode.SceneTransition)
            {
                if (SceneSpawnPoint.TryFind(spawnId, out Vector3 arrivalPosition))
                    player.transform.position = arrivalPosition;
                else
                    Debug.LogWarning($"Ponto de chegada '{spawnId}' não encontrado nesta cena.");
                SaveGame();
            }
        }

        Slot.AnySlotChanged += QueueSave;
        ItemStack.AnyQuantityChanged += QueueSave;
        if (equipmentController != null) equipmentController.EquipmentChanged += QueueSave;
        if (playerVitals != null) playerVitals.StatsChanged += QueueSave;
        if (playerVitals != null) playerVitals.Died += SaveGame;
        GameSession.ProgressChanged += QueueSave;
        QuickbarController.AnySlotChanged += QueueSave;
        initialized = true;
    }

    private void Update()
    {
        if (!saveQueued || Time.frameCount <= saveQueuedFrame) return;
        saveQueued = false;
        SaveGame();
    }

    private void OnDestroy()
    {
        // Um evento de inventário no último quadro pode ter deixado um save na fila.
        if (initialized && saveQueued && !loading && player != null) SaveGame();
        Slot.AnySlotChanged -= QueueSave;
        ItemStack.AnyQuantityChanged -= QueueSave;
        if (equipmentController != null) equipmentController.EquipmentChanged -= QueueSave;
        if (playerVitals != null) playerVitals.StatsChanged -= QueueSave;
        if (playerVitals != null) playerVitals.Died -= SaveGame;
        GameSession.ProgressChanged -= QueueSave;
        QuickbarController.AnySlotChanged -= QueueSave;
    }

    public void RegisterCollectedWorldItem(Item item)
    {
        if (item != null && !item.IsRuntimeSpawn && collectedWorldItems.Add(item.WorldSaveKey))
            QueueSave();
    }

    public void RegisterDefeatedEnemy(EnemyHealth enemy)
    {
        if (enemy != null && !enemy.IsRuntimeSpawn && defeatedEnemies.Add(enemy.WorldSaveKey))
            QueueSave();
    }

    public void CreateRewardDrop(Item prefabItem, int quantity)
    {
        if (prefabItem == null || quantity <= 0 || player == null) return;
        Vector3 position = FindRewardDropPosition();
        PendingRewardDrop drop = new PendingRewardDrop
        {
            id = System.Guid.NewGuid().ToString("N"),
            sceneName = SceneManager.GetActiveScene().name,
            itemName = prefabItem.Name,
            quantity = quantity,
            position = position
        };
        pendingRewardDrops.Add(drop);
        SpawnRewardDrop(drop, prefabItem.gameObject);
        QueueSave();
    }

    private Vector3 FindRewardDropPosition()
    {
        Vector2[] directions =
        {
            Vector2.right, Vector2.left, Vector2.up, Vector2.down,
            new Vector2(1f, 1f), new Vector2(-1f, 1f),
            new Vector2(1f, -1f), new Vector2(-1f, -1f)
        };
        foreach (Vector2 direction in directions)
        {
            Vector3 position = player.transform.position + (Vector3)(direction.normalized * 1.1f);
            bool blocked = false;
            foreach (Collider2D collider in Physics2D.OverlapCircleAll(position, 0.3f))
            {
                if (!collider.isTrigger && !collider.transform.IsChildOf(player.transform))
                {
                    blocked = true;
                    break;
                }
            }
            if (!blocked) return position;
        }
        return player.transform.position;
    }

    public void UpdateRewardDrop(string id, int remaining)
    {
        if (string.IsNullOrEmpty(id)) return;
        PendingRewardDrop drop = pendingRewardDrops.Find(entry => entry.id == id);
        if (drop == null) return;
        if (remaining <= 0) pendingRewardDrops.Remove(drop);
        else drop.quantity = remaining;
        QueueSave();
    }

    public int CountPendingRewardItem(string itemName)
    {
        int count = 0;
        foreach (PendingRewardDrop drop in pendingRewardDrops)
            if (drop != null && drop.itemName == itemName) count += drop.quantity;
        return count;
    }

    // Adia o save até depois da mudança de slot ou atributo terminar no quadro atual.
    private void QueueSave()
    {
        if (!initialized || loading) return;
        saveQueued = true;
        saveQueuedFrame = Time.frameCount;
    }

    // Fotografa o estado do personagem, mundo, tempo e inventários em um SaveData serializado para JSON.
    public void SaveGame()
    {
        if (player == null) return;
        if (string.IsNullOrEmpty(saveLocation))
            saveLocation = Path.Combine(Application.persistentDataPath, "saveData.json");

        SaveData saveData = new SaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            playerPosition = player.transform.position,
            hasVitalsData = playerVitals != null,
            playerName = playerVitals != null ? playerVitals.PlayerName : string.Empty,
            playerHealth = playerVitals != null ? playerVitals.CurrentHealth : 0,
            playerStamina = playerVitals != null ? playerVitals.CurrentStamina : 0,
            playerLevel = playerVitals != null ? playerVitals.Level : 1,
            playerExperience = playerVitals != null ? playerVitals.Experience : 0,
            remainingFireImmunity = playerVitals != null ? playerVitals.RemainingFireImmunity : 0f,
            remainingPoisonImmunity = playerVitals != null ? playerVitals.RemainingPoisonImmunity : 0f,
            difficulty = (int)GameSession.CurrentDifficulty,
            runElapsedSeconds = GameSession.RunElapsedSeconds,
            runWon = GameSession.RunWon,
            playerDead = playerVitals != null && playerVitals.IsDead,
            runId = GameSession.CurrentRunId,
            runCoins = GameSession.RunCoins,
            runEnemiesKilled = GameSession.RunEnemiesKilled,
            runFireSlimesKilled = GameSession.RunFireSlimesKilled,
            runGhostSlimesKilled = GameSession.RunGhostSlimesKilled,
            runBossSlimesKilled = GameSession.RunBossSlimesKilled,
            runUsedIronSword = GameSession.RunUsedIronSword,
            runUsedBow = GameSession.RunUsedBow,
            runUsedMeleeWeapon = GameSession.RunUsedMeleeWeapon,
            claimedNpcRewards = GameSession.ClaimedNpcRewards,
            nextArrowRefillAt = GameSession.NextArrowRefillAt,
            inventorySaveData = inventoryController != null
                ? inventoryController.GetInventoryItems() : new List<InventorySaveData>(),
            equipmentSaveData = equipmentController != null
                ? equipmentController.GetEquipmentItems() : new List<EquipmentSaveData>(),
            backpackSaveData = backpackController != null
                ? backpackController.GetBackpackItems() : new List<InventorySaveData>(),
            quickbarSaveData = FindAnyObjectByType<QuickbarController>() != null
                ? FindAnyObjectByType<QuickbarController>().GetSavedItems() : new List<QuickbarSaveData>(),
            collectedWorldItems = new List<string>(collectedWorldItems),
            defeatedEnemies = new List<string>(defeatedEnemies),
            pendingRewardDrops = new List<PendingRewardDrop>(pendingRewardDrops)
        };

        try
        {
            string directory = Path.GetDirectoryName(saveLocation);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temporaryPath = saveLocation + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(saveData));
            if (File.Exists(saveLocation)) File.Replace(temporaryPath, saveLocation, saveLocation + ".bak");
            else File.Move(temporaryPath, saveLocation);
            Item primaryWeapon = equipmentController != null &&
                equipmentController.GetSlot(EquipmentSlotType.PrimaryWeapon) != null
                ? equipmentController.GetSlot(EquipmentSlotType.PrimaryWeapon).CurrentItem?.GetComponent<Item>()
                : null;
            int swordDamage = primaryWeapon != null
                ? primaryWeapon.weaponDamage + (playerVitals != null ? playerVitals.SwordDamageBonus : 0) : 0;
            int bowDamage = 7 + (playerVitals != null ? playerVitals.SwordDamageBonus : 0);
            PlayerProfileService.SyncRun(GameSession.CurrentRunId, saveData.playerName,
                GameSession.CurrentSlotIndex, GameSession.CurrentDifficulty, GameSession.RunElapsedSeconds,
                saveData.playerLevel, playerVitals != null ? playerVitals.MaxHealth : 0,
                playerVitals != null ? playerVitals.MaxStamina : 0,
                playerVitals != null ? playerVitals.CurrentHealth : 0,
                playerVitals != null ? playerVitals.CurrentStamina : 0, swordDamage, bowDamage,
                playerVitals != null ? playerVitals.Defense : 0, GameSession.RunCoins,
                GameSession.RunEnemiesKilled, GameSession.RunFireSlimesKilled,
                GameSession.RunGhostSlimesKilled, GameSession.RunBossSlimesKilled,
                GameSession.RunUsedIronSword, GameSession.RunUsedBow, GameSession.RunUsedMeleeWeapon,
                playerVitals != null && playerVitals.IsDead, GameSession.RunWon);
            saveQueued = false;
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"Não foi possível salvar o jogo: {exception.Message}");
        }
    }

    // Lê o JSON, restaura dificuldade e estado persistente, depois reconstrói itens e progresso da cena.
    public void LoadGame()
    {
        if (string.IsNullOrEmpty(saveLocation))
            saveLocation = Path.Combine(Application.persistentDataPath, "saveData.json");

        if (!File.Exists(saveLocation))
        {
            ResetProgress(playerVitals != null ? playerVitals.PlayerName : "Jogador");
            SaveGame();
            return;
        }

        SaveData saveData;
        try { saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(saveLocation)); }
        catch (System.Exception exception)
        {
            Debug.LogError($"Não foi possível ler o save: {exception.Message}");
            return;
        }

        if (saveData == null)
        {
            Debug.LogError("Arquivo de save inválido.");
            return;
        }

        loading = true;
        GameSession.SetDifficulty(saveData.difficulty);
        GameSession.RestoreRunProgress(saveData.runElapsedSeconds, saveData.runWon,
            saveData.claimedNpcRewards, saveData.nextArrowRefillAt, saveData.runCoins,
            saveData.runEnemiesKilled, saveData.runFireSlimesKilled, saveData.runGhostSlimesKilled,
            saveData.runBossSlimesKilled, saveData.runUsedIronSword, saveData.runUsedBow,
            saveData.runUsedMeleeWeapon, saveData.runId, GameSession.CurrentSlotIndex);
        if (saveData.playerDead || saveData.runWon)
        {
            loading = false;
            Debug.LogWarning("Essa tentativa já terminou e não pode ser continuada. Inicie um novo jogo nessa vaga.");
            return;
        }
        collectedWorldItems.Clear();
        defeatedEnemies.Clear();
        pendingRewardDrops.Clear();
        if (saveData.collectedWorldItems != null)
            collectedWorldItems.UnionWith(saveData.collectedWorldItems);
        if (saveData.defeatedEnemies != null)
            defeatedEnemies.UnionWith(saveData.defeatedEnemies);
        if (saveData.pendingRewardDrops != null)
            pendingRewardDrops.AddRange(saveData.pendingRewardDrops);

        if (player != null && (string.IsNullOrEmpty(saveData.sceneName) ||
                               saveData.sceneName == SceneManager.GetActiveScene().name))
            player.transform.position = saveData.playerPosition;

        inventoryController?.SetInventoryItems(saveData.inventorySaveData ?? new List<InventorySaveData>());
        equipmentController?.SetEquipmentItems(saveData.equipmentSaveData ?? new List<EquipmentSaveData>());
        backpackController?.SetBackpackItems(saveData.backpackSaveData ?? new List<InventorySaveData>());
        if (playerVitals != null && saveData.hasVitalsData)
        {
            playerVitals.SetPlayerName(saveData.playerName);
            playerVitals.RestoreSavedVitals(saveData.playerHealth, saveData.playerStamina,
                                            saveData.playerLevel, saveData.playerExperience,
                                            saveData.remainingFireImmunity,
                                            saveData.remainingPoisonImmunity);
        }
        FindAnyObjectByType<QuickbarController>()?.SetSavedItems(saveData.quickbarSaveData);

        ApplyWorldProgress();
        RestoreRewardDrops();
        loading = false;
    }

    private void RestoreRewardDrops()
    {
        ItemDictionary dictionary = FindAnyObjectByType<ItemDictionary>();
        if (dictionary == null) return;
        foreach (PendingRewardDrop drop in pendingRewardDrops)
        {
            if (drop.sceneName != SceneManager.GetActiveScene().name) continue;
            GameObject prefab = dictionary.GetItemPrefab(drop.itemName);
            if (prefab != null) SpawnRewardDrop(drop, prefab);
        }
    }

    private static void SpawnRewardDrop(PendingRewardDrop drop, GameObject prefab)
    {
        GameObject worldObject = Instantiate(prefab, drop.position, Quaternion.identity);
        Item item = worldObject.GetComponent<Item>();
        if (item == null)
        {
            Destroy(worldObject);
            return;
        }
        item.MarkRuntimeSpawn();
        item.SetRewardDropId(drop.id);
        ItemStack.Ensure(worldObject).SetQuantity(drop.quantity);
    }

    private void ApplyWorldProgress()
    {
        foreach (Item item in FindObjectsByType<Item>(FindObjectsInactive.Include))
        {
            if (item.gameObject.scene == gameObject.scene && item.CompareTag("Item") &&
                collectedWorldItems.Contains(item.WorldSaveKey))
            {
                item.gameObject.SetActive(false);
                Destroy(item.gameObject);
            }
        }

        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include))
        {
            if (enemy.gameObject.scene == gameObject.scene &&
                defeatedEnemies.Contains(enemy.WorldSaveKey))
            {
                enemy.gameObject.SetActive(false);
                Destroy(enemy.gameObject);
            }
        }
    }

    // Zera o progresso da tentativa mantendo o nome e a dificuldade escolhidos para o reinício.
    private void ResetProgress(string playerName)
    {
        loading = true;
        GameSession.ResetRunProgress();
        collectedWorldItems.Clear();
        defeatedEnemies.Clear();
        pendingRewardDrops.Clear();
        if (player != null) player.transform.position = originalPlayerPosition;
        inventoryController?.SetInventoryItems(new List<InventorySaveData>());
        equipmentController?.SetEquipmentItems(new List<EquipmentSaveData>());
        backpackController?.SetBackpackItems(new List<InventorySaveData>());
        FindAnyObjectByType<QuickbarController>()?.SetSavedItems(new List<QuickbarSaveData>());
        playerVitals?.ResetForNewGame(playerName);
        loading = false;
    }

    public bool StartNewGame(string saveName)
    {
        if (!TryGetNamedSavePath(saveName, out string newSavePath) || File.Exists(newSavePath))
            return false;
        saveLocation = newSavePath;
        GameSession.SetCurrentSavePath(saveLocation);
        ResetProgress(saveName);
        SaveGame();
        return true;
    }

    public bool LoadNamedGame(string saveName)
    {
        if (!TryGetNamedSavePath(saveName, out string namedSavePath) || !File.Exists(namedSavePath))
            return false;
        saveLocation = namedSavePath;
        GameSession.SetCurrentSavePath(saveLocation);
        LoadGame();
        return true;
    }

    private static bool TryGetNamedSavePath(string saveName, out string namedSavePath)
    {
        namedSavePath = null;
        if (string.IsNullOrWhiteSpace(saveName)) return false;
        string trimmedName = saveName.Trim();
        if (trimmedName.Length > 40 || trimmedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return false;
        namedSavePath = Path.Combine(Application.persistentDataPath, $"save_{trimmedName}.json");
        return true;
    }

    private void OnApplicationQuit()
    {
        if (initialized) SaveGame();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && initialized && playerVitals != null && !playerVitals.IsDead && !GameSession.RunWon)
            SaveGame();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && initialized && playerVitals != null && !playerVitals.IsDead && !GameSession.RunWon)
            SaveGame();
    }
}
