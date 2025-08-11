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
    [SerializeField] private TaskCondition[] conditions = Array.Empty<TaskCondition>();

    /// <summary>Unique id for save files.</summary>
    public string Id => id;
    /// <summary>Human readable title shown in UI.</summary>
    public string Title => title;
    /// <summary>Flavor text or instructions for the player.</summary>
    public string Description => description;
    /// <summary>Array of conditions that must all be met.</summary>
    public TaskCondition[] Conditions => conditions;
}

/// <summary>Serializable requirement used by <see cref="TaskSO"/>.</summary>
[Serializable]
public class TaskCondition
{
    [Tooltip("Type of check performed for this condition.")]
    public TaskConditionType type = TaskConditionType.Interact;

    [Tooltip("Item to count when type is CollectItem.")]
    public ItemSO item;
    [Tooltip("Required quantity when collecting items.")]
    public int requiredQty = 1;

    [Tooltip("Interaction identifier when type is Interact.")]
    public string interactId;

    [Tooltip("Upgrade reference when type is UpgradePurchased.")]
    public UpgradeSO upgrade;

    [NonSerialized] public bool completed; // runtime flag only
}

/// <summary>Supported condition categories.</summary>
public enum TaskConditionType { CollectItem, Interact, UpgradePurchased }
