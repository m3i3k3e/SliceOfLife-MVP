using System;

/// <summary>
/// Defines the events and raiser methods for cross-system communication.
/// Decouples managers and UI from a concrete event bus implementation.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Fired after the day counter increments. Payload = new day index.
    /// </summary>
    event Action<int> DayChanged;

    /// <summary>
    /// Fired whenever dungeon key counts change. Payload = (current, perDay).
    /// </summary>
    event Action<int, int> DungeonKeysChanged;

    /// <summary>
    /// Fired when the Sleep gate is re-evaluated. Payload = (canSleep, reason).
    /// </summary>
    event Action<bool, string> SleepEligibilityChanged;

    /// <summary>
    /// Fired when a station is unlocked. Payload = unlocked station.
    /// </summary>
    event Action<IStation> StationUnlocked;

    /// <summary>
    /// Fired when a companion is recruited. Payload = companion data.
    /// </summary>
    event Action<ICompanion> CompanionRecruited;

    /// <summary>
    /// Fired whenever an upgrade is successfully purchased. Payload = upgrade data.
    /// Lets UI refresh when new systems or locations unlock.
    /// </summary>
    event Action<UpgradeSO> UpgradePurchased;

    /// <summary>
    /// Fired after any minigame finishes. Payload = result details.
    /// </summary>
    event Action<MinigameResult> MinigameCompleted;

    /// <summary>
    /// Helper to invoke <see cref="DayChanged"/> safely.
    /// </summary>
    void RaiseDayChanged(int day);

    /// <summary>
    /// Helper to invoke <see cref="DungeonKeysChanged"/> safely.
    /// </summary>
    void RaiseDungeonKeysChanged(int current, int perDay);

    /// <summary>
    /// Helper to invoke <see cref="SleepEligibilityChanged"/> safely.
    /// </summary>
    void RaiseSleepEligibilityChanged(bool canSleep, string reason);

    /// <summary>
    /// Helper to invoke <see cref="StationUnlocked"/> safely.
    /// </summary>
    void RaiseStationUnlocked(IStation station);

    /// <summary>
    /// Helper to invoke <see cref="CompanionRecruited"/> safely.
    /// </summary>
    void RaiseCompanionRecruited(ICompanion companion);

    /// <summary>
    /// Helper to invoke <see cref="UpgradePurchased"/> safely.
    /// </summary>
    void RaiseUpgradePurchased(UpgradeSO upgrade);

    /// <summary>
    /// Helper to invoke <see cref="MinigameCompleted"/> safely.
    /// </summary>
    void RaiseMinigameCompleted(MinigameResult result);
}
