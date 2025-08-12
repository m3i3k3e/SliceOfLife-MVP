/*
 * ResourceManager.cs
 * Role: Maintains counts of raw resources and broadcasts changes to listeners.
 * Key dependencies: ResourceSO catalog for ID resolution.
 * Expansion: Add new ResourceSO assets and list them in resourceCatalog to support more materials.
 */
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks quantities of raw resources collected by the player.
/// Exposes simple add/consume APIs; counts reset on new session.
/// </summary>
public class ResourceManager : MonoBehaviour
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

    // ---- Persistence removed ----

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

