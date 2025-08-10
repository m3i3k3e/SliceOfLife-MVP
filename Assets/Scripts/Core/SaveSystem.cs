using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Small JSON saver for MVP. No external packages required.
/// Data volume is tiny, so JsonUtility is fine (no dictionaries).
/// </summary>
public static class SaveSystem
{
    private const string FileName = "save.json";
    private const int Version = 1; // Bump this when save schema changes

    /// <summary>
    /// Serialize current runtime state to JSON. Static helper follows a tiny facade pattern
    /// so callers don't worry about file paths or formats.
    /// </summary>
    public static void Save(GameManager gm)
    {
        var essence = gm.Essence as EssenceManager;
        var upgrades = gm.Upgrades as UpgradeManager;

        // Build a plain data container. Using JsonUtility keeps dependencies minimal.
        var data = new GameSaveData
        {
            version = Version, // Stamp the schema version so we can migrate later
            Game = gm.ToData(),
            Essence = essence.ToData(),
            Upgrades = upgrades.ToData()
        };

        var json = JsonUtility.ToJson(data, prettyPrint: true);

        // Build the full file path inside Unity's persistent storage.
        var path = Path.Combine(Application.persistentDataPath, FileName);

        // Ensure the directory exists before writing. Safe to call even if it already exists.
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        try
        {
            // Write JSON to disk. Any IO issue gets logged instead of crashing the game.
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to write save file: {ex}");
        }
    }

    /// <summary>
    /// Attempt to load saved JSON and apply it to runtime objects.
    /// Returns a default <see cref="GameSaveData"/> if loading fails for any reason.
    /// </summary>
    public static GameSaveData Load(GameManager gm)
    {
        var path = Path.Combine(Application.persistentDataPath, FileName);

        // Bail out early if no save exists yet.
        if (!File.Exists(path)) return new GameSaveData();

        try
        {
            // Read JSON off disk. Exceptions (missing file, locked file, etc.) are caught below.
            var json = File.ReadAllText(path);

            // Deserialize into our DTO; if parsing somehow returns null, fall back to defaults.
            var data = JsonUtility.FromJson<GameSaveData>(json) ?? new GameSaveData();

            // Rehydrate systems only after we have valid data.
            var essence = gm.Essence as EssenceManager;
            var upgrades = gm.Upgrades as UpgradeManager;

            gm.LoadFrom(data.Game);
            essence?.LoadFrom(data.Essence);
            upgrades?.LoadFrom(data.Upgrades);

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
    public int version = 1;

    public EssenceData Essence = new();
    public UpgradeData Upgrades = new();
    public GameData Game = new();

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
}
