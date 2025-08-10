using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Central place to compute and grant post-battle rewards.
/// Keeps GameManager coupling out of the core BattleManager logic.
/// </summary>
public class BattleRewardService
{
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
        var upgrades = GameManager.Instance != null ? GameManager.Instance.Upgrades : null;
        if (upgrades != null)
        {
            multiplier = Mathf.Max(0f, upgrades.RewardMultiplier);
        }

        // 3) Apply multiplier and round to an int for currency.
        int reward = Mathf.RoundToInt(baseReward * multiplier);

        // 4) Grant the reward via the Essence manager (if present).
        var gm = GameManager.Instance;
        if (gm != null && gm.Essence != null)
        {
            gm.Essence.AddExternal(reward);
            // Persist the payout so it isn't lost if the app closes immediately after battle.
            await SaveSystem.SaveAsync(gm); // optional: persist immediately
        }

        return reward;
    }
}

