using System.Threading.Tasks;

/// <summary>
/// Simplest possible contract for a station mini-game.
/// Implementations decide how to present themselves (scene load, popup, etc.).
/// </summary>
public interface IMinigame
{
    /// <summary>
    /// Begins the mini-game and resolves when the session finishes.
    /// Returning a result allows callers to react (grant resources, etc.).
    /// </summary>
    Task<MinigameResult> PlayAsync();
}
