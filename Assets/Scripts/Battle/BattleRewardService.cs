using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Central place to compute and grant post-battle rewards.
/// Keeps GameManager coupling out of the core BattleManager logic.
/// </summary>
public class BattleRewardService
{
    private readonly IGameManager _gameManager;

    /// <summary>
    /// Require a reference to <see cref="IGameManager"/> so callers can inject
    /// the concrete implementation. Keeps battle logic decoupled from the
    /// GameManager singleton.
    /// </summary>
    public BattleRewardService(IGameManager gameManager)
    {
        _gameManager = gameManager;
    }

    /// <summary>
    /// Calculates the final reward based on config and active upgrades,
    /// grants it through the EssenceManager, and returns the amount awarded.
    /// </summary>
    public async Task<int> GrantVictoryReward(BattleConfigSO config)
    {
        // 1) Base reward from config. Defensive in case designers leave it negative.
        int baseReward = Mathf.Max(0, config.baseEssenceReward);

        // 2) Pull the multiplier from the Upgrade system if available.
        float multiplier = 1f;
        var upgrades = _gameManager?.Upgrades;
        if (upgrades != null)
        {
            multiplier = Mathf.Max(0f, upgrades.RewardMultiplier);
        }

        // 3) Apply multiplier and round to an int for currency.
        int reward = Mathf.RoundToInt(baseReward * multiplier);

        // 4) Grant the reward via the Essence manager (if present).
        var essence = _gameManager?.Essence;
        if (essence != null)
        {
            essence.AddExternal(reward);
            // Persist the payout so it isn't lost if the app closes immediately after battle.
            if (_gameManager is GameManager concrete)
            {
                await SaveSystem.SaveAsync(concrete); // optional: persist immediately
            }
        }

        // 5) Unlock any recipe reward configured for this battle.
        if (config != null && config.recipeReward != null)
        {
            _gameManager?.Recipes?.UnlockRecipe(config.recipeReward.Id);
        }

        return reward;
    }
}

