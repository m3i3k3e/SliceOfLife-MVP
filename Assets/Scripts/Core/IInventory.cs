using System;

/// <summary>
/// Minimal surface for inventory interactions.
/// </summary>
public interface IInventory
{
    /// <summary>Maximum number of slots available.</summary>
    int Capacity { get; }

    /// <summary>Attempt to add items; returns false if insufficient space.</summary>
    bool TryAdd(ItemCardSO item, int quantity);

    /// <summary>Attempt to remove items; returns false if insufficient quantity.</summary>
    bool TryRemove(ItemCardSO item, int quantity);

    /// <summary>Current quantity of a specific item across all stacks.</summary>
    int GetCount(ItemCardSO item);

    /// <summary>Raised whenever the inventory contents change.</summary>
    event Action OnInventoryChanged;
}
