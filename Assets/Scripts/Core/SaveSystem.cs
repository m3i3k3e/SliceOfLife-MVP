using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Small JSON saver for MVP. No external packages required.
/// Data volume is tiny, so JsonUtility is fine (no dictionaries).
/// </summary>
public static class SaveSystem
{
    private const string FileName = "save.json";
    private const int Version = 3; // Bump this when save schema changes

    /// <summary>
    /// Serialize current runtime state to JSON asynchronously. Callers can await this task
    /// if they need confirmation that disk I/O has completed.
    /// </summary>
    public static async Task SaveAsync(GameManager gm)
    {
        var essence = gm.Essence as EssenceManager;
        var upgrades = gm.Upgrades as UpgradeManager;
        var stations = gm.Stations;
        var inventory = gm.Inventory as InventoryManager;

        // Build a plain data container. Using JsonUtility keeps dependencies minimal.
        var data = new GameSaveData
        {
            version = Version, // Stamp the schema version so we can migrate later
            Game = gm.ToData(),
            Essence = essence.ToData(),
            Upgrades = upgrades.ToData(),
            Inventory = inventory != null ? inventory.ToData() : new GameSaveData.InventoryData(),
        };

        if (stations != null)
        {
            var sd = stations.ToData();
            data.Stations = sd.Stations;
            data.Companions = sd.Companions;
        }

        var json = JsonUtility.ToJson(data, prettyPrint: true);

        // Build the full file path inside Unity's persistent storage.
        var path = Path.Combine(Application.persistentDataPath, FileName);

        // Ensure the directory exists before writing. Safe to call even if it already exists.
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        try
        {
            // Asynchronously write JSON to disk. Any IO issue gets logged instead of crashing the game.
            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to write save file: {ex}");
        }
    }

    /// <summary>
    /// Legacy synchronous wrapper so existing callers don't need to care about tasks.
    /// </summary>
    public static void Save(GameManager gm) => SaveAsync(gm).GetAwaiter().GetResult();

    /// <summary>
    /// Attempt to load saved JSON and apply it to runtime objects asynchronously.
    /// Returns a default <see cref="GameSaveData"/> if loading fails for any reason.
    /// </summary>
    public static async Task<GameSaveData> LoadAsync(GameManager gm)
    {
        var path = Path.Combine(Application.persistentDataPath, FileName);

        // Bail out early if no save exists yet.
        if (!File.Exists(path)) return new GameSaveData();

        try
        {
            // Asynchronously read JSON off disk. Exceptions are caught below.
            var json = await File.ReadAllTextAsync(path);

            // Deserialize into our DTO; if parsing somehow returns null, fall back to defaults.
            var data = JsonUtility.FromJson<GameSaveData>(json) ?? new GameSaveData();

            // Rehydrate systems only after we have valid data.
            var essence = gm.Essence as EssenceManager;
            var upgrades = gm.Upgrades as UpgradeManager;
            var stations = gm.Stations;

            gm.LoadFrom(data.Game);
            essence?.LoadFrom(data.Essence);
            upgrades?.LoadFrom(data.Upgrades);
            stations?.LoadFrom(data.Stations, data.Companions);
            var inventory = gm.Inventory as InventoryManager;
            inventory?.LoadFrom(data.Inventory);

            // Ensure Sleep gate reflects restored state
            gm.ReevaluateSleepGate();

            return data;
        }
        catch (Exception ex)
        {
            // Any failure (bad JSON, IO errors) returns fresh defaults and leaves runtime untouched.
            Debug.LogError($"Failed to load save file: {ex}");
            return new GameSaveData();
        }
    }

    /// <summary>
    /// Legacy synchronous wrapper for code that hasn't adopted async yet.
    /// </summary>
    public static GameSaveData Load(GameManager gm) => LoadAsync(gm).GetAwaiter().GetResult();
}

/// <summary>
/// Plain data-transfer object representing all persistent game state.
/// Nested records keep JsonUtility serialization simple and explicit.
/// </summary>
[Serializable]
public class GameSaveData
{
    /// <summary>
    /// Schema version so future migrations know how to interpret the data.
    /// </summary>
    public int version = 3;

    public EssenceData Essence = new();
    public UpgradeData Upgrades = new();
    public GameData Game = new();
    public StationData Stations = new();
    public CompanionData Companions = new();
    public InventoryData Inventory = new();

    [Serializable]
    public class EssenceData
    {
        public int currentEssence;
        public int dailyClicksRemaining;
        public int essencePerClick;
        public float passivePerSecond;
    }

    [Serializable]
    public class UpgradeData
    {
        public List<string> purchasedUpgradeIds = new();
    }

    [Serializable]
    public class GameData
    {
        public int day;
    }

    [Serializable]
    public class StationData
    {
        // Store station IDs so we can rebuild unlocked state on load.
        public List<string> unlockedStationIds = new();
    }

    [Serializable]
    public class CompanionData
    {
        [Serializable]
        public class Assignment
        {
            public string companionId;
            public string stationId; // null/empty means unassigned
        }

        public List<Assignment> assignments = new();
    }

    [Serializable]
    public class InventoryData
    {
        public int unlockedRows;
        public List<ItemStack> items = new();
    }

    [Serializable]
    public class ItemStack
    {
        public string itemId;
        public int quantity;
    }
}
