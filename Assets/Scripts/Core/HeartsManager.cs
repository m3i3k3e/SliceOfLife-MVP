using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maintains relationship heart totals for each companion.
/// Data lives here rather than on the companion assets so we can
/// save/load runtime progress cleanly.
/// </summary>
public class HeartsManager : MonoBehaviour, ISaveParticipant
{
    [Header("Catalog")]
    [Tooltip("All companions available. Used to resolve IDs during load.\n" +
             "\u26a0\ufe0f New CompanionSO assets MUST be added here.")]
    [SerializeField] private List<CompanionSO> companionCatalog = new();

    /// <summary>Lookup from companion ID to asset for fast resolution.</summary>
    private readonly Dictionary<string, CompanionSO> _catalogById = new();

    /// <summary>Runtime heart totals keyed by companion asset.</summary>
    private readonly Dictionary<CompanionSO, int> _hearts = new();

    /// <summary>Raised whenever a companion's heart total changes.</summary>
    public event Action<CompanionSO, int> OnHeartsChanged;

    private void Awake()
    {
        // Pre-build the ID lookup so load-time resolution is O(1).
        BuildCatalogLookup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Unity calls this in the editor when values change; rebuild the cache.
        BuildCatalogLookup();
    }
#endif

    /// <summary>Populate the ID-to-asset lookup from the serialized catalog list.</summary>
    private void BuildCatalogLookup()
    {
        _catalogById.Clear();
        for (int i = 0; i < companionCatalog.Count; i++)
        {
            var comp = companionCatalog[i];
            if (comp == null || string.IsNullOrEmpty(comp.Id)) continue;

            // Skip duplicates but warn so designers can fix the data.
            if (_catalogById.ContainsKey(comp.Id))
            {
                Debug.LogWarning($"Duplicate companion id '{comp.Id}' in HeartsManager catalog.");
                continue;
            }

            _catalogById[comp.Id] = comp;
        }
    }

    /// <summary>
    /// Add hearts to a companion's total.
    /// </summary>
    public void AddHearts(CompanionSO companion, int amount)
    {
        if (companion == null || amount == 0) return; // ignore bad input

        int current = GetHearts(companion);
        int newTotal = Mathf.Max(0, current + amount); // prevent negatives
        _hearts[companion] = newTotal;
        OnHeartsChanged?.Invoke(companion, newTotal);
        // Request a save so progress persists without hammering disk writes.
        SaveScheduler.RequestSave(GameManager.Instance);
    }

    /// <summary>Get the current heart total for a companion.</summary>
    public int GetHearts(CompanionSO companion)
    {
        if (companion == null) return 0;
        return _hearts.TryGetValue(companion, out int value) ? value : 0;
    }

    // ---- ISaveParticipant implementation ----

    /// <summary>Write heart totals into the aggregated save model.</summary>
    public void Capture(SaveModelV2 model)
    {
        if (model == null) return;
        foreach (var kvp in _hearts)
        {
            string id = kvp.Key ? kvp.Key.Id : string.Empty;
            if (string.IsNullOrEmpty(id)) continue;
            model.companionHearts[id] = kvp.Value;
        }
    }

    /// <summary>Restore heart totals from the save model.</summary>
    public void Apply(SaveModelV2 model)
    {
        _hearts.Clear();
        if (model?.companionHearts == null) return;

        foreach (var kvp in model.companionHearts)
        {
            var comp = FindCompanion(kvp.Key);
            if (comp == null) continue; // ignore unknown ids
            _hearts[comp] = kvp.Value;
            OnHeartsChanged?.Invoke(comp, kvp.Value); // notify listeners for UI refresh
        }
    }

    /// <summary>Resolve a companion asset by its stable string ID.</summary>
    private CompanionSO FindCompanion(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _catalogById.TryGetValue(id, out var comp) ? comp : null;
    }
}
