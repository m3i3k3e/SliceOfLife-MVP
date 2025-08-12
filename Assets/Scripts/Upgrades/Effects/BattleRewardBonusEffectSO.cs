using UnityEngine;

/// <summary>
/// Multiplies battle rewards by a percentage.
/// Derived stats are handled via <see cref="ApplyDerived"/> to avoid double-applying.
/// </summary>
[CreateAssetMenu(menuName = "SliceOfLife/UpgradeEffects/BattleRewardBonus")]
public class BattleRewardBonusEffectSO : UpgradeEffectSO
{
    [SerializeField, Tooltip("Percent bonus: 25 means +25% rewards")]
    private float percent = 25f;

    /// <inheritdoc />
    public override void Apply(GameManager gm)
    {
        // One-shot application does nothing; multiplier handled in derived stats
    }

    /// <inheritdoc />
    public override void ApplyDerived(UpgradeManager um)
    {
        if (um == null) return;
        um.RewardMultiplier *= 1f + (percent / 100f);
    }
}
