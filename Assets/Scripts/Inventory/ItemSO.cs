using UnityEngine;

/// <summary>
/// Basic inventory item definition. ScriptableObject allows designers to author
/// data without touching code. Only the fields required by InventoryManager live here
/// for now: an identifier, a display title, and a stack size limit.
/// </summary>
[CreateAssetMenu(fileName = "Item", menuName = "SliceOfLife/Item")]
public class ItemSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id = "item_id"; // Stable ID for saves & lookups
    [SerializeField] private string title = "New Item"; // Human readable name

    [Header("Stacking")]
    [Tooltip("Maximum quantity allowed per inventory stack.")]
    [SerializeField] private int stackSize = 1;

    /// <summary>Stable identifier used for save/load.</summary>
    public string Id => id;
    /// <summary>Title shown in UI.</summary>
    public string Title => title;
    /// <summary>Largest number allowed in a single slot. Clamped to at least one.</summary>
    public int StackSize => Mathf.Max(1, stackSize);
}
