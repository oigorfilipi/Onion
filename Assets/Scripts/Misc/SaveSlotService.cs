using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Resolve os caminhos das cinco vagas e valida dados básicos do menu de saves.
/// </summary>
public static class SaveSlotService
{
    public const int SlotCount = 5;

    public static string GetSlotPath(int slotIndex)
    {
        if (slotIndex < 1 || slotIndex > SlotCount)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex));
        }

        return Path.Combine(Application.persistentDataPath, $"save_slot_{slotIndex}.json");
    }

    public static bool TryRead(int slotIndex, out SaveData saveData)
    {
        saveData = null;
        string path = GetLoadPath(slotIndex);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            return saveData != null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Não foi possível ler o save da vaga {slotIndex}: {exception.Message}");
            return false;
        }
    }

    public static bool Exists(int slotIndex)
    {
        return File.Exists(GetLoadPath(slotIndex));
    }

    public static string GetLoadPath(int slotIndex)
    {
        string slotPath = GetSlotPath(slotIndex);
        string oldSavePath = Path.Combine(Application.persistentDataPath, "saveData.json");
        return slotIndex == 1 && !File.Exists(slotPath) && File.Exists(oldSavePath)
            ? oldSavePath
            : slotPath;
    }

    public static bool IsValidName(string playerName)
    {
        return !string.IsNullOrWhiteSpace(playerName) && playerName.Trim().Length <= 40;
    }
}
