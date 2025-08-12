using UnityEngine;

/// <summary>
/// Strategy interface so different enemy AIs can decide intents.
/// </summary>
public interface IEnemyAI
{
    /// <summary>
    /// Decide the enemy's next intent based on its data.
    /// </summary>
    EnemyIntent DecideNextIntent(EnemySO enemy);
}
