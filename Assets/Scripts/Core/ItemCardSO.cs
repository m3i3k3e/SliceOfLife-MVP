using UnityEngine;

/// <summary>
/// Combines card presentation with inventory item data.
/// </summary>
[CreateAssetMenu(fileName = "ItemCard", menuName = "SliceOfLife/Item Card")]
public class ItemCardSO : CardSO
{
    [Header("Item")]
    [SerializeField] private ItemType _itemType = ItemType.Material; // broad category for gameplay logic
    [SerializeField] private ItemRarity _rarity = ItemRarity.Common; // drop rate / value tier
    [Tooltip("Maximum number of this item per inventory stack. Use 1 for non-stackable items.")]
    [SerializeField] private int _stackSize = 1;

    /// <summary>Category classification for this item.</summary>
    public ItemType ItemType => _itemType;

    /// <summary>Rarity tier which may influence drop rates or value.</summary>
    public ItemRarity Rarity => _rarity;

    /// <summary>Maximum quantity allowed in a single inventory slot.</summary>
    public int StackSize => Mathf.Max(1, _stackSize);

    /// <summary>Expose card identifier for save/load lookups.</summary>
    public string Id => id;
}

/// <summary>High level buckets for different item behaviors.</summary>
public enum ItemType { Material, Consumable, Quest }

/// <summary>Simplified rarity ladder used by loot tables.</summary>
public enum ItemRarity { Common, Rare, Epic }
