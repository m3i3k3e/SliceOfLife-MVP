using UnityEngine;

/// <summary>
/// Describes a generic gatherable resource (e.g., wood, ore).
/// Designers author instances as ScriptableObjects.
/// </summary>
[CreateAssetMenu(fileName = "Resource", menuName = "SliceOfLife/Resource")]
public class ResourceSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id = "resource_id"; // Stable string for save/load
    [SerializeField] private string displayName = "New Resource";
    [SerializeField] private ResourceRarity rarity = ResourceRarity.Common;

    [Header("Stacking")]
    [Tooltip("Typical stack size when represented in UI or inventory.")]
    [SerializeField] private int defaultStackSize = 1;

    /// <summary>Stable identifier used for lookups and persistence.</summary>
    public string Id => id;

    /// <summary>Human-readable name displayed in UI.</summary>
    public string DisplayName => displayName;

    /// <summary>Rarity tier which may influence drop rates or value.</summary>
    public ResourceRarity Rarity => rarity;

    /// <summary>Default number shown for a full stack of this resource.</summary>
    public int DefaultStackSize => Mathf.Max(1, defaultStackSize);
}

/// <summary>Simple rarity ladder for resources.</summary>
public enum ResourceRarity { Common, Rare, Epic }

