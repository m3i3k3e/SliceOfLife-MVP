using System;
using UnityEngine;

/// <summary>
/// Placeholder runtime service that will eventually coordinate game tasks and quests.
/// Exists now so <see cref="GameBootstrap"/> can spawn a single persistent instance.
/// </summary>
public class TaskService : MonoBehaviour
{
    // Intentionally minimal; future iterations will flesh out task logic.

    /// <summary>
    /// Raised whenever some task progresses. Signature kept simple for now.
    /// </summary>
    public event Action OnTaskAdvanced;

    /// <summary>
    /// Unity lifecycle: bridge our instance event to <see cref="GameEvents"/>.
    /// </summary>
    private void OnEnable()
    {
        // Allow ad-hoc listeners to respond without referencing TaskService directly.
        OnTaskAdvanced += GameEvents.RaiseTaskAdvanced;
    }

    /// <summary>
    /// Unity lifecycle: drop mirrored subscription to avoid leaks when disabled.
    /// </summary>
    private void OnDisable()
    {
        OnTaskAdvanced -= GameEvents.RaiseTaskAdvanced;
    }

    /// <summary>
    /// External call to simulate a task advancing. Useful for testing.
    /// </summary>
    public void AdvanceTask()
    {
        // Fire the local event which will cascade into GameEvents via the bridge.
        OnTaskAdvanced?.Invoke();
    }

    /// <summary>
    /// Placeholder hook so the save system can pass loaded state once tasks exist.
    /// </summary>
    public void ApplyLoadedState(SaveModelV2 data)
    {
        // No task state to restore yet.
    }
}

