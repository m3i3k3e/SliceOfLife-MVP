using System;

/// <summary>
/// Central event hub for loose coupling between managers and UI.
/// Each event is exposed as a C# event with helper methods to raise it
/// so callers don't repeat null-check boilerplate.
/// </summary>
public static class GameEvents
{
    /// <summary>
    /// Fired after the day counter increments. Payload = new day index.
    /// </summary>
    public static event Action<int> DayChanged;

    /// <summary>
    /// Fired whenever dungeon key counts change. Payload = (current, perDay).
    /// </summary>
    public static event Action<int, int> DungeonKeysChanged;

    /// <summary>
    /// Fired when the Sleep gate is re-evaluated. Payload = (canSleep, reason).
    /// </summary>
    public static event Action<bool, string> SleepEligibilityChanged;

    /// <summary>
    /// Helper to invoke <see cref="DayChanged"/> safely.
    /// </summary>
    public static void RaiseDayChanged(int day) => DayChanged?.Invoke(day);

    /// <summary>
    /// Helper to invoke <see cref="DungeonKeysChanged"/> safely.
    /// </summary>
    public static void RaiseDungeonKeysChanged(int current, int perDay)
        => DungeonKeysChanged?.Invoke(current, perDay);

    /// <summary>
    /// Helper to invoke <see cref="SleepEligibilityChanged"/> safely.
    /// </summary>
    public static void RaiseSleepEligibilityChanged(bool canSleep, string reason)
        => SleepEligibilityChanged?.Invoke(canSleep, reason);
}
