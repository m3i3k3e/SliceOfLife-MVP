/// <summary>
/// Public contract for any in-game station (Farm, Kitchen, etc.).
/// Stations primarily exist as data objects but expose a minigame hook.
/// </summary>
public interface IStation
{
    /// <summary>Stable identifier used for saves and lookups.</summary>
    string Id { get; }

    /// <summary>Display name shown in UI.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Optional minigame associated with the station.
    /// Returning null allows data-only stations during early prototypes.
    /// </summary>
    IMinigame Minigame { get; }
}
