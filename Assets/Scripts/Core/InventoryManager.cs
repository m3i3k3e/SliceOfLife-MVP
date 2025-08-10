using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime container for item stacks. Offers basic add/remove/query APIs and fires
/// <see cref="OnInventoryChanged"/> whenever contents mutate.
/// </summary>
public class InventoryManager : MonoBehaviour, IInventory, ISaveable
{
    private const int SlotsPerRow = 10; // Each row exposes 10 slots in the dungeon bar

    [Header("Catalog")]
    [Tooltip("All item definitions available. Used to resolve IDs during load.")]
    [SerializeField] private List<ItemCardSO> itemCatalog = new();

    [Header("Progression")]
    [Tooltip("How many rows of slots are currently unlocked (1-5).")]
    [SerializeField, Range(1,5)] private int unlockedRows = 1;

    /// <summary>Simple serializable container representing one occupied slot.</summary>
    [Serializable]
    private class Slot
    {
        public ItemCardSO item;
        public int quantity;

        public Slot(ItemCardSO item, int qty)
        {
            this.item = item;
            quantity = qty;
        }
    }

    // Internal list of item stacks. Order doesn't matter yet.
    private readonly List<Slot> _slots = new();

    /// <inheritdoc />
    public int Capacity => unlockedRows * SlotsPerRow;

    /// <inheritdoc />
    public event Action OnInventoryChanged;

    /// <inheritdoc />
    public bool TryAdd(ItemCardSO item, int quantity)
    {
        if (item == null || quantity <= 0) return false;

        int remaining = quantity;

        // Fill existing stacks first to maximize space usage
        for (int i = 0; i < _slots.Count && remaining > 0; i++)
        {
            var slot = _slots[i];
            if (slot.item != item) continue;
            if (slot.quantity >= item.StackSize) continue;

            int space = item.StackSize - slot.quantity;
            int toAdd = Mathf.Min(space, remaining);
            slot.quantity += toAdd;
            remaining -= toAdd;
        }

        // Create new stacks while we have room and items left
        while (remaining > 0 && _slots.Count < Capacity)
        {
            int toAdd = Mathf.Min(item.StackSize, remaining);
            _slots.Add(new Slot(item, toAdd));
            remaining -= toAdd;
        }

        bool success = remaining == 0;
        if (success) OnInventoryChanged?.Invoke();
        return success;
    }

    /// <inheritdoc />
    public bool TryRemove(ItemCardSO item, int quantity)
    {
        if (item == null || quantity <= 0) return false;
        if (GetCount(item) < quantity) return false; // not enough to remove

        int remaining = quantity;
        for (int i = _slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var slot = _slots[i];
            if (slot.item != item) continue;

            int take = Mathf.Min(slot.quantity, remaining);
            slot.quantity -= take;
            remaining -= take;

            if (slot.quantity <= 0)
                _slots.RemoveAt(i); // remove empty stacks
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <inheritdoc />
    public int GetCount(ItemCardSO item)
    {
        if (item == null) return 0;
        int total = 0;
        foreach (var slot in _slots)
            if (slot.item == item)
                total += slot.quantity;
        return total;
    }

    // ----- Persistence -----

    // ---- ISaveable implementation ----

    /// <summary>Key used in the save file for inventory data.</summary>
    public string SaveKey => "Inventory";

    /// <summary>
    /// Extract plain data for JSON serialization.
    /// </summary>
    public object ToData()
    {
        var data = new SaveData
        {
            unlockedRows = unlockedRows,
        };

        foreach (var slot in _slots)
        {
            data.items.Add(new ItemStack
            {
                itemId = slot.item ? slot.item.Id : string.Empty,
                quantity = slot.quantity
            });
        }

        return data;
    }

    /// <summary>
    /// Rebuild runtime state from serialized data.
    /// </summary>
    public void LoadFrom(object data)
    {
        var d = data as SaveData;
        _slots.Clear();
        if (d == null) return;

        unlockedRows = Mathf.Clamp(d.unlockedRows, 1, 5);

        foreach (var stack in d.items)
        {
            var item = FindItem(stack.itemId);
            if (item != null)
                _slots.Add(new Slot(item, stack.quantity));
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>Serializable representation of the inventory grid.</summary>
    [Serializable]
    public class SaveData
    {
        public int unlockedRows;
        public List<ItemStack> items = new();
    }

    /// <summary>Simple ID/quantity pair for one slot.</summary>
    [Serializable]
    public class ItemStack
    {
        public string itemId;
        public int quantity;
    }

    /// <summary>Lookup helper to resolve an item ID from the catalog.</summary>
    private ItemCardSO FindItem(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < itemCatalog.Count; i++)
        {
            var item = itemCatalog[i];
            if (item != null && item.Id == id) return item;
        }
        return null;
    }
}
