using UnityEngine;

/// <summary>
/// Ordered collection of tasks representing a tutorial sequence.
/// </summary>
[CreateAssetMenu(fileName = "TaskGraph", menuName = "SliceOfLife/Task Graph")]
public class TaskGraphSO : ScriptableObject
{
    [Tooltip("Tasks executed in order; each must complete before the next unlocks.")]
    [SerializeField] private TaskSO[] tasks = System.Array.Empty<TaskSO>();

    /// <summary>Sequential list of tasks.</summary>
    public TaskSO[] Tasks => tasks;
}
