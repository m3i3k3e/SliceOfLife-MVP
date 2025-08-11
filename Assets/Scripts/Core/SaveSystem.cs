using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Small JSON saver for MVP. No external packages required.
/// Data volume is tiny, so JsonUtility is fine. Each saveable system contributes
/// a JSON section keyed by name, enabling new systems without touching this class.
/// </summary>
public static class SaveSystem
{
    private const string FileName = "save.json";
    // Version is baked into the save file so future migrations can detect
    // outdated data layouts. Adding new saveable sections warrants a bump.
    private const int Version = 6; // v6 adds Resources, Recipes and Dungeon sections

    /// <summary>
    /// Serialize current runtime state to JSON asynchronously. Callers can await this task
    /// if they need confirmation that disk I/O has completed.
    /// </summary>
    public static async Task SaveAsync(GameManager gm)
    {
        // Build a plain data container. Using JsonUtility keeps dependencies minimal.
        var data = new GameSaveData { version = Version };

        // Iterate all registered saveables and let each contribute its section.
        foreach (var saveable in gm.Saveables)
        {
            var payload = saveable.ToData();
            var jsonSection = JsonUtility.ToJson(payload);
            data.SetSection(saveable.SaveKey, jsonSection);
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
            foreach (var saveable in gm.Saveables)
            {
                var sectionJson = data.GetSection(saveable.SaveKey);
                if (string.IsNullOrEmpty(sectionJson)) continue;

                // Use ToData() to discover the expected type for deserialization.
                var type = saveable.ToData().GetType();
                var payload = JsonUtility.FromJson(sectionJson, type);
                saveable.LoadFrom(payload);
            }

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
