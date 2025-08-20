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

    // ----- Resources -----
    public List<ResourceStackDTO> resources = new();

    // ----- Recipes -----
    public List<string> unlockedRecipeIds = new();

    // ----- Skills -----
    public List<string> unlockedSkillIds = new();

    // ----- Stations & Companions -----
    public List<string> unlockedStationIds = new();
    public Dictionary<string, string> companionAssignments = new();
    /// <summary>Heart totals per companion, keyed by companion ID.</summary>
    public Dictionary<string, int> companionHearts = new();

    // ----- Dungeon -----
    public int currentFloor;
    public int maxFloorReached;
    public List<int> unlockedDungeonMilestones = new();

    /// <summary>Simple ID+quantity pair used for inventory serialization.</summary>
    [Serializable]
    public class ItemStackDTO
    {
        public string itemId;
        public int qty;
    }

    /// <summary>Simple ID+quantity pair used for resource serialization.</summary>
    [Serializable]
    public class ResourceStackDTO
    {
        public string resourceId;
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

