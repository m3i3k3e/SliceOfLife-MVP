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
    // Increment this when the save structure changes. Allows us to migrate old
    // saves in the future without silently breaking players.
    private const int CurrentVersion = 1;

    [Serializable]
    private class SaveData
    {
        // Bump when fields are added/removed/changed. Simple int keeps JSON light.
        public int version;
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

        // Gather state into a serializable bag. Version stamped so migrations
        // know what schema they are dealing with.
        var data = new SaveData
        {
            version = CurrentVersion,
            day = gm.Day,
            essence = essence.CurrentEssence,
            dailyClicksRemaining = essence.DailyClicksRemaining,
            essencePerClick = essence.EssencePerClick,
            passivePerSecond = essence.PassivePerSecond,
            purchasedUpgradeIds = new List<string>(upgrades.PurchasedIds)
        };

        var json = JsonUtility.ToJson(data, prettyPrint: true);
        var path = Path.Combine(Application.persistentDataPath, FileName);

        try
        {
            // File IO can fail (disk full, permissions, etc.) so we guard it.
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Save failed at {path}: {ex.Message}");
        }
    }

    public static void Load(GameManager gm)
    {
        var path = Path.Combine(Application.persistentDataPath, FileName);
        if (!File.Exists(path))
        {
            // No save yet: create one so future Save() overwrites a valid file.
            Debug.LogWarning("Save file missing. Creating default save.");
            Save(gm);
            return;
        }

        SaveData data;
        try
        {
            // Both read and JSON parse can throw, so we catch once.
            var json = File.ReadAllText(path);
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Load failed at {path}: {ex.Message}. Rebuilding default save.");
            // Fallback: create clean file from current (startup) state.
            Save(gm);
            return; // Bail; game already has defaults set in GameManager ctor.
        }

        if (data.version != CurrentVersion)
        {
            // For now we just warn; future versions could migrate here.
            Debug.LogWarning($"Loading save version {data.version}, expected {CurrentVersion}.");
        }

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
