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
    public static Difficulty CurrentDifficulty { get; private set; } = Difficulty.Medium;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        pendingMode = StartMode.None;
        pendingSlot = 0;
        pendingName = null;
        pendingSpawnId = null;
        CurrentSavePath = null;
        CurrentDifficulty = Difficulty.Medium;
        ResetRunProgress();
    }

    public static void ResetRunProgress()
    {
        RunElapsedSeconds = 0f;
        RunWon = false;
        ClaimedNpcRewards = 0;
        NextArrowRefillAt = 0f;
    }

    public static void RestoreRunProgress(float elapsedSeconds, bool won, int claimedRewards,
                                          float nextArrowRefillAt)
    {
        RunElapsedSeconds = Mathf.Max(0f, elapsedSeconds);
        RunWon = won;
        ClaimedNpcRewards = claimedRewards;
        NextArrowRefillAt = Mathf.Max(0f, nextArrowRefillAt);
    }

    public static void AddRunTime(float seconds)
    {
        if (!RunWon) RunElapsedSeconds += Mathf.Max(0f, seconds);
    }

    public static void MarkRunWon() => RunWon = true;

    public static bool HasClaimedNpcReward(NpcRole role) =>
        (ClaimedNpcRewards & (1 << (int)role)) != 0;

    public static void ClaimNpcReward(NpcRole role) =>
        ClaimedNpcRewards |= 1 << (int)role;

    public static void SetNextArrowRefillAt(float elapsedSeconds) =>
        NextArrowRefillAt = Mathf.Max(0f, elapsedSeconds);

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
    }

    public static void ChooseContinue(int slotIndex)
    {
        pendingMode = StartMode.Continue;
        pendingSlot = slotIndex;
        pendingName = null;
        pendingSpawnId = null;
        CurrentSavePath = SaveSlotService.GetLoadPath(slotIndex);
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
