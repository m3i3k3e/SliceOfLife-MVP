/// <summary>
/// Contract for a recruitable companion who can manage a station.
/// This keeps gameplay code decoupled from specific companion implementations.
/// </summary>
public interface ICompanion
{
    /// <summary>Stable identifier for save data and lookups.</summary>
    string Id { get; }

    /// <summary>Player-facing name.</summary>
    string DisplayName { get; }

    /// <summary>The station this companion manages (can be null for unassigned).</summary>
    IStation AssignedStation { get; }
}
