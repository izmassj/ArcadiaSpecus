using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class BunkerSaveSystem
{
    private const string SaveFolderName = "BunkerSaves";

    public static string GetSaveFolderPath()
    {
        string path = Path.Combine(Application.persistentDataPath, SaveFolderName);
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }

    public static string GetSaveFilePath(string slotId)
    {
        return Path.Combine(GetSaveFolderPath(), $"{slotId}.json");
    }

    public static BunkerSaveFileData CreateNewSlot(string displayName, BunkerGameMode mode)
    {
        string safeName = string.IsNullOrWhiteSpace(displayName) ? "Partida" : displayName.Trim();
        string slotId = Guid.NewGuid().ToString("N");
        long now = DateTime.UtcNow.Ticks;

        BunkerSaveFileData saveFile = new BunkerSaveFileData();
        saveFile.metadata.slotId = slotId;
        saveFile.metadata.displayName = safeName;
        saveFile.metadata.gameMode = (int)mode;
        saveFile.metadata.createdUtcTicks = now;
        saveFile.metadata.updatedUtcTicks = now;

        Save(saveFile);
        return saveFile;
    }

    public static void Save(BunkerSaveFileData saveFile)
    {
        if (saveFile == null || saveFile.metadata == null || string.IsNullOrWhiteSpace(saveFile.metadata.slotId))
            return;

        UpdateMetadataPreview(saveFile);
        saveFile.metadata.updatedUtcTicks = DateTime.UtcNow.Ticks;

        string json = JsonUtility.ToJson(saveFile, true);
        File.WriteAllText(GetSaveFilePath(saveFile.metadata.slotId), json);
    }

    public static BunkerSaveFileData Load(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            return null;

        string path = GetSaveFilePath(slotId);
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonUtility.FromJson<BunkerSaveFileData>(json);
    }

    public static List<BunkerSaveMetadata> LoadAllMetadata()
    {
        List<BunkerSaveMetadata> result = new List<BunkerSaveMetadata>();
        string folder = GetSaveFolderPath();
        string[] files = Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly);

        for (int i = 0; i < files.Length; i++)
        {
            try
            {
                string json = File.ReadAllText(files[i]);
                BunkerSaveFileData saveFile = JsonUtility.FromJson<BunkerSaveFileData>(json);
                if (saveFile != null && saveFile.metadata != null)
                    result.Add(saveFile.metadata);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"No se pudo leer la partida '{files[i]}'. {exception.Message}");
            }
        }

        result.Sort((a, b) => b.updatedUtcTicks.CompareTo(a.updatedUtcTicks));
        return result;
    }

    public static void Delete(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            return;

        string path = GetSaveFilePath(slotId);
        if (File.Exists(path))
            File.Delete(path);
    }

    private static void UpdateMetadataPreview(BunkerSaveFileData saveFile)
    {
        if (saveFile == null || saveFile.metadata == null || saveFile.world == null)
            return;

        if (saveFile.world.resources != null)
        {
            saveFile.metadata.previewScrap = saveFile.world.resources.scrap;
            saveFile.metadata.previewElectricity = saveFile.world.resources.electricity;
            saveFile.metadata.previewWater = saveFile.world.resources.water;
            saveFile.metadata.previewFood = saveFile.world.resources.food;
        }

        if (saveFile.world.dayCycle != null)
            saveFile.metadata.previewDay = saveFile.world.dayCycle.currentDay;

        saveFile.metadata.previewNpcCount = saveFile.world.npcs != null ? saveFile.world.npcs.Count : 0;
    }
}
