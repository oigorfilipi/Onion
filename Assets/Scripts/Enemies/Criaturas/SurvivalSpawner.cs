using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Gera slimes e itens de cura no mapa externo em células válidas e nos intervalos da partida.
/// </summary>
public class SurvivalSpawner : MonoBehaviour
{
    [Header("Área de nascimento")]
    [SerializeField] private Tilemap walkableGround;
    [SerializeField] private Tilemap blockingTiles;
    [SerializeField, Min(1f)] private float minimumDistanceFromPlayer = 9f;
    [SerializeField, Min(1f)] private float maximumDistanceFromPlayer = 16f;
    [SerializeField, Min(1)] private int positionAttempts = 36;
    [SerializeField, Min(0)] private int borderMarginCells = 3;

    [Header("Slimes")]
    [SerializeField] private GameObject fireSlimePrefab;
    [SerializeField] private GameObject ghostSlimePrefab;
    [SerializeField, Min(0.1f)] private float initialSpawnInterval = 3f;
    [SerializeField, Min(0.1f)] private float minimumSpawnInterval = 1f;
    [SerializeField, Min(1)] private int maximumSlimesAlive = 20;
    [SerializeField, Range(0f, 1f)] private float projectileShooterChance = 0.27f;

    [Header("Balanceamento por dificuldade")]
    [SerializeField, Min(1f)] private float normalSlimeBaseMultiplier = 1.15f;
    [SerializeField, Min(1f)] private float bossBaseMultiplier = 3f;
    [SerializeField, Min(1f)] private float mediumEnemyMultiplier = 1.05f;
    [SerializeField, Min(1f)] private float hardEnemyMultiplier = 1.10f;
    [SerializeField, Min(1f)] private float insaneEnemyMultiplier = 1.15f;
    [SerializeField, Min(1f)] private float mediumItemIntervalMultiplier = 1.3f;
    [SerializeField, Min(1f)] private float hardItemIntervalMultiplier = 1.6f;
    [SerializeField, Min(1f)] private float insaneItemIntervalMultiplier = 2f;

    [Header("Itens de cura")]
    [SerializeField] private Item[] healingItemPrefabs;
    [SerializeField, Min(1f)] private float healingSpawnInterval = 25f;
    [SerializeField, Min(1)] private int maximumHealingItems = 8;
    [Header("Moedas e chefes")]
    [SerializeField, Min(2f)] private float minimumCoinSpawnDelay = 8f;
    [SerializeField, Min(2f)] private float maximumCoinSpawnDelay = 18f;
    [SerializeField, Min(1)] private int maximumCoinsOnGround = 10;
    [SerializeField, Min(1)] private int bossEveryLevels = 5;

    private PlayerVitals player;
    private SaveController saveController;
    private PrologueChallenge challenge;
    private OnionMenuController pauseMenu;
    private Camera gameplayCamera;
    private ItemDictionary itemDictionary;
    private float nextSlimeAt;
    private float nextHealingAt;
    private float nextCoinAt;
    private int lastBossLevelMilestone;
    private readonly List<Bounds> pickupOcclusionBounds = new List<Bounds>();
    private float DifficultyEnemyMultiplier => GameSession.CurrentDifficulty switch
    {
        GameSession.Difficulty.Medium => mediumEnemyMultiplier,
        GameSession.Difficulty.Hard => hardEnemyMultiplier,
        GameSession.Difficulty.Insane => insaneEnemyMultiplier,
        _ => 1f
    };
    private float HealingInterval => healingSpawnInterval * (GameSession.CurrentDifficulty switch
    {
        GameSession.Difficulty.Medium => mediumItemIntervalMultiplier,
        GameSession.Difficulty.Hard => hardItemIntervalMultiplier,
        GameSession.Difficulty.Insane => insaneItemIntervalMultiplier,
        _ => 1f
    });

    private void Start()
    {
        player = FindAnyObjectByType<PlayerVitals>();
        saveController = FindAnyObjectByType<SaveController>();
        challenge = FindAnyObjectByType<PrologueChallenge>();
        pauseMenu = FindAnyObjectByType<OnionMenuController>();
        gameplayCamera = Camera.main;
        itemDictionary = FindAnyObjectByType<ItemDictionary>();
        // Evita colocar curas e moedas sobre as copas de árvores e arbustos.
        foreach (SpriteRenderer decoration in FindObjectsByType<SpriteRenderer>())
        {
            string objectName = decoration.gameObject.name;
            if (objectName.StartsWith("Arvore") || objectName.StartsWith("Árvore") ||
                objectName.StartsWith("Arbusto"))
                pickupOcclusionBounds.Add(decoration.bounds);
        }
        nextSlimeAt = Time.time + initialSpawnInterval;
        nextHealingAt = Time.time + HealingInterval;
        nextCoinAt = Time.time + Random.Range(minimumCoinSpawnDelay, maximumCoinSpawnDelay);
        // Recarregar um save depois de derrotar o chefe do marco não deve criá-lo novamente.
        int reachedBossMilestones = player != null ? player.Level / bossEveryLevels : 0;
        lastBossLevelMilestone = Mathf.Min(reachedBossMilestones, GameSession.RunBossSlimesKilled);
    }

    private void Update()
    {
        if (player == null || player.IsDead || saveController == null ||
            !saveController.IsInitialized || GameSession.RunWon || Time.timeScale <= 0f ||
            (pauseMenu != null && pauseMenu.IsPauseOpenOrOpening)) return;

        TrySpawnBossForNewLevels();
        if (Time.time >= nextSlimeAt)
        {
            SpawnSlime();
            float progress = Mathf.Clamp01(GameSession.RunElapsedSeconds / 600f);
            float timeDrivenMinimum = Mathf.Max(0.4f, minimumSpawnInterval -
                Mathf.Floor(GameSession.RunElapsedSeconds / 180f) * 0.1f);
            nextSlimeAt = Time.time + Mathf.Lerp(initialSpawnInterval, timeDrivenMinimum, progress);
        }

        if (Time.time >= nextHealingAt)
        {
            SpawnHealingItem();
            nextHealingAt = Time.time + HealingInterval;
        }

        if (Time.time >= nextCoinAt)
        {
            SpawnCoin();
            nextCoinAt = Time.time + Random.Range(minimumCoinSpawnDelay, maximumCoinSpawnDelay);
        }
    }

    // Escolhe o prefab e os multiplicadores de variante forte, normal ou fraca, respeitando o limite de vivos.
    private void SpawnSlime()
    {
        int dynamicLimit = maximumSlimesAlive + Mathf.FloorToInt(GameSession.RunElapsedSeconds / 60f) * 2;
        if (FindObjectsByType<EnemyHealth>().Length >= dynamicLimit ||
            !TryFindSpawnPosition(out Vector3 position,
                                  minimumDistanceFromPlayer, maximumDistanceFromPlayer, true)) return;

        GameObject prefab = Random.value < 0.5f ? fireSlimePrefab : ghostSlimePrefab;
        if (prefab == null) return;
        GameObject slime = Instantiate(prefab, position, Quaternion.identity, transform);

        float progress = Mathf.Clamp01(GameSession.RunElapsedSeconds / 600f);
        float roll = Random.value;
        float strongChance = Mathf.Lerp(0.2f, 0.4f, progress);
        bool strong = roll < strongChance;
        bool weak = !strong && roll > 0.7f;
        float healthMultiplier = strong ? 1.5f : weak ? 0.7f : 1f;
        float damageMultiplier = strong ? 1.3f : weak ? 0.75f : 1f;
        float speedMultiplier = strong ? 1.15f : weak ? 0.85f : 1f;

        slime.GetComponent<EnemyHealth>()?.ConfigureRuntimeHealth(
            healthMultiplier * normalSlimeBaseMultiplier * DifficultyEnemyMultiplier);
        EnemyAI slimeAI = slime.GetComponent<EnemyAI>();
        if (slimeAI != null)
        {
            slimeAI.ConfigureRuntimeDamage(
                damageMultiplier * normalSlimeBaseMultiplier * DifficultyEnemyMultiplier);
            slimeAI.ConfigureRangedAttacks(Random.value < projectileShooterChance);
        }
        slime.GetComponent<EnemyPathfinding>()?.ConfigureRuntimeSpeed(speedMultiplier);
        slime.transform.localScale *= strong ? 1.15f : weak ? 0.85f : 1f;
        SpriteRenderer sprite = slime.GetComponent<SpriteRenderer>();
        if (sprite != null)
            sprite.color = strong ? new Color(1f, 0.68f, 0.68f) :
                           weak ? new Color(0.76f, 0.91f, 1f) : Color.white;
    }

    // Cria uma cura visível no mapa enquanto não atingir o limite configurado no Inspector.
    private void SpawnHealingItem()
    {
        List<Item> candidates = GetRandomConsumablePrefabs();
        if (candidates.Count == 0) return;
        int existing = 0;
        foreach (Item item in FindObjectsByType<Item>())
            if (item.IsRuntimeSpawn && !IsCoin(item) &&
                (item.healAmount > 0 || item.staminaRestore > 0 || item.speedMultiplier > 1f)) existing++;
        if (existing >= maximumHealingItems ||
            !TryFindSpawnPosition(out Vector3 position, 2.5f, 7f, false)) return;

        Item prefab = candidates[Random.Range(0, candidates.Count)];
        if (prefab == null) return;
        GameObject pickup = Instantiate(prefab.gameObject, position, Quaternion.identity);
        Item spawnedItem = pickup.GetComponent<Item>();
        spawnedItem?.MarkRuntimeSpawn();
        RuntimeGameUI.ShowSpawnNotice(spawnedItem != null ? spawnedItem.Name : prefab.Name);
    }

    private List<Item> GetRandomConsumablePrefabs()
    {
        List<Item> result = new List<Item>();
        string[] names = { "Pão Francês", "Pão de Forma", "Poção de Cura 50PV",
            "Poção de Cura 100PV", "Poção de Estamina 50PV", "Poção de Estamina 100PV", "Açúcar" };
        if (itemDictionary == null) itemDictionary = FindAnyObjectByType<ItemDictionary>();
        if (itemDictionary != null)
            foreach (string name in names)
            {
                if (!itemDictionary.TryGetItemPrefab(name, out GameObject prefab)) continue;
                Item item = prefab != null ? prefab.GetComponent<Item>() : null;
                if (item != null) result.Add(item);
            }
        if (result.Count == 0 && healingItemPrefabs != null)
            foreach (Item item in healingItemPrefabs) if (item != null) result.Add(item);
        return result;
    }

    private void SpawnCoin()
    {
        int existing = 0;
        foreach (Item item in FindObjectsByType<Item>()) if (item.IsRuntimeSpawn && IsCoin(item)) existing++;
        if (existing >= maximumCoinsOnGround ||
            !TryFindSpawnPosition(out Vector3 position, 2f, 7f, false)) return;
        if (itemDictionary == null) itemDictionary = FindAnyObjectByType<ItemDictionary>();
        GameObject prefab = itemDictionary != null && itemDictionary.TryGetItemPrefab("Moedas", out GameObject coinPrefab)
            ? coinPrefab : null;
        if (prefab == null) return;
        GameObject spawned = Instantiate(prefab, position, Quaternion.identity);
        Item itemComponent = spawned.GetComponent<Item>();
        itemComponent?.MarkRuntimeSpawn();
        RuntimeGameUI.ShowSpawnNotice("1 moeda");
    }

    private static bool IsCoin(Item item) => item != null &&
        (item.Name.ToLowerInvariant().Contains("moeda") || item.Name.ToLowerInvariant().Contains("coin"));

    private void TrySpawnBossForNewLevels()
    {
        if (player == null || bossEveryLevels <= 0) return;
        int reached = player.Level / bossEveryLevels;
        if (reached <= lastBossLevelMilestone ||
            !TryFindSpawnPosition(out Vector3 position, 1.8f, 3f, false)) return;
        lastBossLevelMilestone = reached;
        SpawnBoss(position);
    }

    private void SpawnBoss(Vector3 position)
    {
        GameObject prefab = Random.value < 0.5f ? fireSlimePrefab : ghostSlimePrefab;
        if (prefab == null) return;
        GameObject boss = Instantiate(prefab, position, Quaternion.identity, transform);
        boss.name = "Boss Slime - " + prefab.name;
        EnemyHealth health = boss.GetComponent<EnemyHealth>();
        if (health != null) health.ConfigureAsBoss(bossBaseMultiplier * DifficultyEnemyMultiplier);
        EnemyAI ai = boss.GetComponent<EnemyAI>();
        if (ai != null) ai.ConfigureAsBoss(bossBaseMultiplier * DifficultyEnemyMultiplier);
        boss.transform.localScale *= 1.65f;
        RuntimeGameUI.ShowSpawnNotice("Boss Slime");
    }

    public void SpawnBossMinion(Vector3 nearPosition)
    {
        if (FindObjectsByType<EnemyHealth>().Length >= maximumSlimesAlive + 12) return;
        GameObject prefab = Random.value < 0.5f ? fireSlimePrefab : ghostSlimePrefab;
        if (prefab == null) return;
        Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(1.5f, 3f);
        Vector3 position = nearPosition + (Vector3)offset;
        if (blockingTiles != null && blockingTiles.HasTile(blockingTiles.WorldToCell(position)))
            return;
        GameObject minion = Instantiate(prefab, position, Quaternion.identity, transform);
        minion.GetComponent<EnemyHealth>()?.ConfigureRuntimeHealth(
            0.8f * normalSlimeBaseMultiplier * DifficultyEnemyMultiplier);
        minion.GetComponent<EnemyAI>()?.ConfigureRuntimeDamage(
            0.8f * normalSlimeBaseMultiplier * DifficultyEnemyMultiplier);
    }

    // Valida grama, margem de borda, Foreground, câmera e colisão antes de aceitar uma célula.
    private bool TryFindSpawnPosition(out Vector3 position, float minDistance,
                                      float maxDistance, bool requireOffscreen)
    {
        position = Vector3.zero;
        if (walkableGround == null) return false;
        if (gameplayCamera == null) gameplayCamera = Camera.main;

        BoundsInt groundBounds = walkableGround.cellBounds;
        for (int attempt = 0; attempt < positionAttempts * 2; attempt++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < 0.01f) continue;
            Vector3 candidate = player.transform.position +
                (Vector3)(direction * Random.Range(minDistance, maxDistance));
            Vector3Int cell = walkableGround.WorldToCell(candidate);
            if (!walkableGround.HasTile(cell) ||
                cell.x < groundBounds.xMin + borderMarginCells ||
                cell.x >= groundBounds.xMax - borderMarginCells ||
                cell.y < groundBounds.yMin + borderMarginCells ||
                cell.y >= groundBounds.yMax - borderMarginCells) continue;
            candidate = walkableGround.GetCellCenterWorld(cell);
            if (!requireOffscreen && IsCoveredByDecoration(candidate)) continue;
            if (blockingTiles != null)
            {
                Vector3Int blockedCell = blockingTiles.WorldToCell(candidate);
                if (blockingTiles.HasTile(blockedCell) ||
                    blockingTiles.HasTile(blockedCell + Vector3Int.left) ||
                    blockingTiles.HasTile(blockedCell + Vector3Int.right) ||
                    blockingTiles.HasTile(blockedCell + Vector3Int.up) ||
                    blockingTiles.HasTile(blockedCell + Vector3Int.down)) continue;
            }

            if (gameplayCamera != null)
            {
                Vector3 viewport = gameplayCamera.WorldToViewportPoint(candidate);
                bool onScreen = viewport.z > 0f && viewport.x > -0.1f && viewport.x < 1.1f &&
                                viewport.y > -0.1f && viewport.y < 1.1f;
                bool safelyVisible = viewport.z > 0f && viewport.x > 0.05f && viewport.x < 0.95f &&
                                     viewport.y > 0.08f && viewport.y < 0.92f;
                if (requireOffscreen ? onScreen : !safelyVisible) continue;
            }

            bool blocked = false;
            foreach (Collider2D collider in Physics2D.OverlapCircleAll(
                         candidate, requireOffscreen ? 1f : 0.65f))
            {
                if (!collider.isTrigger)
                {
                    blocked = true;
                    break;
                }
            }
            if (blocked) continue;

            position = new Vector3(candidate.x, candidate.y, 0f);
            return true;
        }

        return false;
    }

    private bool IsCoveredByDecoration(Vector3 position)
    {
        foreach (Bounds bounds in pickupOcclusionBounds)
            if (position.x >= bounds.min.x - 0.2f && position.x <= bounds.max.x + 0.2f &&
                position.y >= bounds.min.y - 0.2f && position.y <= bounds.max.y + 0.2f)
                return true;
        return false;
    }
}
