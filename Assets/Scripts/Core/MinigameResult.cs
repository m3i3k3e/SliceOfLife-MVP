/// <summary>
/// Lightweight payload returned by mini-games.
/// Extra fields can be added as mini-games become more complex.
/// </summary>
public record MinigameResult
(
    bool Success,
    int ResourcesGained,
    ItemSO RewardItem = null,
    int RewardQuantity = 0
);
