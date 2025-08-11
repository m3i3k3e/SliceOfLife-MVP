using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps track of how far the player has climbed in the endless dungeon.
/// Also handles unlocking crafting stations at floor milestones.
/// </summary>
public class DungeonProgression : MonoBehaviour, ISaveable
{
    // ----- Milestone configuration -----
    [Serializable]
    private class StationMilestone
    {
        [Tooltip("Floor at which the station unlocks. Expect multiples of 10.")]
        public int floor = 10;

        [Tooltip("Station unlocked when reaching this floor.")]
        public StationSO station;
    }

    [Header("Station Milestones")]
    [Tooltip("List of stations granted when the player reaches specific floors.")]
    [SerializeField] private List<StationMilestone> stationMilestones = new();

    // Track which milestone floors have already been rewarded so we don't double unlock.
    private readonly HashSet<int> _unlockedMilestones = new();

    // ----- Runtime state -----

    /// <summary>Current floor within the active run.</summary>
    public int CurrentFloor { get; private set; } = 1;

    /// <summary>Highest floor ever reached across all runs.</summary>
    public int MaxFloorReached { get; private set; } = 1;

    /// <summary>Fired whenever a new floor is reached. Payload = floor index.</summary>
    public event Action<int> OnFloorReached;

    /// <summary>
    /// Reset the run back to floor 1. Call when starting a new attempt.
    /// </summary>
    public void StartRun()
    {
        CurrentFloor = 1;
    }

    /// <summary>
    /// Move the player to the next floor and trigger milestone checks.
    /// </summary>
    public void AdvanceFloor()
    {
        // Increment current run progress.
        CurrentFloor = Mathf.Max(1, CurrentFloor + 1);

        // Record new high-water mark.
        if (CurrentFloor > MaxFloorReached)
            MaxFloorReached = CurrentFloor;

        // Unlock any stations tied to newly reached milestones.
        CheckMilestones();

        // Notify listeners of the floor change.
        OnFloorReached?.Invoke(CurrentFloor);

        // Persist progress so it survives app restarts.
        var gm = GameManager.Instance;
        if (gm != null)
            SaveSystem.Save(gm);
    }

    /// <summary>
    /// Iterate milestone definitions and unlock stations when their floor threshold is reached.
    /// </summary>
    private void CheckMilestones()
    {
        var stations = GameManager.Instance?.Stations;
        foreach (var milestone in stationMilestones)
        {
            if (milestone == null || milestone.station == null) continue;

            // Only fire once per milestone.
            if (MaxFloorReached >= milestone.floor && _unlockedMilestones.Add(milestone.floor))
            {
                // Delegate actual unlock logic to StationManager.
                stations?.UnlockStation(milestone.station.Id);
            }
        }
    }

    // ----- Persistence -----

    /// <inheritdoc/>
    public string SaveKey => "Dungeon";

    [Serializable]
    private class SaveData
    {
        public int maxFloorReached;
        public List<int> unlockedMilestones = new();
    }

    /// <inheritdoc/>
    public object ToData()
    {
        return new SaveData
        {
            maxFloorReached = MaxFloorReached,
            unlockedMilestones = new List<int>(_unlockedMilestones)
        };
    }

    /// <inheritdoc/>
    public void LoadFrom(object data)
    {
        var d = data as SaveData;
        MaxFloorReached = d != null ? Mathf.Max(1, d.maxFloorReached) : 1;
        CurrentFloor = 1; // fresh run after load

        _unlockedMilestones.Clear();
        var stations = GameManager.Instance?.Stations;
        if (d != null && d.unlockedMilestones != null)
        {
            foreach (var floor in d.unlockedMilestones)
            {
                _unlockedMilestones.Add(floor);

                // Ensure the corresponding station is unlocked on load.
                var milestone = stationMilestones.Find(m => m.floor == floor);
                if (milestone != null && milestone.station != null)
                    stations?.UnlockStation(milestone.station.Id);
            }
        }
    }
}

