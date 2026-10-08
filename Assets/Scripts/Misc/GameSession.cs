using System;
using UnityEngine;

/// <summary>
/// Mantém dificuldade, tempo e informações transitórias necessárias entre cenas da mesma tentativa.
/// </summary>
public static class GameSession
{
    public enum Difficulty
    {
        Medium = 0,
        Easy = 1,
        Hard = 2,
        Insane = 3
    }

    public enum StartMode
    {
        None,
        NewGame,
        Continue,
        SceneTransition,
        Restart
    }

    private static StartMode pendingMode;
    private static int pendingSlot;
    private static string pendingName;
    private static string pendingSpawnId;

    public static string CurrentSavePath { get; private set; }
    public static float RunElapsedSeconds { get; private set; }
    public static bool RunWon { get; private set; }
    public static int ClaimedNpcRewards { get; private set; }
    public static float NextArrowRefillAt { get; private set; }
    public static string CurrentRunId { get; private set; }
    public static int RunCoins { get; private set; }
    public static int RunEnemiesKilled { get; private set; }
    public static int RunFireSlimesKilled { get; private set; }
    public static int RunGhostSlimesKilled { get; private set; }
    public static int RunBossSlimesKilled { get; private set; }
    public static bool RunUsedIronSword { get; private set; }
    public static bool RunUsedBow { get; private set; }
    public static bool RunUsedMeleeWeapon { get; private set; }
    public static int CurrentSlotIndex { get; private set; } = 1;
    public static event Action ProgressChanged;
    public static Difficulty CurrentDifficulty { get; private set; } = Difficulty.Medium;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        pendingMode = StartMode.None;
        pendingSlot = 0;
        pendingName = null;
        pendingSpawnId = null;
        CurrentSavePath = null;
        CurrentSlotIndex = 1;
        CurrentDifficulty = Difficulty.Medium;
        ResetRunProgress();
    }

    public static void ResetRunProgress()
    {
        CurrentRunId = Guid.NewGuid().ToString("N");
        RunElapsedSeconds = 0f;
        RunWon = false;
        ClaimedNpcRewards = 0;
        NextArrowRefillAt = 0f;
        RunCoins = 0;
        RunEnemiesKilled = 0;
        RunFireSlimesKilled = 0;
        RunGhostSlimesKilled = 0;
        RunBossSlimesKilled = 0;
        RunUsedIronSword = false;
        RunUsedBow = false;
        RunUsedMeleeWeapon = false;
        ProgressChanged?.Invoke();
    }

    public static void RestoreRunProgress(float elapsedSeconds, bool won, int claimedRewards,
                                          float nextArrowRefillAt, int coins = 0,
                                          int enemiesKilled = 0, int fireSlimesKilled = 0,
                                          int ghostSlimesKilled = 0, int bossSlimesKilled = 0,
                                          bool usedIronSword = false, bool usedBow = false,
                                          bool usedMeleeWeapon = false, string runId = null,
                                          int slotIndex = 1)
    {
        CurrentRunId = string.IsNullOrEmpty(runId) ? Guid.NewGuid().ToString("N") : runId;
        RunElapsedSeconds = Mathf.Max(0f, elapsedSeconds);
        RunWon = won;
        ClaimedNpcRewards = claimedRewards;
        NextArrowRefillAt = Mathf.Max(0f, nextArrowRefillAt);
        RunCoins = Mathf.Max(0, coins);
        RunEnemiesKilled = Mathf.Max(0, enemiesKilled);
        RunFireSlimesKilled = Mathf.Max(0, fireSlimesKilled);
        RunGhostSlimesKilled = Mathf.Max(0, ghostSlimesKilled);
        RunBossSlimesKilled = Mathf.Max(0, bossSlimesKilled);
        RunUsedIronSword = usedIronSword;
        RunUsedBow = usedBow;
        RunUsedMeleeWeapon = usedMeleeWeapon;
        CurrentSlotIndex = Mathf.Clamp(slotIndex, 1, SaveSlotService.SlotCount);
    }

    public static void AddRunTime(float seconds)
    {
        if (!RunWon) RunElapsedSeconds += Mathf.Max(0f, seconds);
    }

    public static void MarkRunWon()
    {
        RunWon = true;
        ProgressChanged?.Invoke();
    }

    public static void AddCoins(int amount)
    {
        if (amount <= 0) return;
        RunCoins += amount;
        PlayerProfileService.RecordCoins(amount);
        ProgressChanged?.Invoke();
    }

    public static void RecordEnemyKill(string kind)
    {
        RunEnemiesKilled++;
        if (kind == "Fire") RunFireSlimesKilled++;
        else if (kind == "Ghost") RunGhostSlimesKilled++;
        else if (kind == "Boss") RunBossSlimesKilled++;
        PlayerProfileService.RecordEnemyKill(kind);
        ProgressChanged?.Invoke();
    }

    public static void RecordWeaponAttack(bool usedBow, bool usedIronSword = false)
    {
        if (usedBow) RunUsedBow = true;
        else RunUsedMeleeWeapon = true;
        if (usedIronSword) RunUsedIronSword = true;
        ProgressChanged?.Invoke();
    }

    public static bool HasClaimedNpcReward(NpcRole role) =>
        (ClaimedNpcRewards & (1 << (int)role)) != 0;

    public static void ClaimNpcReward(NpcRole role)
    {
        ClaimedNpcRewards |= 1 << (int)role;
        ProgressChanged?.Invoke();
    }

    public static void SetNextArrowRefillAt(float elapsedSeconds)
    {
        NextArrowRefillAt = Mathf.Max(0f, elapsedSeconds);
        ProgressChanged?.Invoke();
    }

    public static void SetDifficulty(int value)
    {
        CurrentDifficulty = value >= (int)Difficulty.Medium && value <= (int)Difficulty.Insane
            ? (Difficulty)value : Difficulty.Medium;
    }

    // Guarda a escolha do menu para o SaveController da próxima cena criar a tentativa correta.
    public static void ChooseNewGame(int slotIndex, string playerName, Difficulty difficulty)
    {
        SetDifficulty((int)difficulty);
        pendingMode = StartMode.NewGame;
        pendingSlot = slotIndex;
        pendingName = playerName;
        pendingSpawnId = null;
        CurrentSavePath = SaveSlotService.GetSlotPath(slotIndex);
        CurrentSlotIndex = Mathf.Clamp(slotIndex, 1, SaveSlotService.SlotCount);
    }

    public static void ChooseContinue(int slotIndex)
    {
        pendingMode = StartMode.Continue;
        pendingSlot = slotIndex;
        pendingName = null;
        pendingSpawnId = null;
        CurrentSavePath = SaveSlotService.GetLoadPath(slotIndex);
        CurrentSlotIndex = Mathf.Clamp(slotIndex, 1, SaveSlotService.SlotCount);
    }

    public static void ChooseSceneTransition(string spawnId)
    {
        pendingMode = StartMode.SceneTransition;
        pendingSpawnId = spawnId;
    }

    public static void ChooseRestart(string playerName)
    {
        pendingMode = StartMode.Restart;
        pendingName = playerName;
        pendingSpawnId = null;
    }

    public static void SetCurrentSavePath(string path)
    {
        CurrentSavePath = path;
    }

    // Entrega e limpa o pedido pendente uma vez para evitar reaplicar o mesmo modo em outra cena.
    public static bool TryConsume(out StartMode mode, out int slotIndex, out string playerName,
                                  out string spawnId)
    {
        mode = pendingMode;
        slotIndex = pendingSlot;
        playerName = pendingName;
        spawnId = pendingSpawnId;
        pendingMode = StartMode.None;
        pendingSlot = 0;
        pendingName = null;
        pendingSpawnId = null;
        return mode != StartMode.None;
    }
}
