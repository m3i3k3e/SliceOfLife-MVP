/*
 * ResourceManager.cs
 * Role: Maintains counts of raw resources and broadcasts changes to listeners.
 * Key dependencies: ResourceSO catalog for ID resolution; participates in SaveSystem via ISaveable.
 * Expansion: Add new ResourceSO assets and list them in resourceCatalog to support more materials.
 */
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks quantities of raw resources collected by the player.
/// Exposes simple add/consume APIs and persists amounts via <see cref="ISaveable"/>.
/// </summary>
public class ResourceManager : MonoBehaviour, ISaveable
{
    /// <summary>Catalog of all resource definitions used to resolve IDs during load.</summary>
    [Header("Catalog")]
    [Tooltip("All resource definitions available. Used to resolve IDs during load.")]
    [SerializeField] private List<ResourceSO> resourceCatalog = new();

    /// <summary>Internal lookup from resource to owned quantity.</summary>
    private readonly Dictionary<ResourceSO, int> _counts = new();

    /// <summary>Raised whenever a resource total changes. Payload = (resource, newAmount).</summary>
    public event Action<ResourceSO, int> OnResourceChanged;

    /// <summary>Add the specified amount of a resource.</summary>
    public void AddResource(ResourceSO resource, int amount)
    {
        if (resource == null || amount <= 0) return; // invalid input guard

        int current = GetCount(resource);
        int newAmount = current + amount;
        _counts[resource] = newAmount;
        OnResourceChanged?.Invoke(resource, newAmount);
    }

    /// <summary>
    /// Try to consume a quantity of the given resource.
    /// Returns false if there isn't enough to cover the request.
    /// </summary>
    public bool TryConsumeResource(ResourceSO resource, int amount)
    {
        if (resource == null || amount <= 0) return false;
        int current = GetCount(resource);
        if (current < amount) return false; // not enough to spend

        int newAmount = current - amount;
        if (newAmount > 0)
            _counts[resource] = newAmount;
        else
            _counts.Remove(resource); // keep dictionary tidy

        OnResourceChanged?.Invoke(resource, newAmount);
        return true;
    }

    /// <summary>Get the current quantity for a resource.</summary>
    public int GetCount(ResourceSO resource)
    {
        if (resource == null) return 0;
        return _counts.TryGetValue(resource, out int value) ? value : 0;
    }

    // ---- Persistence ----
    public string SaveKey => "Resources"; // unique section name for SaveSystem

    [Serializable]
    private class ResourceStack
    {
        public string id;
        public int quantity;
    }

    [Serializable]
    private class SaveData
    {
        public List<ResourceStack> resources = new();
    }

    /// <summary>Package current resource counts into a serializable container.</summary>
    public object ToData()
    {
        var data = new SaveData();
        foreach (var kvp in _counts)
        {
            data.resources.Add(new ResourceStack
            {
                id = kvp.Key ? kvp.Key.Id : string.Empty,
                quantity = kvp.Value
            });
        }
        return data;
    }

    /// <summary>Restore resource amounts from serialized data.</summary>
    public void LoadFrom(object data)
    {
        _counts.Clear();
        var d = data as SaveData;
        if (d == null) return; // nothing to load

        foreach (var entry in d.resources)
        {
            var res = FindResource(entry.id);
            if (res != null)
                _counts[res] = entry.quantity;
        }

        // notify listeners so UI refreshes after load
        foreach (var kvp in _counts)
            OnResourceChanged?.Invoke(kvp.Key, kvp.Value);
    }

    /// <summary>Lookup helper to resolve a resource ID from the catalog.</summary>
    private ResourceSO FindResource(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < resourceCatalog.Count; i++)
        {
            var res = resourceCatalog[i];
            if (res != null && res.Id == id) return res;
        }
        return null;
    }
}

