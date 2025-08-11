using System;

/// <summary>
/// Minimal surface for inventory interactions. Exposed by <see cref="InventoryManager"/>
/// so other systems can manipulate items without referencing the concrete class.
/// </summary>
public interface IInventoryService
{
    /// <summary>Maximum number of slots currently available.</summary>
    int Capacity { get; }

    /// <summary>Attempt to add items; returns false if insufficient space.</summary>
    bool TryAdd(ItemSO item, int quantity);

    /// <summary>Attempt to remove items; returns false if insufficient quantity.</summary>
    bool TryRemove(ItemSO item, int quantity);

    /// <summary>Current quantity of a specific item across all stacks.</summary>
    int GetCount(ItemSO item);

    /// <summary>Raised whenever the inventory contents change.</summary>
    event Action OnInventoryChanged;
}
