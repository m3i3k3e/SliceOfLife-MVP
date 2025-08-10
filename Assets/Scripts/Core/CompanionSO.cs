using UnityEngine;

/// <summary>
/// Data asset for a recruitable companion who may manage a station.
/// Keeping companion data in a ScriptableObject simplifies iteration.
/// </summary>
[CreateAssetMenu(fileName = "Companion", menuName = "SliceOfLife/Companion")]
public class CompanionSO : ScriptableObject, ICompanion
{
    [Header("Identity")]
    [SerializeField] private string id = "companion_id";
    [SerializeField] private string displayName = "New Companion";

    [Header("Assignment")]
    [Tooltip("Station this companion manages at start (optional).")]
    [SerializeField] private StationSO startingStation;

    // Runtime-assigned station. Not serialized so the asset remains a pure data container.
    // Defaults to startingStation when the asset is enabled.
    [System.NonSerialized] private StationSO _assignedStation;

    private void OnEnable()
    {
        // Reset runtime state each time the asset is loaded.
        _assignedStation = startingStation;
    }

    // ---- ICompanion implementation ----

    /// <inheritdoc />
    public string Id => id;

    /// <inheritdoc />
    public string DisplayName => displayName;

    /// <inheritdoc />
    public IStation AssignedStation => _assignedStation;

    /// <summary>
    /// Starting assignment exposed for save/load defaults.
    /// </summary>
    public StationSO StartingStation => startingStation;

    /// <summary>
    /// Assign this companion to a station at runtime.
    /// </summary>
    public void SetAssignedStation(StationSO station)
    {
        // Allow null to represent an unassigned state.
        _assignedStation = station;
    }
}
