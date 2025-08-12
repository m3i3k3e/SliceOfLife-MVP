using UnityEngine;

/// <summary>
/// Simple effect implementation that applies the configured tuning values.
/// Unused fields should be left at zero in the inspector.
/// </summary>
[CreateAssetMenu(menuName = "SliceOfLife/Card Effects/Basic", fileName = "CardEffect")]
public class BasicCardEffect : CardEffect
{
    /// <summary>
    /// Apply any non-zero tuning values to the battle context.
    /// </summary>
    public override void Execute(BattleContext ctx)
    {
        // Each check is independent so designers can mix and match behaviors.
        if (Damage > 0) ctx.DamageEnemy(Damage);
        if (Block > 0) ctx.AddPlayerArmor(Block);
        if (Heal > 0) ctx.TryHealPlayer(Heal);
        if (Weak > 0) ctx.ApplyWeakToEnemy(Weak);
        if (Vulnerable > 0) ctx.ApplyVulnerableToEnemy(Vulnerable);
    }
}
