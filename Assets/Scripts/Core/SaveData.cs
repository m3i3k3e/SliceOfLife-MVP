using System;
using System.Collections.Generic;

/// <summary>
/// DTOs (Data Transfer Objects) describing the persistent state of core systems.
/// Keeping them explicit avoids reflection-heavy save logic and clarifies what we persist.
/// </summary>
[Serializable]
public class EssenceSaveData
{
    // Essence currently held by the player.
    public int currentEssence;

    // Remaining manual clicks for the current day.
    public int dailyClicksRemaining;

    // How much essence each click yields (after upgrades).
    public int essencePerClick;

    // Passive essence generated per second.
    public float passivePerSecond;
}

[Serializable]
public class GameSaveData
{
    // Current day in the long-term progression.
    public int day;

    // How many dungeon keys the player still has for today.
    public int dungeonKeysRemaining;

    // Has the player attempted the dungeon at least once today?
    public bool dungeonAttemptedToday;

    // Temporary click-cap reduction queued for the next day after a loss.
    public int tempNextDayClickDebuff;
}

[Serializable]
public class SaveData
{
    public EssenceSaveData essence = new();
    public GameSaveData game = new();

    // Upgrade IDs already purchased by the player.
    public List<string> purchasedUpgradeIds = new();
}
