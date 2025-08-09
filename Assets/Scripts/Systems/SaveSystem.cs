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

    public static void Save(GameManager gm)
    {
        var essence = gm.Essence as EssenceManager;
        var upgrades = gm.Upgrades as UpgradeManager;

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

    public static void Load(GameManager gm)
    {
        var path = Path.Combine(Application.persistentDataPath, FileName);
        if (!File.Exists(path)) return;

        var json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<SaveData>(json);
        var essence = gm.Essence as EssenceManager;
        var upgrades = gm.Upgrades as UpgradeManager;

        // Rehydrate state. Note we call the same methods that runtime uses,
        // so we don't accidentally bypass any logic.
        while (essence.DailyClicksRemaining > data.dailyClicksRemaining)
            essence.TryClickHarvest(); // crude but keeps events consistent

        // Directly set fields that are safe to assign (or extend EssenceManager with setters)
        typeof(EssenceManager).GetField("_currentEssence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(essence, data.essence);

        essence.AddEssencePerClick(data.essencePerClick - essence.EssencePerClick);
        essence.AddPassivePerSecond(data.passivePerSecond - essence.PassivePerSecond);

        upgrades.LoadPurchased(data.purchasedUpgradeIds);

        // Day last (fires event if you add one in GameManager.Load in future)
        typeof(GameManager).GetProperty("Day")?.SetValue(gm, data.day, null);
    }
}
