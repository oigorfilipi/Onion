using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Descreve os campos serializáveis gravados em cada arquivo JSON de save.
/// </summary>
[System.Serializable]
public class SaveData
{
    public string sceneName;
    public Vector3 playerPosition;
    public bool hasVitalsData;
    public string playerName;
    public int playerHealth;
    public int playerStamina;
    public int playerLevel;
    public int playerExperience;
    public float remainingFireImmunity;
    public float remainingPoisonImmunity;
    public int difficulty;
    public float runElapsedSeconds;
    public bool runWon;
    public bool playerDead;
    public string runId;
    public int runCoins;
    public int runEnemiesKilled;
    public int runFireSlimesKilled;
    public int runGhostSlimesKilled;
    public int runBossSlimesKilled;
    public bool runUsedIronSword;
    public bool runUsedBow;
    public bool runUsedMeleeWeapon;
    public List<QuickbarSaveData> quickbarSaveData;
    public int claimedNpcRewards;
    public float nextArrowRefillAt;
    public List<InventorySaveData> inventorySaveData;
    public List<EquipmentSaveData> equipmentSaveData;
    public List<InventorySaveData> backpackSaveData;
    public List<string> collectedWorldItems;
    public List<string> defeatedEnemies;
    public List<PendingRewardDrop> pendingRewardDrops;
}

[System.Serializable]
public class QuickbarSaveData
{
    public int itemID;
    public int slotIndex;
    public int quantity;
}

/// <summary>Registra um presente de NPC deixado no chão porque o inventário estava cheio.</summary>
[System.Serializable]
public class PendingRewardDrop
{
    public string id;
    public string sceneName;
    public string itemName;
    public int quantity;
    public Vector3 position;
}

/// <summary>Guarda o ID do item e o tipo de slot em que ele estava equipado.</summary>
[System.Serializable]
public class EquipmentSaveData
{
    public int itemID;
    public EquipmentSlotType slotType;
}
