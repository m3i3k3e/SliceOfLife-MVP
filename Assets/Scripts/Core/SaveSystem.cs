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

    public static void Save(GameManager gm)
    {
        var essence = gm.Essence as EssenceManager;
        var upgrades = gm.Upgrades as UpgradeManager;

        var data = new SaveData
        {
            game = new GameSaveData
            {
                day = gm.Day,
                dungeonKeysRemaining = gm.DungeonKeysRemaining,
                dungeonAttemptedToday = gm.DungeonAttemptedToday,
                tempNextDayClickDebuff = gm.TempNextDayClickDebuff
            },
            essence = new EssenceSaveData
            {
                currentEssence = essence.CurrentEssence,
                dailyClicksRemaining = essence.DailyClicksRemaining,
                essencePerClick = essence.EssencePerClick,
                passivePerSecond = essence.PassivePerSecond
            },
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

        // Let each system rehydrate itself so events fire correctly.
        essence?.Load(data.essence);
        upgrades?.LoadPurchased(data.purchasedUpgradeIds);
        gm.Load(data.game);
    }
}
