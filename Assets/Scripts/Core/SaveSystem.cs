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
            Game = gm.ToData(),
            Essence = essence.ToData(),
            Upgrades = upgrades.ToData()
        };

        var json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, FileName), json);
    }

    /// <summary>
    /// Load JSON off disk and rehydrate runtime objects. Mirrors the Save() shape so future
    /// changes stay symmetric.
    /// </summary>
    public static void Load(GameManager gm)
    {
        var path = Path.Combine(Application.persistentDataPath, FileName);
        if (!File.Exists(path)) return; // nothing saved yet

        var json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<GameSaveData>(json);
        var essence = gm.Essence as EssenceManager;
        var upgrades = gm.Upgrades as UpgradeManager;

        gm.LoadFrom(data.Game);
        essence.LoadFrom(data.Essence);
        upgrades.LoadFrom(data.Upgrades);

        // Ensure Sleep gate reflects restored state
        gm.ReevaluateSleepGate();
    }
}

/// <summary>
/// Plain data-transfer object representing all persistent game state.
/// Nested records keep JsonUtility serialization simple and explicit.
/// </summary>
[Serializable]
public class GameSaveData
{
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
