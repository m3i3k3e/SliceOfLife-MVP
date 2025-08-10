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

    // ---- ICompanion implementation ----

    /// <inheritdoc />
    public string Id => id;

    /// <inheritdoc />
    public string DisplayName => displayName;

    /// <inheritdoc />
    public IStation AssignedStation => startingStation;
}
