using UnityEngine;

/// <summary>
/// Data container describing a single station (Farm, Kitchen...).
/// Designers create assets of this type and assign a mini-game implementation.
/// </summary>
[CreateAssetMenu(fileName = "Station", menuName = "SliceOfLife/Station")]
public class StationSO : ScriptableObject, IStation
{
    [Header("Identity")]
    [SerializeField] private string id = "station_id";
    [SerializeField] private string displayName = "New Station";

    [Header("Mini-Game")]
    [Tooltip("Optional ScriptableObject implementing IMinigame.")]
    [SerializeReference] private ScriptableObject minigameAsset;

    // ---- IStation implementation ----

    /// <inheritdoc />
    public string Id => id;

    /// <inheritdoc />
    public string DisplayName => displayName;

    /// <inheritdoc />
    // Cast the asset to IMinigame so callers can run PlayAsync.
    public IMinigame Minigame => minigameAsset as IMinigame;
}
