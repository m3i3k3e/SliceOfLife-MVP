/// <summary>
/// Simplest possible contract for a station mini-game.
/// Implementations decide how to present themselves (scene load, popup, etc.).
/// </summary>
public interface IMinigame
{
    /// <summary>Begin the mini-game. Implementations handle their own lifecycle.</summary>
    void StartGame();
}
