using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Guarda estatísticas globais e as conquistas compartilhadas entre as cinco vagas.</summary>
[Serializable]
public class PlayerProfileData
{
    public int totalRuns;
    public int easyRuns;
    public int mediumRuns;
    public int hardRuns;
    public int insaneRuns;
    public int easyWins;
    public int mediumWins;
    public int hardWins;
    public int totalCoins;
    public int totalFireSlimesKilled;
    public int totalGhostSlimesKilled;
    public int totalBossSlimesKilled;
    public int highestLevel;
    public int highestHealth;
    public int highestStamina;
    public int highestSwordDamage;
    public int highestBowDamage;
    public int highestDefense;
    public int mostCoinsInOneRun;
    public float longestSurvivalSeconds;
    public List<AchievementRecord> achievements = new List<AchievementRecord>();
    public List<RunHistoryRecord> runs = new List<RunHistoryRecord>();
}

[Serializable]
public class AchievementRecord
{
    public string id;
    public string title;
    public string unlockedAtUtc;
}

[Serializable]
public class RunHistoryRecord
{
    public string runId;
    public string playerName;
    public string difficulty;
    public string startedAtUtc;
    public string finishedAtUtc;
    public string result;
    public int slotIndex;
    public int level;
    public int maxHealth;
    public int maxStamina;
    public int currentHealth;
    public int currentStamina;
    public int swordDamage;
    public int bowDamage;
    public int defense;
    public int coins;
    public int enemiesKilled;
    public int fireSlimesKilled;
    public int ghostSlimesKilled;
    public int bossSlimesKilled;
    public float survivalSeconds;
    public bool usedIronSword;
    public bool usedBow;
    public bool usedMeleeWeapon;
    public bool finished;
}

/// <summary>
/// Persiste o histórico geral em um arquivo separado dos saves e anuncia novas conquistas para a interface.
/// </summary>
public static class PlayerProfileService
{
    public const int MaximumHistoryEntries = 500;
    public static event Action<AchievementRecord> AchievementUnlocked;

    private static PlayerProfileData profile;
    private static bool loaded;
    private static string ProfilePath => Path.Combine(Application.persistentDataPath, "onion_profile.json");

    public static PlayerProfileData Data
    {
        get
        {
            EnsureLoaded();
            return profile;
        }
    }

    public static void BeginRun(string runId, int slotIndex, string playerName,
                                GameSession.Difficulty difficulty)
    {
        EnsureLoaded();
        if (profile.runs.Exists(run => run.runId == runId)) return;

        profile.totalRuns++;
        switch (difficulty)
        {
            case GameSession.Difficulty.Easy: profile.easyRuns++; break;
            case GameSession.Difficulty.Hard: profile.hardRuns++; break;
            case GameSession.Difficulty.Insane: profile.insaneRuns++; break;
            default: profile.mediumRuns++; break;
        }

        profile.runs.Add(new RunHistoryRecord
        {
            runId = runId,
            playerName = playerName,
            difficulty = DifficultyName(difficulty),
            startedAtUtc = DateTime.UtcNow.ToString("O"),
            result = "Em andamento",
            slotIndex = slotIndex,
            level = 1,
            finished = false
        });

        TrimHistory();
        if (difficulty == GameSession.Difficulty.Easy && profile.easyRuns >= 5)
        {
            Unlock("EASY_RUNS_" + (profile.easyRuns / 5 * 5),
                   "Jogar o Fácil " + (profile.easyRuns / 5 * 5) + " vezes");
        }

        Save();
    }

    public static void RecordCoins(int amount)
    {
        if (amount <= 0) return;
        EnsureLoaded();
        int previousTotal = profile.totalCoins;
        profile.totalCoins += amount;
        int nextMilestone = (previousTotal / 50 + 1) * 50;
        for (int value = nextMilestone; value <= profile.totalCoins; value += 50)
        {
            Unlock("COINS_" + value, "Coletar " + value + " moedas");
        }

        Save();
    }

    public static void RecordEnemyKill(string kind)
    {
        EnsureLoaded();
        if (kind == "Fire")
        {
            profile.totalFireSlimesKilled++;
            UnlockKillMilestones("FIRE", "Slimes de Fogo", profile.totalFireSlimesKilled);
        }
        else if (kind == "Ghost")
        {
            profile.totalGhostSlimesKilled++;
            UnlockKillMilestones("GHOST", "Slimes Fantasma", profile.totalGhostSlimesKilled);
        }
        else if (kind == "Boss")
        {
            profile.totalBossSlimesKilled++;
        }

        Save();
    }

    public static void RecordLevelReached(int level, int maxHealth, int maxStamina)
    {
        EnsureLoaded();
        int oldHighestLevel = profile.highestLevel;
        profile.highestLevel = Mathf.Max(profile.highestLevel, level);
        profile.highestHealth = Mathf.Max(profile.highestHealth, maxHealth);
        profile.highestStamina = Mathf.Max(profile.highestStamina, maxStamina);
        int nextMilestone = (oldHighestLevel / 5 + 1) * 5;
        for (int milestone = nextMilestone; milestone <= level; milestone += 5)
        {
            Unlock("LEVEL_" + milestone, "Alcançar o nível " + milestone);
        }

        Save();
    }

    public static void SyncRun(string runId, string playerName, int slotIndex,
                               GameSession.Difficulty difficulty, float survivalSeconds,
                               int level, int maxHealth, int maxStamina, int currentHealth,
                               int currentStamina, int swordDamage,
                               int bowDamage, int defense, int coins,
                               int enemiesKilled, int fireKills, int ghostKills, int bossKills,
                               bool usedIronSword, bool usedBow, bool usedMeleeWeapon,
                               bool dead, bool won)
    {
        EnsureLoaded();
        RunHistoryRecord run = profile.runs.Find(entry => entry.runId == runId);
        if (run == null)
        {
            BeginRun(runId, slotIndex, playerName, difficulty);
            run = profile.runs.Find(entry => entry.runId == runId);
        }

        if (run == null) return;
        run.playerName = playerName;
        run.difficulty = DifficultyName(difficulty);
        run.slotIndex = slotIndex;
        run.level = Mathf.Max(run.level, level);
        run.maxHealth = Mathf.Max(run.maxHealth, maxHealth);
        run.maxStamina = Mathf.Max(run.maxStamina, maxStamina);
        run.currentHealth = currentHealth;
        run.currentStamina = currentStamina;
        run.swordDamage = Mathf.Max(run.swordDamage, swordDamage);
        run.bowDamage = Mathf.Max(run.bowDamage, bowDamage);
        run.defense = Mathf.Max(run.defense, defense);
        run.coins = coins;
        run.enemiesKilled = enemiesKilled;
        run.fireSlimesKilled = fireKills;
        run.ghostSlimesKilled = ghostKills;
        run.bossSlimesKilled = bossKills;
        run.survivalSeconds = survivalSeconds;
        run.usedIronSword |= usedIronSword;
        run.usedBow |= usedBow;
        run.usedMeleeWeapon |= usedMeleeWeapon;

        profile.highestLevel = Mathf.Max(profile.highestLevel, level);
        profile.highestHealth = Mathf.Max(profile.highestHealth, maxHealth);
        profile.highestStamina = Mathf.Max(profile.highestStamina, maxStamina);
        profile.highestSwordDamage = Mathf.Max(profile.highestSwordDamage, swordDamage);
        profile.highestBowDamage = Mathf.Max(profile.highestBowDamage, bowDamage);
        profile.highestDefense = Mathf.Max(profile.highestDefense, defense);
        profile.mostCoinsInOneRun = Mathf.Max(profile.mostCoinsInOneRun, coins);
        profile.longestSurvivalSeconds = Mathf.Max(profile.longestSurvivalSeconds, survivalSeconds);

        if (difficulty == GameSession.Difficulty.Insane && survivalSeconds >= 1200f)
        {
            Unlock("INSANE_20_MINUTES", "Sobreviver 20 minutos no Insano");
        }

        if (!run.finished && (dead || won))
        {
            run.finished = true;
            run.finishedAtUtc = DateTime.UtcNow.ToString("O");
            run.result = won ? "Vitória" : "Derrota";

            if (won)
            {
                switch (difficulty)
                {
                    case GameSession.Difficulty.Easy:
                        profile.easyWins++;
                        Unlock("WIN_EASY", "Concluir o nível Fácil");
                        break;
                    case GameSession.Difficulty.Hard:
                        profile.hardWins++;
                        Unlock("WIN_HARD", "Concluir o nível Difícil");
                        break;
                    case GameSession.Difficulty.Medium:
                        profile.mediumWins++;
                        Unlock("WIN_MEDIUM", "Concluir o nível Médio");
                        break;
                }

                if (usedIronSword) Unlock("WIN_IRON_SWORD", "Concluir um nível com espada de ferro");
                if (usedBow && !usedMeleeWeapon) Unlock("WIN_ONLY_BOW", "Concluir um nível só de arco");
            }
        }

        Save();
    }

    public static bool IsRunEnded(string runId)
    {
        EnsureLoaded();
        RunHistoryRecord run = profile.runs.Find(entry => entry.runId == runId);
        return run != null && run.finished;
    }

    public static string DifficultyName(GameSession.Difficulty difficulty)
    {
        return difficulty switch
        {
            GameSession.Difficulty.Easy => "Fácil",
            GameSession.Difficulty.Hard => "Difícil",
            GameSession.Difficulty.Insane => "Insano",
            _ => "Médio"
        };
    }

    private static void UnlockKillMilestones(string idPrefix, string title, int total)
    {
        int nextMilestone = ((total - 1) / 50 + 1) * 50;
        if (total >= nextMilestone)
        {
            Unlock(idPrefix + "_" + nextMilestone, "Matar " + nextMilestone + " " + title);
        }
    }

    private static void Unlock(string id, string title)
    {
        if (profile.achievements.Exists(entry => entry.id == id)) return;
        AchievementRecord record = new AchievementRecord
        {
            id = id,
            title = title,
            unlockedAtUtc = DateTime.UtcNow.ToString("O")
        };
        profile.achievements.Add(record);
        AchievementUnlocked?.Invoke(record);
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        try
        {
            if (File.Exists(ProfilePath))
            {
                profile = JsonUtility.FromJson<PlayerProfileData>(File.ReadAllText(ProfilePath));
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Não foi possível carregar o histórico global: " + exception.Message);
        }

        if (profile == null) profile = new PlayerProfileData();
        if (profile.achievements == null) profile.achievements = new List<AchievementRecord>();
        if (profile.runs == null) profile.runs = new List<RunHistoryRecord>();
    }

    private static void TrimHistory()
    {
        while (profile.runs.Count > MaximumHistoryEntries)
        {
            int removableIndex = profile.runs.FindIndex(entry => entry.finished);
            if (removableIndex < 0) removableIndex = 0;
            profile.runs.RemoveAt(removableIndex);
        }
    }

    private static void Save()
    {
        try
        {
            TrimHistory();
            File.WriteAllText(ProfilePath, JsonUtility.ToJson(profile, true));
        }
        catch (Exception exception)
        {
            Debug.LogError("Não foi possível salvar o histórico global: " + exception.Message);
        }
    }
}
