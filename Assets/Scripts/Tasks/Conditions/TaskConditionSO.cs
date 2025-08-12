using UnityEngine;

/// <summary>
/// Base class for task completion checks. Derive and override
/// <see cref="IsMet(TaskService)"/> to implement custom logic.
/// </summary>
public abstract class TaskConditionSO : ScriptableObject
{
    /// <summary>
    /// Returns true when the condition has been satisfied.
    /// Override in subclasses with specific logic. Default always false.
    /// </summary>
    /// <param name="svc">Task service providing contextual data.</param>
    public virtual bool IsMet(TaskService svc) => false;
}
