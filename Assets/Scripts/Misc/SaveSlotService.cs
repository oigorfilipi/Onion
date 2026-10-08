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

    // Saves criados ao iniciar uma cena diretamente no Editor usavam saveData.json.
    // Ao abrir o menu, importa a tentativa para uma vaga livre sem apagar outra partida.
    public static void ImportStandaloneSave()
    {
        string legacyPath = Path.Combine(Application.persistentDataPath, "saveData.json");
        if (!File.Exists(legacyPath)) return;

        SaveData legacy;
        try { legacy = JsonUtility.FromJson<SaveData>(File.ReadAllText(legacyPath)); }
        catch (Exception exception)
        {
            Debug.LogWarning($"Save avulso não pôde ser lido: {exception.Message}");
            return;
        }
        if (legacy == null || legacy.playerDead || legacy.runWon) return;

        int matchingSlot = 0;
        int freeSlot = 0;
        for (int slot = 1; slot <= SlotCount; slot++)
        {
            string slotPath = GetSlotPath(slot);
            if (!File.Exists(slotPath))
            {
                if (freeSlot == 0) freeSlot = slot;
                continue;
            }
            try
            {
                SaveData existing = JsonUtility.FromJson<SaveData>(File.ReadAllText(slotPath));
                if (!string.IsNullOrEmpty(legacy.runId) && existing != null &&
                    existing.runId == legacy.runId)
                {
                    // Uma partida encerrada não pode ser reativada pelo arquivo avulso antigo.
                    if (existing.playerDead || existing.runWon) return;
                    matchingSlot = slot;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Vaga {slot} não pôde ser comparada: {exception.Message}");
            }
        }

        int destinationSlot = matchingSlot != 0 ? matchingSlot : freeSlot;
        if (destinationSlot == 0) return;
        string destination = GetSlotPath(destinationSlot);
        if (File.Exists(destination) &&
            File.GetLastWriteTimeUtc(destination) >= File.GetLastWriteTimeUtc(legacyPath)) return;
        try
        {
            File.Copy(legacyPath, destination, true);
            Debug.Log($"Save avulso importado para a vaga {destinationSlot}.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Não foi possível importar o save avulso: {exception.Message}");
        }
    }

    public static bool IsValidName(string playerName)
    {
        return !string.IsNullOrWhiteSpace(playerName) && playerName.Trim().Length <= 40;
    }
}
