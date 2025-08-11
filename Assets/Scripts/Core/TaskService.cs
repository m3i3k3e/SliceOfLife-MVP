using UnityEngine;

/// <summary>
/// Placeholder runtime service that will eventually coordinate game tasks and quests.
/// Exists now so <see cref="GameBootstrap"/> can spawn a single persistent instance.
/// </summary>
public class TaskService : MonoBehaviour
{
    // Intentionally empty; future iterations will flesh out task logic.

    /// <summary>
    /// Placeholder hook so the save system can pass loaded state once tasks exist.
    /// </summary>
    public void ApplyLoadedState(SaveModelV2 data)
    {
        // No task state to restore yet.
    }
}

