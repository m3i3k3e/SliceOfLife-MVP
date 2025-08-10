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

    [Serializable]
    private class SaveData
    {
        public int day;
        public int essence;
        public int dailyClicksRemaining;
        public int essencePerClick;
        public float passivePerSecond;
        public List<string> purchasedUpgradeIds = new();
    }

    /// <summary>
    /// Serialize current runtime state to JSON. Static helper follows a tiny facade pattern
    /// so callers don't worry about file paths or formats.
    /// </summary>
    public static void Save(GameManager gm)
    {
        var essence = gm.Essence as EssenceManager;
        var upgrades = gm.Upgrades as UpgradeManager;

        // Build a plain data container. Using JsonUtility keeps dependencies minimal.
        var data = new SaveData
        {
            day = gm.Day,
            essence = essence.CurrentEssence,
            dailyClicksRemaining = essence.DailyClicksRemaining,
            essencePerClick = essence.EssencePerClick,
            passivePerSecond = essence.PassivePerSecond,
            purchasedUpgradeIds = new List<string>(upgrades.PurchasedIds)
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
        var data = JsonUtility.FromJson<SaveData>(json);
        var essence = gm.Essence as EssenceManager;
        var upgrades = gm.Upgrades as UpgradeManager;

        // Rehydrate state. We prefer calling public APIs so events fire consistently.
        while (essence.DailyClicksRemaining > data.dailyClicksRemaining)
            essence.TryClickHarvest(); // burn down to saved value

        // For simple fields without setters, reflection is used as a temporary bridge.
        typeof(EssenceManager).GetField("_currentEssence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(essence, data.essence);

        essence.AddEssencePerClick(data.essencePerClick - essence.EssencePerClick);
        essence.AddPassivePerSecond(data.passivePerSecond - essence.PassivePerSecond);

        // Upgrades rebuild their effects internally from purchased IDs
        upgrades.LoadPurchased(data.purchasedUpgradeIds);

        // Finally restore the day index
        typeof(GameManager).GetProperty("Day")?.SetValue(gm, data.day, null);
    }
}
