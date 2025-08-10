using UnityEngine;

/// <summary>
/// Describes a stackable inventory item. Designers create instances as ScriptableObjects.
/// </summary>
[CreateAssetMenu(fileName = "Item", menuName = "SliceOfLife/Item")]
public class ItemSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id = "item_id"; // Unique string used for save/load lookups

    [SerializeField] private string displayName = "New Item";

    [Header("Stacking")]
    [Tooltip("Maximum number of this item per inventory stack.\nUse 1 for non-stackable items.")]
    [SerializeField] private int maxStack = 1;

    /// <summary>Stable identifier for persistence and lookups.</summary>
    public string Id => id;

    /// <summary>Human-readable name shown in UI.</summary>
    public string DisplayName => displayName;

    /// <summary>Largest quantity allowed in a single slot.</summary>
    public int MaxStack => Mathf.Max(1, maxStack);
}

