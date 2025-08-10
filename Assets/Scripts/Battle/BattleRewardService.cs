using UnityEngine;

/// <summary>
/// Small service that converts a base reward into a final amount
/// by consulting the upgrade system for multipliers.
/// Extracted so future modes (e.g., bosses, quests) can reuse it.
/// </summary>
public class BattleRewardService
{
    private readonly IUpgradeProvider _upgrades;

    public BattleRewardService(IUpgradeProvider upgrades)
    {
        _upgrades = upgrades; // may be null in tests or early boot
    }

    /// <summary>
    /// Compute the final reward given a base value.
    /// Applies a multiplicative upgrade bonus if available.
    /// </summary>
    public int CalculateReward(int baseReward)
    {
        int clamped = Mathf.Max(0, baseReward);
        float mult = 1f;
        if (_upgrades != null)
            mult = Mathf.Max(0f, _upgrades.RewardMultiplier);
        return Mathf.RoundToInt(clamped * mult);
    }
}
