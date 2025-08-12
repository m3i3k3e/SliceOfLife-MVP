/*
 * InventoryManager.cs
 * Purpose: Maintains the player's inventory at runtime with add/remove/query APIs.
 * Dependencies: ItemSO definitions, IInventoryService surface, participates in save/load via ISaveParticipant.
 * Expansion Hooks: OnInventoryChanged event for UI; slot data structure allows future sorting.
 * Rationale: Implements interfaces so other systems talk to abstractions instead of concrete types.
 */
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime container for item stacks. Offers basic add/remove/query APIs and fires
/// <see cref="OnInventoryChanged"/> whenever contents mutate.
/// </summary>
/// <remarks>
/// Implements <see cref="IInventoryService"/> and participates in save/load via
/// <see cref="ISaveParticipant"/> to keep the persistence layer decoupled.
/// </remarks>
public class InventoryManager : MonoBehaviour, IInventoryService, ISaveParticipant
{
    private const int SlotsPerRow = 10; // Each row exposes 10 slots in the dungeon bar

    [Header("Catalog")]
    [Tooltip("All item definitions available. Used to resolve IDs during load.")]
    [SerializeField] private List<ItemSO> itemCatalog = new();

    // Cache mapping item IDs to their definitions for O(1) lookups.
    // Populated at runtime in <see cref="Awake"/> and rebuilt in-editor
    // via <see cref="OnValidate"/> when the catalog list changes.
    private readonly Dictionary<string, ItemSO> _catalogLookup = new();

    [Header("Progression")]
    [Tooltip("How many rows of slots are currently unlocked (1-5).")]
    [SerializeField, Range(1,5)] private int unlockedRows = 1;

    /// <summary>Simple serializable container representing one occupied slot.</summary>
    [Serializable]
    private class Slot
    {
        public ItemSO item;
        public int quantity;

        public Slot(ItemSO item, int qty)
        {
            this.item = item;
            quantity = qty;
        }
    }

    // Internal list of item stacks. Order doesn't matter yet.
    private readonly List<Slot> _slots = new();

    /// <inheritdoc />
    public int Capacity => unlockedRows * SlotsPerRow;

    /// <summary>Raised whenever inventory contents change.</summary>
    public event Action OnInventoryChanged;

    // No bridging needed; other systems subscribe directly to OnInventoryChanged.

    /// <summary>
    /// Unity lifecycle: build the item lookup cache once when the object awakens.
    /// </summary>
    private void Awake()
    {
        BuildCatalogLookup();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only: rebuild the lookup when values change in the inspector so
    /// runtime lookups stay in sync with the serialized catalog.
    /// </summary>
    private void OnValidate()
    {
        BuildCatalogLookup();
    }
#endif

    /// <summary>
    /// Populate <see cref="_catalogLookup"/> from <see cref="itemCatalog"/>.
    /// </summary>
    private void BuildCatalogLookup()
    {
        _catalogLookup.Clear(); // start fresh each time

        // Walk the serialized list once and map IDs to their assets
        foreach (var item in itemCatalog)
        {
            if (item == null || string.IsNullOrEmpty(item.Id)) continue; // skip invalid entries
            _catalogLookup[item.Id] = item; // later entries overwrite earlier ones
        }
    }

    /// <summary>
    /// Attempts to add an item stack, merging into existing stacks before creating new ones.
    /// Returns <c>true</c> only if the entire quantity fits.
    /// </summary>
    public bool TryAdd(ItemSO item, int quantity)
    {
        if (item == null || quantity <= 0) return false;

        int remaining = quantity;

        // Fill existing stacks first to maximize space usage
        for (int i = 0; i < _slots.Count && remaining > 0; i++)
        {
            var slot = _slots[i];
            if (slot.item != item) continue;           // skip non-matching stacks
            if (slot.quantity >= item.StackSize) continue; // stack already full

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

    /// <summary>
    /// Attempts to remove items, failing if insufficient quantity exists.
    /// Returns <c>true</c> when the full amount was removed.
    /// </summary>
    public bool TryRemove(ItemSO item, int quantity)
    {
        if (item == null || quantity <= 0) return false;
        if (GetCount(item) < quantity) return false; // not enough to remove

        int remaining = quantity;
        // Iterate backwards so RemoveAt doesn't skip elements
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

    /// <summary>Total quantity of the specified item across all stacks.</summary>
    public int GetCount(ItemSO item)
    {
        if (item == null) return 0;
        int total = 0;
        foreach (var slot in _slots)
            if (slot.item == item)
                total += slot.quantity; // accumulate across stacks
        return total;
    }

    // ----- Persistence via SaveModelV2 -----

    /// <summary>
    /// Apply loaded inventory state from the aggregate save model.
    /// </summary>
    public void ApplyLoadedState(SaveModelV2 data)
    {
        _slots.Clear();
        if (data == null) return;

        foreach (var stack in data.inventory)
        {
            var item = FindItem(stack.itemId);
            if (item != null)
                _slots.Add(new Slot(item, stack.qty));
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>Lookup helper to resolve an item ID from the catalog.</summary>
    private ItemSO FindItem(string id)
    {
        if (string.IsNullOrEmpty(id)) return null; // no key to search

        // Dictionary lookup avoids O(n) scans of the catalog list
        _catalogLookup.TryGetValue(id, out var item);
        return item;
    }

    // ---- ISaveParticipant implementation ----

    /// <summary>
    /// Push current inventory contents into the save model.
    /// </summary>
    public void Capture(SaveModelV2 model)
    {
        if (model == null) return;
        foreach (var slot in _slots)
        {
            model.inventory.Add(new SaveModelV2.ItemStackDTO
            {
                itemId = slot.item ? slot.item.Id : string.Empty,
                qty = slot.quantity
            });
        }
    }

    /// <summary>
    /// Rebuild inventory state from the save model.
    /// </summary>
    public void Apply(SaveModelV2 model)
    {
        ApplyLoadedState(model);
    }
}
