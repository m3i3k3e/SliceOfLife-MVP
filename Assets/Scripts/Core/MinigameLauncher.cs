using System;
using System.Threading.Tasks;

/// <summary>
/// Helper that runs a mini-game and forwards its result via an event bus.
/// Keeps callers free from managing async/await or bus plumbing.
/// </summary>
public static class MinigameLauncher
{
    /// <summary>
    /// Awaits <paramref name="minigame"/> and publishes its result.
    /// </summary>
    /// <param name="minigame">Mini-game implementation to execute.</param>
    /// <param name="events">Event bus used to broadcast completion.</param>
    /// <returns>The <see cref="MinigameResult"/> returned by the mini-game.</returns>
    public static async Task<MinigameResult> LaunchAsync(IMinigame minigame, IEventBus events)
    {
        if (minigame == null) throw new ArgumentNullException(nameof(minigame));
        if (events == null) throw new ArgumentNullException(nameof(events));

        // Await the mini-game; implementations decide how they complete.
        MinigameResult result = await minigame.PlayAsync();

        // Deposit any item rewards straight into the global inventory.
        var gm = GameManager.Instance;
        if (gm?.Inventory != null && result.RewardItem != null && result.RewardQuantity > 0)
            gm.Inventory.TryAdd(result.RewardItem, result.RewardQuantity);

        // Forward the outcome to any listeners on the event bus.
        events.RaiseMinigameCompleted(result);

        return result;
    }
}
