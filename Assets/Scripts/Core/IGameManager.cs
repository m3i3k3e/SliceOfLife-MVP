using System.Threading.Tasks;

/// <summary>
/// Public surface for the central game orchestrator.
/// Exposes core systems and day progression without
/// tying callers to the concrete <see cref="GameManager"/>.
/// </summary>
/// <summary>
/// Public surface for the central game orchestrator.
/// Extends <see cref="ISaveable"/> so the manager can participate in persistence.
/// </summary>
public interface IGameManager : ISaveable
{
    // -------- Core systems --------
    /// <summary>Read-only access to the currency system.</summary>
    IEssenceProvider Essence { get; }

    /// <summary>Read-only access to the upgrade system.</summary>
    IUpgradeProvider Upgrades { get; }

    /// <summary>Access to station and companion collections.</summary>
    StationManager Stations { get; }

    // -------- Day progression --------
    /// <summary>Current in-game day (starts at 1).</summary>
    int Day { get; }

    /// <summary>Attempts to advance to the next day when allowed.</summary>
    bool TrySleep();

    /// <summary>Computed Sleep gate.</summary>
    bool CanSleep { get; }

    /// <summary>Reevaluate the Sleep gate and broadcast state.</summary>
    void ReevaluateSleepGate();

    // -------- Dungeon key economy --------
    /// <summary>How many keys are granted each day once unlocked.</summary>
    int DungeonKeysPerDay { get; }

    /// <summary>Keys remaining today.</summary>
    int DungeonKeysRemaining { get; }

    /// <summary>Has the player attempted a run today?</summary>
    bool DungeonAttemptedToday { get; }

    /// <summary>Consume one dungeon key if available.</summary>
    bool TryConsumeDungeonKey();

    /// <summary>Async variant that persists the change.</summary>
    Task<bool> TryConsumeDungeonKeyAsync();

    /// <summary>Mark that a dungeon attempt was made today.</summary>
    void MarkDungeonAttempted();

    /// <summary>Apply defeat repercussions immediately.</summary>
    void ApplyDungeonLossPenalty();

    /// <summary>Async variant that persists loss penalties.</summary>
    Task ApplyDungeonLossPenaltyAsync();

}

