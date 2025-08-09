using UnityEngine;

/// <summary>
/// Super tiny AI: randomly chooses one of three intents with rough weights.
/// You can later swap this for per-enemy patterns.
/// </summary>
public class EnemyAI : MonoBehaviour
{
    /// <summary>Rolls the next intent using the given config.</summary>
    public EnemyIntent DecideNextIntent(BattleConfigSO cfg)
    {
        // Weighted random: 60% light, 30% heavy, 10% leech heal
        int roll = Random.Range(0, 100);
        if (roll < 60)
        {
            return new EnemyIntent {
                type = EnemyIntentType.LightAttack,
                magnitude = Mathf.Max(1, cfg.enemyLightDamage),
                label = $"Light Attack ({cfg.enemyLightDamage})"
            };
        }
        if (roll < 90)
        {
            return new EnemyIntent {
                type = EnemyIntentType.HeavyAttack,
                magnitude = Mathf.Max(1, cfg.enemyHeavyDamage),
                label = $"Heavy Attack ({cfg.enemyHeavyDamage})"
            };
        }
        // Leech heal (no damage; heals enemy during execution)
        return new EnemyIntent {
            type = EnemyIntentType.LeechHeal,
            magnitude = 0,
            label = $"Leech (+{cfg.enemyLeechHeal})"
        };
    }
}
