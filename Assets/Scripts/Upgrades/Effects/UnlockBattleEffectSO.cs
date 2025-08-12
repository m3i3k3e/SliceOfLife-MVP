using UnityEngine;

/// <summary>
/// Marker effect used to gate the dungeon/battle system.
/// Purchasing upgrades with this effect sets a flag that UI can query via UpgradeManager.IsPurchased.
/// </summary>
[CreateAssetMenu(menuName = "SliceOfLife/UpgradeEffects/UnlockBattle")]
public class UnlockBattleEffectSO : UpgradeEffectSO
{
    /// <inheritdoc />
    public override void Apply(GameManager gm)
    {
        // No numeric state to modify; presence in PurchasedIds is enough for gating.
    }
}
