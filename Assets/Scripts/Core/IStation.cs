using System;

/// <summary>
/// Public contract for any in-game station (Farm, Kitchen, etc.).
/// Stations primarily exist as data objects but expose a minigame hook
/// and a production-complete notification.
/// </summary>
public interface IStation
{
    /// <summary>
    /// Stable identifier used for saves and lookups.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Optional minigame associated with the station.
    /// Returning null allows data-only stations during early prototypes.
    /// </summary>
    IMinigame Minigame { get; }

    /// <summary>
    /// Fired whenever the station finishes producing its output.
    /// Payload describes the result so other systems can react (grant items, etc.).
    /// </summary>
    event Action<MinigameResult> OnProductionComplete;
}
