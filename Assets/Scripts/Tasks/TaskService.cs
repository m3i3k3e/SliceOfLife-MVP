using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coordinates sequential tutorial tasks and watches global events to mark
/// conditions complete. Raises <see cref="GameEvents"/> when progress occurs
/// so UI or other systems can react without referencing this service directly.
/// </summary>
public class TaskService : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ordered task graph defining the tutorial path.")]
    [SerializeField] private TaskGraphSO graph;
    [Tooltip("Inventory used to query item counts. If left null, one is searched for at runtime.")]
    [SerializeField] private InventoryManager inventory;

    // Internal state for each task in the graph.
    private readonly List<TaskState> _states = new();
    private int _currentIndex; // index of the active task

    /// <summary>Convenience accessor to the inventory interface.</summary>
    private IInventoryService Inventory => inventory;

    private void Awake()
    {
        // Allow the service to function even if the inventory isn't wired in the prefab.
        if (inventory == null)
            inventory = FindObjectOfType<InventoryManager>();

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
    public void Init(TaskGraphSO newGraph, IEnumerable<SaveModelV2.TaskStateDTO> loaded)
    {
        _states.Clear();
        graph = newGraph;
        if (graph == null) return; // nothing to track

        // Build runtime state for each task and apply any loaded completion flags.
        foreach (var task in graph.Tasks)
        {
            bool completed = false;
            if (loaded != null)
            {
                foreach (var dto in loaded)
                    if (dto.taskId == task.Id)
                        completed = dto.completed;
            }
            _states.Add(new TaskState(task, completed));
        }

        // Determine which task is currently active (first not yet completed).
        _currentIndex = _states.FindIndex(s => !s.completed);
        if (_currentIndex < 0)
            _currentIndex = _states.Count; // all tasks done

        // Evaluate immediately in case inventory already satisfies early tasks.
        EvaluateCurrent(TaskEvent.Inventory);
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
        EvaluateCurrent(TaskEvent.Interaction, interactId);
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

    private void HandleInventoryChanged() => EvaluateCurrent(TaskEvent.Inventory);
    private void HandleUpgradePurchased(UpgradeSO up) => EvaluateCurrent(TaskEvent.Upgrade, up != null ? up.id : null);

    // ----- Core evaluation -----

    private enum TaskEvent { Inventory, Interaction, Upgrade }

    /// <summary>
    /// Check the active task against the provided event and fire GameEvents when progress occurs.
    /// </summary>
    private void EvaluateCurrent(TaskEvent evt, string param = null)
    {
        if (_currentIndex < 0 || _currentIndex >= _states.Count) return; // nothing active
        var state = _states[_currentIndex];
        bool advanced = false;

        foreach (var cond in state.task.Conditions)
        {
            if (cond.completed) continue; // skip already satisfied conditions

            switch (cond.type)
            {
                case TaskConditionType.CollectItem:
                    if (evt == TaskEvent.Inventory && Inventory != null && cond.item != null &&
                        Inventory.GetCount(cond.item) >= cond.requiredQty)
                    {
                        cond.completed = true;
                        advanced = true;
                    }
                    break;
                case TaskConditionType.Interact:
                    if (evt == TaskEvent.Interaction && param == cond.interactId)
                    {
                        cond.completed = true;
                        advanced = true;
                    }
                    break;
                case TaskConditionType.UpgradePurchased:
                    if (evt == TaskEvent.Upgrade && cond.upgrade != null && param == cond.upgrade.id)
                    {
                        cond.completed = true;
                        advanced = true;
                    }
                    break;
            }
        }

        if (advanced)
            GameEvents.RaiseTaskAdvanced();

        // If all conditions complete, mark the task finished and move to the next.
        bool allDone = true;
        foreach (var c in state.task.Conditions)
            if (!c.completed)
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
    }

    /// <summary>Runtime container pairing a task with its completion flag.</summary>
    private class TaskState
    {
        public TaskSO task;
        public bool completed;
        public TaskState(TaskSO task, bool completed)
        {
            this.task = task;
            this.completed = completed;
        }
    }
}
