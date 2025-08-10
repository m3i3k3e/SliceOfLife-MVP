using UnityEngine;

/// <summary>
/// Describes a location the player can travel to via the world map.
/// Data lives in a ScriptableObject so designers can author locations
/// without touching code.
/// </summary>
[CreateAssetMenu(fileName = "Location", menuName = "SliceOfLife/Location")]
public class LocationSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id = "location_id";     // unique string for save lookups
    [SerializeField] private string displayName = "New Location"; // label shown on the map

    [Header("Scene")]
    [Tooltip("Exact Unity scene name to load when this location is selected.")]
    [SerializeField] private string sceneName = "Start";

    [Header("Unlocking")]
    [Tooltip("Upgrade ID required to show this location. Leave empty for always available.")]
    [SerializeField] private string unlockUpgradeId = string.Empty;

    /// <summary>External read-only access to the location's string ID.</summary>
    public string Id => id;

    /// <summary>Human-readable name used by UI.</summary>
    public string DisplayName => displayName;

    /// <summary>Scene name loaded when this location button is clicked.</summary>
    public string SceneName => sceneName;

    /// <summary>Upgrade required to unlock. Empty string means unlocked from start.</summary>
    public string UnlockUpgradeId => unlockUpgradeId;
}
