using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coordinates sequential tutorial tasks and watches global events to mark
/// conditions complete. Raises <see cref="GameEvents"/> when progress occurs
/// so UI or other systems can react without referencing this service directly.
/// </summary>
/// <remarks>
/// Requires both <see cref="graph"/> and <see cref="inventory"/> to be assigned
/// before <see cref="Init(TaskGraphSO, IEnumerable{SaveModelV2.TaskStateDTO})"/> runs.
/// Call <c>Init</c> during <see cref="Awake"/> or from an external bootstrapper
/// <b>before</b> the component is enabled so event subscriptions work with
/// initialized state.
/// </remarks>
public class TaskService : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ordered task graph defining the tutorial path.")]
    [SerializeField] private TaskGraphSO graph;
    [Tooltip("Inventory used to query item counts. Must be assigned in the inspector or injected.")]
    [SerializeField] private InventoryManager inventory;

    // Internal state for each task in the graph.
    private readonly List<TaskState> _states = new();
    private int _currentIndex; // index of the active task

    /// <summary>
    /// Title of the task the player is currently working on.
    /// Returns an empty string when all tasks are complete.
    /// </summary>
    public string CurrentTaskTitle
        => (_currentIndex >= 0 && _currentIndex < _states.Count)
            ? _states[_currentIndex].task.Title
            : string.Empty;

    /// <summary>Convenience accessor to the inventory interface.</summary>
    internal IInventoryService Inventory => inventory;

    // Transient context captured from the last external event so conditions can query it.
    private string _lastInteractionId;
    private string _lastUpgradeId;

    /// <summary>Identifier of the most recent interaction.</summary>
    internal string LastInteractionId => _lastInteractionId;
    /// <summary>Identifier of the most recently purchased upgrade.</summary>
    internal string LastUpgradeId => _lastUpgradeId;

    /// <summary>
    /// Unity lifecycle callback. Builds initial task state.
    /// </summary>
    private void Awake()
    {
        // Inventory is expected to be wired ahead of time. Warn if missing so
        // designers notice during testing.
        if (inventory == null)
            Debug.LogError("TaskService requires an InventoryManager reference.");

        Init(graph, null); // start with fresh state; load will reapply if needed
    }

    /// <summary>Subscribe to global events when enabled.</summary>
    private void OnEnable()
    {
        GameEvents.OnInventoryChanged += HandleInventoryChanged;
        GameEvents.OnUpgradePurchased += HandleUpgradePurchased;
    }

    /// <summary>Unsubscribe from global events when disabled.</summary>
    private void OnDisable()
    {
        GameEvents.OnInventoryChanged -= HandleInventoryChanged;
        GameEvents.OnUpgradePurchased -= HandleUpgradePurchased;
    }

    /// <summary>
    /// Initialize the service with a task graph and optionally loaded progress.
    /// </summary>
    /// <param name="newGraph">Graph describing all tutorial tasks in order.</param>
    /// <param name="loaded">Optional previously saved completion states.</param>
    /// <remarks>
    /// Must be invoked after <see cref="inventory"/> is assigned.
    /// </remarks>
    public void Init(TaskGraphSO newGraph, IEnumerable<SaveModelV2.TaskStateDTO> loaded)
    {
        _states.Clear();
        graph = newGraph;
        if (graph == null) return; // nothing to track

        // Build quick lookup from loaded DTOs so we avoid nested loops when
        // matching saved completion flags to tasks.
        var loadedMap = new Dictionary<string, bool>();
        if (loaded != null)
        {
            foreach (var dto in loaded)
                loadedMap[dto.taskId] = dto.completed; // later duplicates overwrite earlier entries
        }

        // Build runtime state for each task and apply any loaded completion flags.
        foreach (var task in graph.Tasks)
        {
            // Try to pull a completion flag from the loaded map; default to false.
            bool completed = loadedMap.TryGetValue(task.Id, out var saved) && saved;
            _states.Add(new TaskState(task, completed));
        }

        // Determine which task is currently active (first not yet completed).
        _currentIndex = _states.FindIndex(s => !s.completed);
        if (_currentIndex < 0)
            _currentIndex = _states.Count; // all tasks done

        // Evaluate immediately in case inventory already satisfies early tasks.
        EvaluateCurrent();
    }

    /// <summary>Is the specified task finished?</summary>
    public bool IsComplete(string taskId)
    {
        foreach (var s in _states)
            if (s.task.Id == taskId)
                return s.completed;
        return false;
    }

    /// <summary>External callers notify when an interaction completes.</summary>
    public void NotifyInteraction(string interactId)
    {
        _lastInteractionId = interactId;
        EvaluateCurrent();
    }

    /// <summary>Extract plain data for persistence.</summary>
    public List<SaveModelV2.TaskStateDTO> CaptureState()
    {
        var list = new List<SaveModelV2.TaskStateDTO>();
        foreach (var s in _states)
            list.Add(new SaveModelV2.TaskStateDTO { taskId = s.task.Id, completed = s.completed, progress = 0 });
        return list;
    }

    /// <summary>Apply state loaded from disk.</summary>
    public void ApplyLoadedState(SaveModelV2 data)
    {
        Init(graph, data?.tasks);
    }

    // ----- Event handlers -----

    private void HandleInventoryChanged() => EvaluateCurrent();

    private void HandleUpgradePurchased(UpgradeSO up)
    {
        _lastUpgradeId = up != null ? up.id : null;
        EvaluateCurrent();
    }
    // ----- Core evaluation -----

    /// <summary>
    /// Check the active task and fire <see cref="GameEvents"/> when progress occurs.
    /// </summary>
    private void EvaluateCurrent()
    {
        if (_currentIndex < 0 || _currentIndex >= _states.Count) return; // nothing active
        var state = _states[_currentIndex];
        bool advanced = false;

        for (int i = 0; i < state.task.Conditions.Length; i++)
        {
            if (state.conditionCompleted[i]) continue; // skip already satisfied conditions

            var cond = state.task.Conditions[i];
            if (cond != null && cond.IsMet(this))
            {
                state.conditionCompleted[i] = true;
                advanced = true;
            }
        }

        if (advanced)
            GameEvents.RaiseTaskAdvanced();

        // If all conditions complete, mark the task finished and move to the next.
        bool allDone = true;
        foreach (var done in state.conditionCompleted)
            if (!done)
            {
                allDone = false;
                break;
            }
        if (allDone)
        {
            state.completed = true;
            GameEvents.RaiseTaskCompleted();
            _currentIndex++;
        }

        // Clear transient event info so one interaction doesn't satisfy future tasks.
        _lastInteractionId = null;
        _lastUpgradeId = null;
    }

    /// <summary>Runtime container pairing a task with its completion flag.</summary>
    private class TaskState
    {
        public TaskSO task;
        public bool completed;
        public bool[] conditionCompleted;

        public TaskState(TaskSO task, bool completed)
        {
            this.task = task;
            this.completed = completed;
            conditionCompleted = new bool[task != null ? task.Conditions.Length : 0];
            if (completed)
            {
                for (int i = 0; i < conditionCompleted.Length; i++)
                    conditionCompleted[i] = true;
            }
        }
    }
}
