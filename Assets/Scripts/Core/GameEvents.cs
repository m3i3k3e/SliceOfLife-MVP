using System;

/// <summary>
/// Simple static event bus so disparate systems can talk
/// without hard references to one another.
/// </summary>
public static class GameEvents
{
    // --- Day cycle events ---

    /// <summary>
    /// Fired after a successful sleep when the day counter increments.
    /// Payload: new day number.
    /// </summary>
    public static event Action<int> DayChanged;

    /// <summary>
    /// Publish a day change event.
    /// </summary>
    public static void RaiseDayChanged(int day)
        => DayChanged?.Invoke(day);

    // --- Dungeon key events ---

    /// <summary>
    /// Fired whenever the player's dungeon keys change.
    /// Payload: (current keys, keys granted per day).
    /// </summary>
    public static event Action<int, int> DungeonKeysChanged;

    /// <summary>
    /// Publish a dungeon key change event.
    /// </summary>
    public static void RaiseDungeonKeysChanged(int current, int perDay)
        => DungeonKeysChanged?.Invoke(current, perDay);

    // --- Sleep gate events ---

    /// <summary>
    /// Fired when the sleep eligibility state changes.
    /// Payload: (canSleep flag, reason string when false).
    /// </summary>
    public static event Action<bool, string> SleepEligibilityChanged;

    /// <summary>
    /// Publish the latest sleep eligibility state.
    /// </summary>
    public static void RaiseSleepEligibilityChanged(bool canSleep, string reason)
        => SleepEligibilityChanged?.Invoke(canSleep, reason);
}

