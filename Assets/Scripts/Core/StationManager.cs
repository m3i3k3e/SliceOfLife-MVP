using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central registry for stations and companions.
/// Keeps runtime lists typed by interfaces so gameplay systems remain decoupled
/// from specific ScriptableObject implementations.
/// </summary>
public class StationManager : MonoBehaviour
{
    [Header("Data Sources (assign in Inspector)")]
    [Tooltip("List of all station definitions available in the game.")]
    [SerializeField] private List<StationSO> stationAssets = new();

    [Tooltip("List of all companions player can eventually recruit.")]
    [SerializeField] private List<CompanionSO> companionAssets = new();

    // Backing collections exposed as interface lists.
    private readonly List<IStation> _stations = new();
    private readonly List<ICompanion> _companions = new();

    // Persistence containers
    // Tracks unlocked stations by their stable IDs
    private readonly HashSet<string> _unlockedStationIds = new();
    // Maps companion ID -> station ID they are assigned to
    private readonly Dictionary<string, string> _companionAssignments = new();

    /// <summary>
    /// Fired whenever a station becomes unlocked.
    /// Payload is the station ScriptableObject (typed via interface).
    /// </summary>
    public event Action<IStation> OnStationUnlocked;

    /// <summary>
    /// Fired when the player recruits a companion.
    /// The companion is added to our assignment list with no station.
    /// </summary>
    public event Action<ICompanion> OnCompanionRecruited;

    private void Awake()
    {
        // Build interface lists once on startup; ScriptableObjects live in memory.
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

    /// <summary>Enumerate all known stations.</summary>
    public IReadOnlyList<IStation> Stations => _stations;

    /// <summary>Enumerate all companions.</summary>
    public IReadOnlyList<ICompanion> Companions => _companions;

    /// <summary>
    /// Unlock a station by its ID.
    /// Returns false if the ID is unknown or already unlocked.
    /// </summary>
    public bool UnlockStation(string id)
    {
        var so = FindStationById(id);
        if (so == null || _unlockedStationIds.Contains(id))
            return false; // invalid or already unlocked

        _unlockedStationIds.Add(id); // track newly unlocked station
        OnStationUnlocked?.Invoke(so); // notify listeners
        return true;
    }

    /// <summary>
    /// Recruit a companion by ID.
    /// Adds the companion to the assignment map with no station.
    /// Returns false if the ID is unknown or already recruited.
    /// </summary>
    public bool RecruitCompanion(string id)
    {
        var co = FindCompanionById(id);
        if (co == null || _companionAssignments.ContainsKey(id))
            return false; // invalid or already recruited

        _companionAssignments[id] = null; // recruited, not yet assigned
        OnCompanionRecruited?.Invoke(co);
        return true;
    }

    // ---- Save/Load helpers ----

    /// <summary>
    /// Convert runtime station/companion state into serializable records.
    /// </summary>
    public (GameSaveData.StationData Stations, GameSaveData.CompanionData Companions) ToData()
    {
        // Build station data
        var stationData = new GameSaveData.StationData
        {
            unlockedStationIds = new List<string>(_unlockedStationIds)
        };

        // Build companion assignment list
        var companionData = new GameSaveData.CompanionData();
        foreach (var kvp in _companionAssignments)
        {
            companionData.assignments.Add(new GameSaveData.CompanionData.Assignment
            {
                companionId = kvp.Key,
                stationId = kvp.Value
            });
        }

        return (stationData, companionData);
    }

    /// <summary>
    /// Restore unlocked stations and companion assignments from save data.
    /// </summary>
    public void LoadFrom(GameSaveData.StationData stationData, GameSaveData.CompanionData companionData)
    {
        // ---- Stations ----
        _unlockedStationIds.Clear();
        if (stationData != null && stationData.unlockedStationIds != null)
            foreach (var id in stationData.unlockedStationIds)
                _unlockedStationIds.Add(id);

        // ---- Companions ----
        _companionAssignments.Clear();
        if (companionData != null && companionData.assignments != null)
            foreach (var a in companionData.assignments)
                if (!string.IsNullOrEmpty(a.companionId))
                    _companionAssignments[a.companionId] = a.stationId;

        // Apply assignments to ScriptableObjects so runtime queries reflect loaded state.
        foreach (var co in companionAssets)
        {
            if (co == null) continue;

            if (_companionAssignments.TryGetValue(co.Id, out var stationId))
                co.SetAssignedStation(FindStationById(stationId));
            else
                co.SetAssignedStation(co.StartingStation); // default if missing
        }
    }

    /// <summary>
    /// Helper to locate a StationSO by ID.
    /// </summary>
    private StationSO FindStationById(string id)
    {
        for (int i = 0; i < stationAssets.Count; i++)
        {
            var so = stationAssets[i];
            if (so != null && so.Id == id) return so;
        }
        return null;
    }

    /// <summary>
    /// Helper to locate a CompanionSO by ID.
    /// </summary>
    private CompanionSO FindCompanionById(string id)
    {
        for (int i = 0; i < companionAssets.Count; i++)
        {
            var co = companionAssets[i];
            if (co != null && co.Id == id) return co;
        }
        return null;
    }
}
