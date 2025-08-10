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

    private void Awake()
    {
        // Build interface lists once on startup; ScriptableObjects live in memory.
        _stations.Clear();
        foreach (var so in stationAssets)
            if (so != null) _stations.Add(so);

        _companions.Clear();
        foreach (var co in companionAssets)
            if (co != null) _companions.Add(co);
    }

    /// <summary>Enumerate all known stations.</summary>
    public IReadOnlyList<IStation> Stations => _stations;

    /// <summary>Enumerate all companions.</summary>
    public IReadOnlyList<ICompanion> Companions => _companions;
}
