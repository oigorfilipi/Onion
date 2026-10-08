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

    [Header("Itens de cura")]
    [SerializeField] private Item[] healingItemPrefabs;
    [SerializeField, Min(1f)] private float healingSpawnInterval = 10f;
    [SerializeField, Min(1)] private int maximumHealingItems = 8;

    private PlayerVitals player;
    private SaveController saveController;
    private PrologueChallenge challenge;
    private OnionMenuController pauseMenu;
    private Camera gameplayCamera;
    private float nextSlimeAt;
    private float nextHealingAt;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerVitals>();
        saveController = FindAnyObjectByType<SaveController>();
        challenge = FindAnyObjectByType<PrologueChallenge>();
        pauseMenu = FindAnyObjectByType<OnionMenuController>();
        gameplayCamera = Camera.main;
        nextSlimeAt = Time.time + initialSpawnInterval;
        nextHealingAt = Time.time + 4f;
    }

    private void Update()
    {
        if (player == null || player.IsDead || saveController == null ||
            !saveController.IsInitialized || GameSession.RunWon || Time.timeScale <= 0f ||
            (pauseMenu != null && pauseMenu.IsPauseOpenOrOpening)) return;

        if (Time.time >= nextSlimeAt)
        {
            SpawnSlime();
            float progress = Mathf.Clamp01(GameSession.RunElapsedSeconds /
                                           (challenge != null ? challenge.DurationSeconds : 600f));
            nextSlimeAt = Time.time + Mathf.Lerp(initialSpawnInterval, minimumSpawnInterval, progress);
        }

        if (Time.time >= nextHealingAt)
        {
            SpawnHealingItem();
            nextHealingAt = Time.time + healingSpawnInterval;
        }
    }

    // Escolhe o prefab e os multiplicadores de variante forte, normal ou fraca, respeitando o limite de vivos.
    private void SpawnSlime()
    {
        if (FindObjectsByType<EnemyHealth>().Length >= maximumSlimesAlive ||
            !TryFindSpawnPosition(out Vector3 position,
                                  minimumDistanceFromPlayer, maximumDistanceFromPlayer, true)) return;

        GameObject prefab = Random.value < 0.5f ? fireSlimePrefab : ghostSlimePrefab;
        if (prefab == null) return;
        GameObject slime = Instantiate(prefab, position, Quaternion.identity, transform);

        float progress = Mathf.Clamp01(GameSession.RunElapsedSeconds /
                                       (challenge != null ? challenge.DurationSeconds : 600f));
        float roll = Random.value;
        float strongChance = Mathf.Lerp(0.2f, 0.4f, progress);
        bool strong = roll < strongChance;
        bool weak = !strong && roll > 0.7f;
        float healthMultiplier = strong ? 1.5f : weak ? 0.7f : 1f;
        float damageMultiplier = strong ? 1.3f : weak ? 0.75f : 1f;
        float speedMultiplier = strong ? 1.15f : weak ? 0.85f : 1f;

        slime.GetComponent<EnemyHealth>()?.ConfigureRuntimeHealth(healthMultiplier);
        slime.GetComponent<EnemyAI>()?.ConfigureRuntimeDamage(damageMultiplier);
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
        if (healingItemPrefabs == null || healingItemPrefabs.Length == 0) return;
        int existing = 0;
        foreach (Item item in FindObjectsByType<Item>())
            if (item.IsRuntimeSpawn && item.healAmount > 0) existing++;
        if (existing >= maximumHealingItems ||
            !TryFindSpawnPosition(out Vector3 position, 2.5f, 7f, false)) return;

        Item prefab = healingItemPrefabs[Random.Range(0, healingItemPrefabs.Length)];
        if (prefab == null) return;
        GameObject pickup = Instantiate(prefab.gameObject, position, Quaternion.identity);
        pickup.GetComponent<Item>()?.MarkRuntimeSpawn();
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
}
