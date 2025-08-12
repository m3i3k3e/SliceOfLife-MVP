using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps track of how far the player has climbed in the endless dungeon.
/// Also handles unlocking crafting stations at floor milestones.
/// </summary>
public class DungeonProgression : MonoBehaviour, ISaveParticipant
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

        // Starting a run should immediately inform UI of the current floor so
        // labels like the dungeon HUD reflect "Floor 1" right away. Going
        // through <see cref="GameManager.Events"/> ensures any listeners wired
        // to the global event bus react even before the player advances.
        var events = GameManager.Instance?.Events;
        events?.RaiseFloorReached(CurrentFloor);
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
            SaveScheduler.RequestSave(gm); // schedule persistence instead of immediate write
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

    // ---- Save/Load via SaveModelV2 ----

    /// <summary>Write current floor progress into the save model.</summary>
    public void Capture(SaveModelV2 model)
    {
        if (model == null) return;
        model.currentFloor = CurrentFloor;
        model.maxFloorReached = MaxFloorReached;

        // Persist which milestone floors have already granted their rewards so
        // we don't double unlock stations after loading.
        model.unlockedDungeonMilestones = new List<int>(_unlockedMilestones);
    }

    /// <summary>Restore floor progress from the save model.</summary>
    public void Apply(SaveModelV2 model)
    {
        if (model == null) return;

        CurrentFloor = Mathf.Max(1, model.currentFloor);
        MaxFloorReached = Mathf.Max(CurrentFloor, model.maxFloorReached);

        // Restore previously granted milestone floors so we don't double unlock
        // stations after loading. Fall back to recomputing from the max floor
        // for backward compatibility with older saves that lack the list.
        _unlockedMilestones.Clear();
        if (model.unlockedDungeonMilestones != null)
        {
            foreach (var floor in model.unlockedDungeonMilestones)
                _unlockedMilestones.Add(floor);
        }

        foreach (var milestone in stationMilestones)
        {
            if (milestone == null) continue;
            if (MaxFloorReached >= milestone.floor)
                _unlockedMilestones.Add(milestone.floor);
        }

        // Inform listeners of the restored floor so UI can update.
        OnFloorReached?.Invoke(CurrentFloor);
    }
}

