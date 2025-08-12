using UnityEngine;

/// <summary>
/// Basic weighted-random enemy AI. Uses intent weights defined on the EnemySO.
/// </summary>
[CreateAssetMenu(menuName = "SliceOfLife/Enemy AI/Weighted", fileName = "EnemyAI")]
public class EnemyAI : ScriptableObject, IEnemyAI
{
    /// <inheritdoc />
    public EnemyIntent DecideNextIntent(EnemySO enemy)
    {
        if (enemy == null)
        {
            // Fallback so callers never deal with null intents
            return default;
        }

        // Sum weights to get range for random roll
        int totalWeight = Mathf.Max(0, enemy.LightWeight) + Mathf.Max(0, enemy.HeavyWeight) + Mathf.Max(0, enemy.LeechWeight);
        if (totalWeight <= 0) totalWeight = 1; // avoid division by zero

        int roll = Random.Range(0, totalWeight);

        if (roll < enemy.LightWeight)
        {
            // Light attack branch
            return new EnemyIntent
            {
                type = EnemyIntentType.LightAttack,
                magnitude = Mathf.Max(1, enemy.LightDamage),
                label = $"Light Attack ({enemy.LightDamage})"
            };
        }

        if (roll < enemy.LightWeight + enemy.HeavyWeight)
        {
            // Heavy attack branch
            return new EnemyIntent
            {
                type = EnemyIntentType.HeavyAttack,
                magnitude = Mathf.Max(1, enemy.HeavyDamage),
                label = $"Heavy Attack ({enemy.HeavyDamage})"
            };
        }

        // Otherwise choose leech heal
        return new EnemyIntent
        {
            type = EnemyIntentType.LeechHeal,
            magnitude = 0,
            label = $"Leech (+{enemy.LeechHeal})"
        };
    }
}
