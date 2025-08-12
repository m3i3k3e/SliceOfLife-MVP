using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Simple JSON persistence built around <see cref="SaveModelV2"/>.
/// Uses Unity's built-in <see cref="JsonUtility"/> since data volume is tiny.
/// </summary>
public static class SaveSystem
{
    private const string FileName = "save.json";

    /// <summary>Full path to the save file in Unity's persistent data location.</summary>
    private static string PathToFile => System.IO.Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>Does a save file exist on disk?</summary>
    public static bool HasAnySave() => File.Exists(PathToFile);

    /// <summary>Delete the current save if it exists.</summary>
    public static void Delete()
    {
        try
        {
            if (File.Exists(PathToFile)) File.Delete(PathToFile);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to delete save file: {ex}");
        }
    }

    /// <summary>
    /// Serialize runtime state to disk.
    /// </summary>
    public static void Save(GameManager gm)
    {
        if (gm == null) return;
        var model = BuildModel(gm);
        WriteToDisk(model);
    }

    /// <summary>
    /// Load state from disk, migrating v1 data if encountered, and apply to managers.
    /// Returns the loaded model for convenience.
    /// </summary>
    public static SaveModelV2 Load(GameManager gm)
    {
        var model = new SaveModelV2();
        if (gm == null) return model;
        var path = PathToFile;

        if (!File.Exists(path))
        {
            ApplyModel(gm, model);
            return model; // nothing to load yet
        }

        try
        {
            var json = File.ReadAllText(path);
            model = JsonUtility.FromJson<SaveModelV2>(json);

            if (model == null || model.version != SaveModelV2.Version)
            {
                // Old schema detected — attempt migration from GameSaveData sections.
                var legacy = JsonUtility.FromJson<GameSaveData>(json);
                model = MigrateFromLegacy(legacy);
                WriteToDisk(model); // persist upgraded schema
                Debug.Log("Migrated legacy save to SaveModelV2");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load save file: {ex}");
            model = new SaveModelV2();
        }

        ApplyModel(gm, model);
        return model;
    }

    /// <summary>Gather data from registered systems into a <see cref="SaveModelV2"/>.</summary>
    private static SaveModelV2 BuildModel(GameManager gm)
    {
        var model = new SaveModelV2();
        // GameManager captures its own fields first.
        gm.Capture(model);

        // Then ask each registered participant to append their data.
        var participants = gm.SaveParticipants;
        for (int i = 0; i < participants.Count; i++)
            participants[i]?.Capture(model);

        return model;
    }

    /// <summary>Apply a loaded model to all registered systems.</summary>
    private static void ApplyModel(GameManager gm, SaveModelV2 model)
    {
        gm?.Apply(model);

        var participants = gm?.SaveParticipants;
        if (participants != null)
        {
            for (int i = 0; i < participants.Count; i++)
                participants[i]?.Apply(model);
        }

        gm?.ReevaluateSleepGate();
    }

    /// <summary>Write the model to disk as JSON.</summary>
    private static void WriteToDisk(SaveModelV2 model)
    {
        var path = PathToFile;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var json = JsonUtility.ToJson(model, prettyPrint: true);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to write save file: {ex}");
        }
    }

    /// <summary>
    /// Convert legacy <see cref="GameSaveData"/> into the new <see cref="SaveModelV2"/>.
    /// Only maps sections that existed in v1; new fields fall back to defaults.
    /// </summary>
    private static SaveModelV2 MigrateFromLegacy(GameSaveData legacy)
    {
        var model = new SaveModelV2();
        if (legacy == null) return model;

        // Game section -> day
        var gameJson = legacy.GetSection("Game");
        var game = string.IsNullOrEmpty(gameJson) ? null : JsonUtility.FromJson<GameManager.GameData>(gameJson);
        model.day = game?.day ?? 1;

        // Essence section
        var essenceJson = legacy.GetSection("Essence");
        var ess = string.IsNullOrEmpty(essenceJson) ? null : JsonUtility.FromJson<EssenceManager.SaveData>(essenceJson);
        if (ess != null)
        {
            model.essence = ess.currentEssence;
            model.dailyClicksRemaining = ess.dailyClicksRemaining;
            model.essencePerClick = ess.essencePerClick;
            model.passivePerSecond = ess.passivePerSecond;
        }

        // Inventory section
        var invJson = legacy.GetSection("Inventory");
        var inv = string.IsNullOrEmpty(invJson) ? null : JsonUtility.FromJson<InventoryManager.SaveData>(invJson);
        if (inv != null)
        {
            foreach (var s in inv.items)
                model.inventory.Add(new SaveModelV2.ItemStackDTO { itemId = s.itemId, qty = s.quantity });
        }

        // Upgrades section
        var upJson = legacy.GetSection("Upgrades");
        var up = string.IsNullOrEmpty(upJson) ? null : JsonUtility.FromJson<UpgradeManager.SaveData>(upJson);
        if (up != null)
        {
            model.purchasedUpgradeIds.AddRange(up.purchasedUpgradeIds);
            model.dungeonUnlocked = up.purchasedUpgradeIds.Contains(UpgradeIds.UnlockBattle);
        }

        return model;
    }
}

