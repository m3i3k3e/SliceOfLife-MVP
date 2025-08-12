using System;
using UnityEngine;

/// <summary>
/// Data asset describing a single tutorial task with a set of conditions.
/// </summary>
[CreateAssetMenu(fileName = "Task", menuName = "SliceOfLife/Task")]
public class TaskSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable identifier used for saves and lookups.")]
    [SerializeField] private string id = "task_id";
    [SerializeField] private string title = "New Task";
    [TextArea]
    [SerializeField] private string description = "";

    [Header("Conditions")]
    [Tooltip("All conditions must be satisfied to complete the task.")]
    [SerializeField] private TaskConditionSO[] conditions = Array.Empty<TaskConditionSO>();

    /// <summary>Unique id for save files.</summary>
    public string Id => id;
    /// <summary>Human readable title shown in UI.</summary>
    public string Title => title;
    /// <summary>Flavor text or instructions for the player.</summary>
    public string Description => description;
    /// <summary>Array of conditions that must all be met.</summary>
    public TaskConditionSO[] Conditions => conditions;
}
