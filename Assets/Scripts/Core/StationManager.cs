/*
 * StationManager.cs
 * Role: Central registry for stations and companions, decoupling runtime systems from ScriptableObject implementations.
 * Key dependencies: StationSO and CompanionSO assets; GameManager event bus for unlock notifications.
 * Expansion: Implement additional station types by extending IStation and creating new StationSO assets.
 */
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Payload broadcast when a companion gets recruited.
/// Bundles the companion reference and any starting data so
/// other systems can initialize immediately without extra lookups.
/// </summary>
public readonly struct CompanionRecruitedPayload
{
    /// <summary>The companion that was just recruited.</summary>
    public ICompanion Companion { get; }

    /// <summary>
    /// ID of the station the companion is assigned to.
    /// Null when freshly recruited and not yet placed.
    /// </summary>
    public string StationId { get; }

    /// <summary>Starting battle cards granted by the companion.</summary>
    public IReadOnlyList<CardSO> Cards { get; }

    /// <summary>Passive upgrades granted by the companion.</summary>
    public IReadOnlyList<UpgradeSO> Upgrades { get; }

    /// <summary>
    /// Construct the payload with all relevant recruitment data.
    /// </summary>
    public CompanionRecruitedPayload(
        ICompanion companion,
        string stationId,
        IReadOnlyList<CardSO> cards,
        IReadOnlyList<UpgradeSO> upgrades)
    {
        Companion = companion;
        StationId = stationId;
        Cards = cards;
        Upgrades = upgrades;
    }
}

/// <summary>
/// Central registry for stations and companions.
/// Keeps runtime lists typed by interfaces so gameplay systems remain decoupled
/// from specific ScriptableObject implementations.
/// </summary>
public class StationManager : MonoBehaviour, ISaveParticipant
{
    [Header("Data Sources (assign in Inspector)")]
    /// <summary>List of all station definitions available in the game.</summary>
    [Tooltip("List of all station definitions available in the game.")]
    [SerializeField] private List<StationSO> stationAssets = new();

    /// <summary>List of all companions player can eventually recruit.</summary>
    [Tooltip("List of all companions player can eventually recruit.")]
    [SerializeField] private List<CompanionSO> companionAssets = new();

    // Backing collections exposed as interface lists.
    /// <summary>Runtime list of station interfaces for decoupled access.</summary>
    private readonly List<IStation> _stations = new();
    /// <summary>Runtime list of companion interfaces for decoupled access.</summary>
    private readonly List<ICompanion> _companions = new();

    // Lookup dictionaries for O(1) ID resolution.
    // Built from the serialized lists during Awake so other systems can
    // quickly fetch a specific asset without scanning linearly.
    private readonly Dictionary<string, StationSO> _stationLookup = new();
    private readonly Dictionary<string, CompanionSO> _companionLookup = new();

    // Persistence containers
    /// <summary>Tracks unlocked stations by their stable IDs.</summary>
    private readonly HashSet<string> _unlockedStationIds = new();
    /// <summary>Maps companion ID to the station ID they are assigned to.</summary>
    private readonly Dictionary<string, string> _companionAssignments = new();

    /// <summary>
    /// Fired when the player recruits a companion.
    /// Event passes a <see cref="CompanionRecruitedPayload"/> containing the
    /// companion's starting cards, passive upgrades and current station assignment
    /// so battle and upgrade systems can react instantly.
    /// </summary>
    public event Action<CompanionRecruitedPayload> OnCompanionRecruited;

    /// <summary>
    /// Unity Awake: build interface lists and capture default companion assignments
    /// so the manager starts with clean runtime collections.
    /// </summary>
    private void Awake()
    {
        BuildLookups();

        // Build interface lists once on startup; ScriptableObjects live in memory.
        // We iterate the serialized lists rather than dictionary values to
        // preserve inspector ordering for any UI that might rely on it.
        _stations.Clear();
        foreach (var so in stationAssets)
            if (so != null) _stations.Add(so);

        _companions.Clear();
        _companionAssignments.Clear();
        foreach (var co in companionAssets)
        {
            if (co == null) continue;
            _companions.Add(co);

            // Capture default assignment so it can be persisted later.
            var assigned = co.AssignedStation;
            if (assigned != null) _companionAssignments[co.Id] = assigned.Id;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only: keep lookup dictionaries in sync when values change in the inspector.
    /// </summary>
    private void OnValidate()
    {
        BuildLookups();
    }
#endif

    /// <summary>
    /// Populate the ID lookup dictionaries from the serialized asset lists.
    /// </summary>
    private void BuildLookups()
    {
        _stationLookup.Clear();
        foreach (var so in stationAssets)
        {
            // Skip null entries or assets missing an ID to avoid exceptions later.
            if (so == null || string.IsNullOrEmpty(so.Id)) continue;
            _stationLookup[so.Id] = so; // last one wins on duplicate IDs
        }

        _companionLookup.Clear();
        foreach (var co in companionAssets)
        {
            if (co == null || string.IsNullOrEmpty(co.Id)) continue;
            _companionLookup[co.Id] = co;
        }
    }

    /// <summary>Enumerate all known stations.</summary>
    public IReadOnlyList<IStation> Stations => _stations;

    /// <summary>Enumerate all companions.</summary>
    public IReadOnlyList<ICompanion> Companions => _companions;

    /// <summary>Read-only map of station IDs to their definitions.</summary>
    public IReadOnlyDictionary<string, StationSO> StationLookup => _stationLookup;

    /// <summary>Read-only map of companion IDs to their definitions.</summary>
    public IReadOnlyDictionary<string, CompanionSO> CompanionLookup => _companionLookup;

    /// <summary>
    /// Unlock a station by its ID.
    /// Returns false if the ID is unknown or already unlocked.
    /// </summary>
    public bool UnlockStation(string id)
    {
        // Lookup via dictionary instead of scanning the list each time.
        var so = GetStationById(id);
        if (so == null || _unlockedStationIds.Contains(id))
            return false; // invalid or already unlocked

        _unlockedStationIds.Add(id); // track newly unlocked station

        // Broadcast through the global event bus so UI and other systems stay in sync.
        GameManager.Instance?.Events?.RaiseStationUnlocked(so);
        // Persist the unlock asynchronously.
        SaveScheduler.RequestSave(GameManager.Instance);
        return true;
    }

    /// <summary>
    /// Recruit a companion by ID.
    /// Adds the companion to the assignment map with no station.
    /// Returns false if the ID is unknown or already recruited.
    /// </summary>
    public bool RecruitCompanion(string id)
    {
        // Dictionary lookup avoids O(n) scans over companion assets.
        var co = GetCompanionById(id);
        if (co == null || _companionAssignments.ContainsKey(id))
            return false; // invalid or already recruited

        const string stationId = null; // newly recruited companions start unassigned
        _companionAssignments[id] = stationId;

        // Build a payload bundling all relevant data so interested systems
        // (battle, upgrades, etc.) can initialize without extra lookups.
        var payload = new CompanionRecruitedPayload(
            co,
            stationId,
            co.GetStartingCards(),
            co.GetPassiveBuffs());

        // Fire event with companion loadout so listeners can update immediately
        OnCompanionRecruited?.Invoke(payload);

        // Also notify the global bus for general awareness.
        GameManager.Instance?.Events?.RaiseCompanionRecruited(co);

        // Persist recruitment and assignment changes.
        SaveScheduler.RequestSave(GameManager.Instance);
        return true;
    }

    /// <summary>
    /// Unity OnEnable: subscribe to station production events so results can be
    /// forwarded through the global event bus.
    /// </summary>
    private void OnEnable()
    {
        for (int i = 0; i < _stations.Count; i++)
            _stations[i].OnProductionComplete += HandleStationProductionComplete;
    }

    /// <summary>
    /// Unity OnDisable: unsubscribe from station production events to avoid leaks.
    /// </summary>
    private void OnDisable()
    {
        for (int i = 0; i < _stations.Count; i++)
            _stations[i].OnProductionComplete -= HandleStationProductionComplete;
    }

    /// <summary>
    /// Forward station production results to the global event bus.
    /// </summary>
    private void HandleStationProductionComplete(MinigameResult result)
    {
        GameManager.Instance?.Events?.RaiseMinigameCompleted(result);
    }

    // ---- Save/Load via SaveModelV2 ----

    /// <summary>Restore station and companion state from the save model.</summary>
    public void ApplyLoadedState(SaveModelV2 data)
    {
        _unlockedStationIds.Clear();
        _companionAssignments.Clear();
        if (data == null) return;

        var gm = GameManager.Instance;

        // Reapply unlocked stations and notify listeners so UI can refresh.
        foreach (var id in data.unlockedStationIds)
        {
            var so = GetStationById(id);
            if (so == null) continue;
            _unlockedStationIds.Add(id);
            gm?.Events?.RaiseStationUnlocked(so);
        }

        // Restore companion assignments (key = companion ID, value = station ID or null).
        foreach (var kvp in data.companionAssignments)
        {
            _companionAssignments[kvp.Key] = kvp.Value;
            var co = GetCompanionById(kvp.Key);
            var station = GetStationById(kvp.Value);
            co?.SetAssignedStation(station);
            gm?.Events?.RaiseCompanionRecruited(co);
        }
    }

    /// <summary>Write station unlocks and companion assignments into the save model.</summary>
    public void Capture(SaveModelV2 model)
    {
        if (model == null) return;
        foreach (var id in _unlockedStationIds)
            model.unlockedStationIds.Add(id);

        foreach (var kvp in _companionAssignments)
            model.companionAssignments[kvp.Key] = kvp.Value;
    }

    /// <summary>Apply station and companion state from the save model.</summary>
    public void Apply(SaveModelV2 model)
    {
        ApplyLoadedState(model);
    }

    /// <summary>
    /// Helper to locate a <see cref="StationSO"/> by its stable ID.
    /// </summary>
    /// <remarks>
    /// Exposed publicly so callers outside this manager can perform quick lookups
    /// without needing to maintain their own references.
    /// </remarks>
    public StationSO GetStationById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null; // guard against bad input

        // TryGetValue avoids KeyNotFoundException and returns null when missing.
        _stationLookup.TryGetValue(id, out var so);
        return so;
    }

    /// <summary>
    /// Helper to locate a <see cref="CompanionSO"/> by ID.
    /// </summary>
    /// <remarks>
    /// Public so other systems can fetch companion data directly.
    /// </remarks>
    public CompanionSO GetCompanionById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        _companionLookup.TryGetValue(id, out var co);
        return co;
    }
}
