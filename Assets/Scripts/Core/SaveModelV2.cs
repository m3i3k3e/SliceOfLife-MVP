using System;
using System.Collections.Generic;

/// <summary>
/// Strongly typed root save model for the new persistence system.
/// Each field maps to concrete runtime data so we can avoid
/// the old stringly-typed section approach.
/// </summary>
[Serializable]
public class SaveModelV2
{
    /// <summary>Schema version so future migrations know how to parse the file.</summary>
    public const int Version = 2;

    /// <summary>Persisted copy for JSON; defaults to current <see cref="Version"/>.</summary>
    public int version = Version;

    // ----- Meta -----
    public string lastScene;
    public string spawnPointId;

    // ----- Day / Rule tracking -----
    public int day;
    public int dungeonKeysRemaining;
    public int dungeonKeysPerDay;
    public int tempNextDayClickDebuff;

    // ----- Currency -----
    public int essence;
    public int dailyClicksRemaining;
    public int essencePerClick;
    public float passivePerSecond;

    // ----- Upgrades -----
    public List<string> purchasedUpgradeIds = new();

    // ----- Inventory -----
    public List<ItemStackDTO> inventory = new();

    // ----- Tasks -----
    public List<TaskStateDTO> tasks = new();

    // ----- World flags -----
    public float altarPolishProgress;
    public bool familiarAwakened;
    public bool dungeonUnlocked;

    /// <summary>Simple ID+quantity pair used for inventory serialization.</summary>
    [Serializable]
    public class ItemStackDTO
    {
        public string itemId;
        public int qty;
    }

    /// <summary>Serializable snapshot of one task's progress.</summary>
    [Serializable]
    public class TaskStateDTO
    {
        public string taskId;
        public bool completed;
        public int progress;
    }
}

